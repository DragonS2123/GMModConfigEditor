using System.IO;

namespace GMCraftTableEditor.Services;

public static class GmPathService
{
    public static string? GmConfigPath { get; private set; }

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(GmConfigPath) && Directory.Exists(GmConfigPath);

    public static string CraftTablePath =>
        Path.Combine(GmConfigPath!, "CraftTable");

    public static string RecipesPath =>
        Path.Combine(CraftTablePath, "Recipes");

    public static string QuestSystemPath =>
        Path.Combine(GmConfigPath!, "QuestSystem");

    public static string QuestsPath =>
        Path.Combine(QuestSystemPath, "Quests");

    public static string AbilityPath =>
        Path.Combine(GmConfigPath!, "Ability");

    public static string FoodPath =>
        Path.Combine(GmConfigPath!, "Food");

    public static string MineRockPath =>
        Path.Combine(GmConfigPath!, "MineRock");

    public static bool HasCraftTable =>
    IsConfigured && Directory.Exists(Path.Combine(GmConfigPath!, "CraftTable"));

    public static bool HasRecipes =>
        HasCraftTable && Directory.Exists(Path.Combine(GmConfigPath!, "CraftTable", "Recipes"));

    public static bool HasQuestSystem =>
        IsConfigured && Directory.Exists(Path.Combine(GmConfigPath!, "QuestSystem"));

    public static bool HasQuests =>
        HasQuestSystem && Directory.Exists(Path.Combine(GmConfigPath!, "QuestSystem", "Quests"));

    public static bool HasAbility =>
        IsConfigured && Directory.Exists(Path.Combine(GmConfigPath!, "Ability"));

    public static bool HasFood =>
        IsConfigured && Directory.Exists(Path.Combine(GmConfigPath!, "Food"));

    public static bool HasMineRock =>
        IsConfigured && Directory.Exists(Path.Combine(GmConfigPath!, "MineRock"));

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
        if (!Directory.Exists(gmConfigPath))
        {
            error = "Папка GM_Config не найдена.";
            return false;
        }

        var knownFolders = new[]
        {
            "CraftTable",
            "QuestSystem",
            "Ability",
            "Food",
            "MineRock"
        };

        var hasAnyKnownFolder = knownFolders.Any(folder =>
            Directory.Exists(Path.Combine(gmConfigPath, folder)));

        if (!hasAnyKnownFolder)
        {
            error = "GM_Config найдена, но внутри нет поддерживаемых папок модов.";
            return false;
        }

        error = "";
        return true;
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
        // Используем SetProjectPath чтобы правильно сохранить настройки
        AppSettingsService.SetProjectPath(selectedPath);
        // Убедимся что GmConfigPath установлена правильно (SetProjectPath может переиндексировать)
        AppSettingsService.Current.GmConfigPath = gmConfig;
        AppSettingsService.Save();
        return true;
    }

    public static bool LoadFromSettings()
    {
        // GmConfigPath помечена как [JsonIgnore], поэтому её нет в JSON
        // Загружаем ProjectPath и пересчитываем из него
        var appSettings = AppSettingsService.Current;
        
        if (string.IsNullOrWhiteSpace(appSettings.ProjectPath)) 
            return false;
        
        // Пересчитываем GmConfigPath из ProjectPath
        var gmConfig = TryResolveGmConfig(appSettings.ProjectPath);
        if (gmConfig == null) 
            return false;
        
        if (!ValidateGmConfig(gmConfig, out _)) 
            return false;

        GmConfigPath = gmConfig;
        appSettings.GmConfigPath = gmConfig;  // Обновляем в памяти
        return true;
    }

    private static string Require(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}