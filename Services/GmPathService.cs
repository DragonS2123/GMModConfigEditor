using System.IO;

namespace GMCraftTableEditor.Services;

public static class GmPathService
{
    public static string? GmConfigPath { get; private set; }

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(GmConfigPath) && Directory.Exists(GmConfigPath);

    public static string CraftTablePath => Require(Path.Combine(GmConfigPath!, "CraftTable"));
    public static string RecipesPath => Require(Path.Combine(CraftTablePath, "Recipes"));
    public static string QuestSystemPath => Require(Path.Combine(GmConfigPath!, "QuestSystem"));
    public static string QuestsPath => Require(Path.Combine(QuestSystemPath, "Quests"));
    public static string AbilityPath => Require(Path.Combine(GmConfigPath!, "Ability"));
    public static string FoodPath => Require(Path.Combine(GmConfigPath!, "Food"));
    public static string MineRockPath => Require(Path.Combine(GmConfigPath!, "MineRock"));

    public static string? TryResolveGmConfig(string selectedPath)
    {
        if (string.IsNullOrWhiteSpace(selectedPath)) return null;
        selectedPath = selectedPath.Trim().Trim('"');

        if (Directory.Exists(Path.Combine(selectedPath, "GM_Config")))
            return Path.GetFullPath(Path.Combine(selectedPath, "GM_Config"));

        if (Path.GetFileName(selectedPath).Equals("GM_Config", StringComparison.OrdinalIgnoreCase) && Directory.Exists(selectedPath))
            return Path.GetFullPath(selectedPath);

        return null;
    }

    public static bool ValidateGmConfig(string gmConfigPath, out string error)
    {
        var required = new[]
        {
            Path.Combine(gmConfigPath, "CraftTable"),
            Path.Combine(gmConfigPath, "CraftTable", "Recipes"),
            Path.Combine(gmConfigPath, "QuestSystem"),
            Path.Combine(gmConfigPath, "QuestSystem", "Quests"),
        };

        var missing = required.Where(p => !Directory.Exists(p)).ToList();
        if (missing.Count == 0)
        {
            error = "";
            return true;
        }

        error = "Не найдены обязательные папки:\n" + string.Join("\n", missing);
        return false;
    }

    public static bool Configure(string selectedPath, out string error)
    {
        var gmConfig = TryResolveGmConfig(selectedPath);
        if (gmConfig == null)
        {
            error = "Выберите папку profiles, внутри которой есть GM_Config, или саму папку GM_Config.";
            return false;
        }

        if (!ValidateGmConfig(gmConfig, out error)) return false;

        GmConfigPath = gmConfig;
        var settings = AppSettingsService.Current;
        settings.GmConfigPath = gmConfig;
        AppSettingsService.Save();
        return true;
    }

    public static bool LoadFromSettings()
    {
        var path = AppSettingsService.Current.GmConfigPath;
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!Directory.Exists(path)) return false;
        if (!ValidateGmConfig(path, out _)) return false;
        GmConfigPath = path;
        return true;
    }

    private static string Require(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}