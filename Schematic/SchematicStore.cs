using System.Text.Json;

namespace Schematics.Schematic;

/// <summary>
/// Disk-side store for schematic JSON files. Lives at
/// <c>plugins/Schematics-data/&lt;name&gt;.json</c> relative to the server working
/// directory.
/// </summary>
public static class SchematicStore
{
    public const string DataFolder = "plugins/Schematics-data";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,    // compact; these files can be huge
        IncludeFields = false,
    };

    public static string getDir()
    {
        Directory.CreateDirectory(DataFolder);
        return DataFolder;
    }

    public static string pathFor(string name) =>
        Path.Combine(getDir(), sanitize(name) + ".json");

    public static void save(string name, SchematicData data)
    {
        var path = pathFor(name);
        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, data, Options);
    }

    public static SchematicData? load(string name)
    {
        var path = pathFor(name);
        if (!File.Exists(path)) return null;
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<SchematicData>(stream, Options);
    }

    public static IEnumerable<string> list()
    {
        if (!Directory.Exists(DataFolder)) yield break;
        foreach (var f in Directory.EnumerateFiles(DataFolder, "*.json"))
            yield return Path.GetFileNameWithoutExtension(f);
    }

    public static long sizeOnDisk(string name)
    {
        var path = pathFor(name);
        if (!File.Exists(path)) return 0;
        return new FileInfo(path).Length;
    }

    /// <summary>Allow letters, digits, underscore, dash. Strip anything else.</summary>
    private static string sanitize(string name)
    {
        var chars = name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray();
        var clean = new string(chars);
        return string.IsNullOrEmpty(clean) ? "unnamed" : clean;
    }
}
