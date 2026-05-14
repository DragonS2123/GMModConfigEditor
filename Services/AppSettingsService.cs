using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GMCraftTableEditor.Services;

public class AppSettings
{
    /// <summary>Путь к папке проекта (где лежат profiles / мод-папки).</summary>
    public string ProjectPath { get; set; } = "";

    /// <summary>Найденный путь к GM_Config (вычисляется автоматически).</summary>
    [JsonIgnore]
    public string GmConfigPath { get; set; } = "";

    public string Language { get; set; } = "ru";
    public string Theme    { get; set; } = "dark";

    // ── Вычисленные пути к папкам ────────────────────────────────────────────

    [JsonIgnore] public string RecipesPath      => Path.Combine(GmConfigPath, "CraftTable", "Recipes");
    [JsonIgnore] public string QuestsPath       => Path.Combine(GmConfigPath, "QuestSystem", "Quests");
    [JsonIgnore] public string CraftTablePath   => Path.Combine(GmConfigPath, "CraftTable");
    [JsonIgnore] public string QuestSystemPath  => Path.Combine(GmConfigPath, "QuestSystem");
    [JsonIgnore] public string AbilityPath      => Path.Combine(GmConfigPath, "Ability");
    [JsonIgnore] public string FoodPath         => Path.Combine(GmConfigPath, "Food");
    [JsonIgnore] public string MineRockPath     => Path.Combine(GmConfigPath, "MineRock");

    [JsonIgnore] public bool IsConfigured => !string.IsNullOrEmpty(GmConfigPath) && Directory.Exists(GmConfigPath);
}

public static class AppSettingsService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private static string SettingsPath =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static AppSettings Current { get; private set; } = new();

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                Current = JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
            }
        }
        catch
        {
            Current = new AppSettings();
        }

        // Вычисляем GmConfigPath из ProjectPath
        ResolveGmConfigPath();

        return Current;
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, Options);
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* игнорируем ошибки записи */ }
    }

    public static void SetProjectPath(string path)
    {
        Current.ProjectPath = path;
        ResolveGmConfigPath();
        Save();
    }

    /// <summary>
    /// Ищет папку GM_Config начиная от ProjectPath.
    /// Проверяет: ProjectPath\GM_Config, ProjectPath\profiles\GM_Config,
    /// и рекурсивно до глубины 3.
    /// </summary>
    private static void ResolveGmConfigPath()
    {
        Current.GmConfigPath = "";

        if (string.IsNullOrEmpty(Current.ProjectPath))
            return;

        if (!Directory.Exists(Current.ProjectPath))
            return;

        var found = FindGmConfig(Current.ProjectPath, depth: 0, maxDepth: 3);
        if (found != null)
            Current.GmConfigPath = found;
    }

    private static string? FindGmConfig(string dir, int depth, int maxDepth)
    {
        // Проверяем, может быть сама текущая папка это GM_Config
        if (Path.GetFileName(dir).Equals("GM_Config", StringComparison.OrdinalIgnoreCase) && Directory.Exists(dir))
            return dir;

        // Проверяем текущую папку
        var candidate = Path.Combine(dir, "GM_Config");
        if (Directory.Exists(candidate))
            return candidate;

        if (depth >= maxDepth)
            return null;

        // Рекурсивно ищем в подпапках
        try
        {
            foreach (var sub in Directory.GetDirectories(dir))
            {
                var name = Path.GetFileName(sub);
                // Пропускаем системные и скрытые папки
                if (name.StartsWith('.') || name.Equals("$Recycle.Bin", StringComparison.OrdinalIgnoreCase))
                    continue;

                var result = FindGmConfig(sub, depth + 1, maxDepth);
                if (result != null)
                    return result;
            }
        }
        catch { /* нет доступа */ }

        return null;
    }
}
