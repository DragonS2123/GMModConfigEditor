using System.IO;
using System.Xml.Linq;
using System.Text.Json;
using GMCraftTableEditor.Models;

namespace GMCraftTableEditor.Services;

public static class ItemDatabaseService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static List<ItemDatabaseEntry> Load(string path)
    {
        if (!File.Exists(path))
            return new List<ItemDatabaseEntry>();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<ItemDatabaseEntry>>(json, Options)
               ?? new List<ItemDatabaseEntry>();
    }

    public static void Save(string path, List<ItemDatabaseEntry> items)
    {
        var json = JsonSerializer.Serialize(items, Options);
        File.WriteAllText(path, json);
    }

    public static List<ItemDatabaseEntry> ImportFromTxt(string path, string category = "", string sourceMod = "")
    {
        var lines = File.ReadAllLines(path);

        return lines
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Where(x => !x.StartsWith("//"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => new ItemDatabaseEntry
            {
                ClassName = x,
                DisplayName = "",
                Category = category,
                SourceMod = sourceMod,
                Comment = "",
                Favorite = false
            })
            .ToList();
    }

    public static List<ItemDatabaseEntry> ImportFromTypesXml(string path, string sourceMod = "")
    {
        var doc = XDocument.Load(path);

        return doc
            .Descendants("type")
            .Select(x => x.Attribute("name")?.Value?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => new ItemDatabaseEntry
            {
                ClassName = x!,
                DisplayName = "",
                Category = "types.xml",
                SourceMod = string.IsNullOrWhiteSpace(sourceMod)
                    ? Path.GetFileNameWithoutExtension(path)
                    : sourceMod,
                Comment = "",
                Favorite = false
            })
            .ToList();
    }
    
    public static List<ItemDatabaseEntry> ImportFromTypesFolder(string folderPath)
    {
        var result = new List<ItemDatabaseEntry>();

        var files = Directory.GetFiles(folderPath, "*.xml", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            try
            {
                var imported = ImportFromTypesXml(file, Path.GetFileNameWithoutExtension(file));
                result.AddRange(imported);
            }
            catch
            {
                // пропускаем XML, которые не являются types.xml
            }
        }

        return result
            .GroupBy(x => x.ClassName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }
}