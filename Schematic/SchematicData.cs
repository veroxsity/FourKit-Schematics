namespace Schematics.Schematic;

/// <summary>
/// In-memory representation of a copied region. Serializes to/from JSON.
/// Blocks are packed as (typeId &lt;&lt; 4) | (data &amp; 0x0F) so a single int holds both.
/// Air (typeId 0) is stored as 0 to keep deserialization uniform.
/// </summary>
public sealed class SchematicData
{
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "";

    /// <summary>[width, height, depth] in blocks.</summary>
    public int[] Size { get; set; } = new int[] { 0, 0, 0 };

    /// <summary>World coords the source AABB originated at, recorded for reference only.</summary>
    public int[] ExportOrigin { get; set; } = new int[] { 0, 0, 0 };

    /// <summary>ISO 8601 timestamp of when the export ran.</summary>
    public string ExportedAt { get; set; } = "";

    /// <summary>World name the schematic was exported from. Informational only.</summary>
    public string SourceWorld { get; set; } = "";

    /// <summary>Number of non-air blocks; useful for progress estimates on paste.</summary>
    public long NonAirCount { get; set; }

    /// <summary>Packed blocks, length = width * height * depth. Index = (y*depth + z)*width + x.</summary>
    public int[] Blocks { get; set; } = System.Array.Empty<int>();

    // ---- helpers ----

    public int Width  => Size[0];
    public int Height => Size[1];
    public int Depth  => Size[2];

    public long volume() => (long)Width * Height * Depth;

    public int indexOf(int x, int y, int z) => (y * Depth + z) * Width + x;

    public static int pack(int typeId, byte data) => (typeId << 4) | (data & 0x0F);

    public static (int typeId, byte data) unpack(int packed) =>
        (packed >> 4, (byte)(packed & 0x0F));
}
