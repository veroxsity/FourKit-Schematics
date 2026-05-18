using Minecraft.Server.FourKit;
using Minecraft.Server.FourKit.Command;
using Minecraft.Server.FourKit.Entity;

using Schematics.Schematic;

namespace Schematics.Commands;

public sealed class SchemCommand : CommandExecutor
{
    private readonly Schematics _plugin;

    public SchemCommand(Schematics plugin) { _plugin = plugin; }

    public bool onCommand(CommandSender sender, Command command, string label, string[] args)
    {
        if (sender is not Player p)
        {
            sender.sendMessage("Only players can run /schem.");
            return true;
        }

        if (args.Length == 0) { sendUsage(p); return true; }

        switch (args[0].ToLowerInvariant())
        {
            case "export": return handleExport(p, args);
            case "paste":  return handlePaste(p, args);
            case "list":   return handleList(p);
            case "info":   return handleInfo(p, args);
            default:       sendUsage(p); return true;
        }
    }

    private bool handleExport(Player p, string[] args)
    {
        if (args.Length < 8)
        {
            p.sendMessage("§cUsage: /schem export <name> <x1> <y1> <z1> <x2> <y2> <z2>");
            return true;
        }
        var name = args[1];
        if (!tryParseInt(args[2], out var x1) || !tryParseInt(args[3], out var y1) || !tryParseInt(args[4], out var z1) ||
            !tryParseInt(args[5], out var x2) || !tryParseInt(args[6], out var y2) || !tryParseInt(args[7], out var z2))
        {
            p.sendMessage("§cCoordinates must be integers.");
            return true;
        }

        var min = (Math.Min(x1, x2), Math.Min(y1, y2), Math.Min(z1, z2));
        var max = (Math.Max(x1, x2), Math.Max(y1, y2), Math.Max(z1, z2));

        var worldName = p.getLocation().getWorld()?.getName() ?? "world";
        long w = max.Item1 - min.Item1 + 1;
        long h = max.Item2 - min.Item2 + 1;
        long d = max.Item3 - min.Item3 + 1;
        long volume = w * h * d;

        p.sendMessage("§6[Schem] §7Exporting §f" + name + " (" + w + "x" + h + "x" + d + " = " + volume + " blocks)...");

        var job = new SchematicCopier.ExportJob
        {
            Name = name,
            WorldName = worldName,
            Min = min,
            Max = max,
        };

        SchematicCopier.runExport(
            _plugin,
            job,
            blocksPerTick: 8000,
            onProgress: (done, total) =>
            {
                if (done % 80000 == 0)
                    p.sendMessage("  " + done + "/" + total + " (" + (done * 100 / Math.Max(1, total)) + "%)");
            },
            onComplete: j =>
            {
                long sizeKb = SchematicStore.sizeOnDisk(j.Name) / 1024;
                p.sendMessage("§a[Schem] §7Saved §f" + j.Name + ".json§7 (" + j.NonAir + " non-air blocks, " + sizeKb + " KB on disk)");
            });
        return true;
    }

    private bool handlePaste(Player p, string[] args)
    {
        if (args.Length < 2)
        {
            p.sendMessage("§cUsage: /schem paste <name> [<x> <y> <z>]");
            return true;
        }
        var name = args[1];

        int ox, oy, oz;
        if (args.Length >= 5)
        {
            if (!tryParseInt(args[2], out ox) || !tryParseInt(args[3], out oy) || !tryParseInt(args[4], out oz))
            {
                p.sendMessage("§cCoordinates must be integers.");
                return true;
            }
        }
        else
        {
            var loc = p.getLocation();
            ox = loc.getBlockX();
            oy = loc.getBlockY();
            oz = loc.getBlockZ();
            p.sendMessage("§7No coords given; pasting at your current position (" + ox + "," + oy + "," + oz + ").");
        }

        var data = SchematicStore.load(name);
        if (data == null)
        {
            p.sendMessage("No schematic named " + name + "§c. Try /schem list.");
            return true;
        }

        var worldName = p.getLocation().getWorld()?.getName() ?? "world";
        p.sendMessage("§6[Schem] §7Pasting §f" + name + " (" + data.Width + "x" + data.Height + "x" + data.Depth + ") at (" + ox + "," + oy + "," + oz + ")...");

        var job = new SchematicCopier.PasteJob
        {
            Name = name,
            WorldName = worldName,
            Origin = (ox, oy, oz),
            Data = data,
        };
        // Hack: SchematicCopier expects Data field set before runPaste; set it explicitly via a wrapper struct field.
        // (Done above.)
        SchematicCopier.runPaste(
            _plugin,
            job,
            blocksPerTick: 8000,
            skipAir: true,
            onProgress: (done, total) =>
            {
                if (done % 80000 == 0)
                    p.sendMessage("  " + done + "/" + total + " (" + (done * 100 / Math.Max(1, total)) + "%)");
            },
            onComplete: j =>
            {
                p.sendMessage("§a[Schem] §7Pasted §f" + j.Name + ". " + j.Written + " blocks written.");
            });
        return true;
    }

    private bool handleList(Player p)
    {
        var names = SchematicStore.list().ToList();
        if (names.Count == 0)
        {
            p.sendMessage("§7No schematics saved. Run §f/schem export §7to create one.");
            return true;
        }
        p.sendMessage("§6Schematics (" + names.Count + "):");
        foreach (var n in names)
            p.sendMessage("- " + n + " (" + (SchematicStore.sizeOnDisk(n) / 1024) + " KB)");
        return true;
    }

    private bool handleInfo(Player p, string[] args)
    {
        if (args.Length < 2)
        {
            p.sendMessage("§cUsage: /schem info <name>");
            return true;
        }
        var data = SchematicStore.load(args[1]);
        if (data == null)
        {
            p.sendMessage("§cNo schematic named §f" + args[1] + "§c.");
            return true;
        }
        p.sendMessage("[Schem] " + data.Name);
        p.sendMessage("§7  size:       §f" + data.Width + "x" + data.Height + "x" + data.Depth + " (" + data.volume() + " blocks)");
        p.sendMessage("§7  non-air:    §f" + data.NonAirCount);
        p.sendMessage("§7  origin:     §f" + data.ExportOrigin[0] + "," + data.ExportOrigin[1] + "," + data.ExportOrigin[2]);
        p.sendMessage("§7  source:     §f" + data.SourceWorld);
        p.sendMessage("§7  exported:   §f" + data.ExportedAt);
        p.sendMessage("§7  file size:  §f" + (SchematicStore.sizeOnDisk(args[1]) / 1024) + " KB");
        return true;
    }

    private void sendUsage(Player p)
    {
        p.sendMessage("§6[Schem] §7Commands:");
        p.sendMessage("§7- §f/schem export <name> <x1> <y1> <z1> <x2> <y2> <z2>");
        p.sendMessage("§7- §f/schem paste <name> [<x> <y> <z>]");
        p.sendMessage("§7- §f/schem list");
        p.sendMessage("§7- §f/schem info <name>");
    }

    private static bool tryParseInt(string s, out int result) =>
        int.TryParse(s, System.Globalization.NumberStyles.Integer,
                     System.Globalization.CultureInfo.InvariantCulture, out result);
}
