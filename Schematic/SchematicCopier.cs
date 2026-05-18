using Minecraft.Server.FourKit;
using Minecraft.Server.FourKit.Scheduler;

using FKWorld = Minecraft.Server.FourKit.World;

namespace Schematics.Schematic;

/// <summary>
/// Batched block read/write between a live world and a SchematicData.
/// Both directions use the scheduler to spread work across ticks so the
/// server stays responsive during large copies.
/// </summary>
public static class SchematicCopier
{
    public sealed class ExportJob
    {
        public string Name = "";
        public string WorldName = "";
        public (int x, int y, int z) Min;
        public (int x, int y, int z) Max;

        internal SchematicData Data = null!;
        internal int CursorX, CursorY, CursorZ;
        internal bool Started;
        internal long Total;
        internal long Done;
        internal long NonAir;
    }

    public sealed class PasteJob
    {
        public string Name = "";
        public string WorldName = "";
        public (int x, int y, int z) Origin;

        internal SchematicData Data = null!;
        internal int CursorX, CursorY, CursorZ;
        internal bool Started;
        internal long Total;
        internal long Done;
        internal long Written;
    }

    // ---------- EXPORT ----------

    public static void runExport(
        Schematics plugin,
        ExportJob job,
        int blocksPerTick,
        Action<long, long>? onProgress = null,
        Action<ExportJob>? onComplete = null)
    {
        var world = FourKit.getWorld(job.WorldName);
        if (world == null) { onComplete?.Invoke(job); return; }

        int w = job.Max.x - job.Min.x + 1;
        int h = job.Max.y - job.Min.y + 1;
        int d = job.Max.z - job.Min.z + 1;
        long volume = (long)w * h * d;

        // Preload source chunks so we read real data, not air.
        preloadChunks(world, job.Min, job.Max);

        job.Data = new SchematicData
        {
            Name = job.Name,
            Size = new[] { w, h, d },
            ExportOrigin = new[] { job.Min.x, job.Min.y, job.Min.z },
            ExportedAt = DateTime.UtcNow.ToString("O"),
            SourceWorld = job.WorldName,
            Blocks = new int[volume],
        };
        job.CursorX = 0; job.CursorY = 0; job.CursorZ = 0;
        job.Total = volume;
        job.Done = 0;
        job.NonAir = 0;
        job.Started = true;

        FourKitTask? task = null;
        task = FourKit.getScheduler().runTaskTimer(plugin, () =>
        {
            for (int i = 0; i < blocksPerTick; i++)
            {
                if (job.CursorY >= h)
                {
                    job.Data.NonAirCount = job.NonAir;
                    SchematicStore.save(job.Name, job.Data);
                    task!.cancel();
                    onComplete?.Invoke(job);
                    return;
                }

                int wx = job.Min.x + job.CursorX;
                int wy = job.Min.y + job.CursorY;
                int wz = job.Min.z + job.CursorZ;
                var b = world.getBlockAt(wx, wy, wz);
                int typeId = b.getTypeId();
                byte data = b.getData();
                int idx = (job.CursorY * d + job.CursorZ) * w + job.CursorX;
                job.Data.Blocks[idx] = SchematicData.pack(typeId, data);
                if (typeId != 0) job.NonAir++;
                job.Done++;

                job.CursorX++;
                if (job.CursorX >= w)
                {
                    job.CursorX = 0;
                    job.CursorZ++;
                    if (job.CursorZ >= d)
                    {
                        job.CursorZ = 0;
                        job.CursorY++;
                    }
                }
            }
            onProgress?.Invoke(job.Done, job.Total);
        }, 1, 1);
    }

    // ---------- PASTE ----------

    public static void runPaste(
        Schematics plugin,
        PasteJob job,
        int blocksPerTick,
        bool skipAir,
        Action<long, long>? onProgress = null,
        Action<PasteJob>? onComplete = null)
    {
        var world = FourKit.getWorld(job.WorldName);
        if (world == null) { onComplete?.Invoke(job); return; }
        if (job.Data == null) throw new InvalidOperationException("PasteJob.Data must be set before runPaste");

        int w = job.Data.Width;
        int h = job.Data.Height;
        int d = job.Data.Depth;
        long volume = (long)w * h * d;

        // Preload destination chunks.
        preloadChunks(world,
            (job.Origin.x, job.Origin.y, job.Origin.z),
            (job.Origin.x + w - 1, job.Origin.y + h - 1, job.Origin.z + d - 1));

        job.CursorX = 0; job.CursorY = 0; job.CursorZ = 0;
        job.Total = volume;
        job.Done = 0;
        job.Written = 0;
        job.Started = true;

        FourKitTask? task = null;
        task = FourKit.getScheduler().runTaskTimer(plugin, () =>
        {
            for (int i = 0; i < blocksPerTick; i++)
            {
                if (job.CursorY >= h)
                {
                    task!.cancel();
                    onComplete?.Invoke(job);
                    return;
                }

                int idx = (job.CursorY * d + job.CursorZ) * w + job.CursorX;
                int packed = job.Data.Blocks[idx];
                var (typeId, data) = SchematicData.unpack(packed);

                if (!(skipAir && typeId == 0))
                {
                    int wx = job.Origin.x + job.CursorX;
                    int wy = job.Origin.y + job.CursorY;
                    int wz = job.Origin.z + job.CursorZ;
                    var b = world.getBlockAt(wx, wy, wz);
                    b.setTypeIdAndData(typeId, data, false);
                    job.Written++;
                }
                job.Done++;

                job.CursorX++;
                if (job.CursorX >= w)
                {
                    job.CursorX = 0;
                    job.CursorZ++;
                    if (job.CursorZ >= d)
                    {
                        job.CursorZ = 0;
                        job.CursorY++;
                    }
                }
            }
            onProgress?.Invoke(job.Done, job.Total);
        }, 1, 1);
    }

    private static void preloadChunks(FKWorld world, (int x, int y, int z) min, (int x, int y, int z) max)
    {
        int minCx = min.x >> 4;
        int maxCx = max.x >> 4;
        int minCz = min.z >> 4;
        int maxCz = max.z >> 4;
        for (int cx = minCx; cx <= maxCx; cx++)
        for (int cz = minCz; cz <= maxCz; cz++)
            world.loadChunk(cx, cz, generate: true);
    }
}
