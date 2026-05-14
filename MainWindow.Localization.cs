// MainWindow.Localization.cs

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GMCraftTableEditor.Models;
using GMCraftTableEditor.Services;

namespace GMCraftTableEditor;

public partial class MainWindow
{
    // ─── Переключение языка ──────────────────────────────────────────────────

    private void LangBtn_Click(object sender, RoutedEventArgs e)
    {
        LanguageManager.Toggle();
        AppSettingsService.Current.Language = LanguageManager.CurrentLanguage;
        AppSettingsService.Save();
        RefreshPageTitle();
    }

    private void RefreshPageTitle()
    {
        if (_navItems == null) return;
        for (int i = 0; i < _navItems.Length; i++)
        {
            if (_navItems[i]?.Tag?.ToString() == "active")
            {
                var keys = new[] {
                    "S.Nav.Recipes","S.Nav.GmConfig","S.Nav.Quests","S.Nav.NPC","S.Nav.Loot",
                    "S.Nav.Triggers","S.Nav.Player","S.Items.Title","S.Nav.MedicineConfig",
                    "S.Nav.AbilityConfig","S.Nav.LiquidConfig","S.Nav.FoodConfig",
                    "S.Nav.MineRock","S.Nav.RockPoints","S.Nav.Map",
                    "S.Items.MakeStash","S.Items.MakeStash","S.Items.MakeStash",
                    "S.Items.MakeStash","S.Items.MakeStash"
                };
                if (i < keys.Length)
                    PageTitle.SetResourceReference(TextBlock.TextProperty, keys[i]);
                break;
            }
        }
    }

    // ─── Настройки проекта ───────────────────────────────────────────────────

    private void ProjectSettings_Click(object sender, RoutedEventArgs e)
    {
        var setup = new ProjectSetupWindow();
        setup.ShowDialog();
        if (GmPathService.IsConfigured)
            AutoLoadFromGmConfig();
    }

    // ─── Автозагрузка всех конфигов из GM_Config ─────────────────────────────

    public void AutoLoadFromGmConfig()
    {
        if (!GmPathService.IsConfigured) return;

        LoadRecipes();
        LoadGmCraftConfig();
        LoadGmAccessConfig();
        LoadQuestFile();
        LoadQuestSystemConfig();
        LoadPresetConfig();
        LoadMedicineConfig();
        LoadAbilityConfig();
        LoadLiquidConfig();
        LoadFoodConfig();
        LoadMineRockConfig();
        LoadRockPointsConfig();
    }

    // ── Рецепты ──────────────────────────────────────────────────────────────

    private void LoadRecipes()
    {
        if (_recipesPath != null) return;
        var dir = GmPathService.RecipesPath;
        if (!Directory.Exists(dir)) return;

        var files = Directory.GetFiles(dir, "*.json");
        if (files.Length == 0) return;

        var path = files.FirstOrDefault(f =>
            Path.GetFileName(f).Equals("craft_recipes.json", StringComparison.OrdinalIgnoreCase))
            ?? files[0];

        try
        {
            _recipesPath = path;
            _recipes = JsonService.Load<ObservableCollection<Recipe>>(_recipesPath);
            NormalizeRecipes();
            RecipesList.ItemsSource = _recipes;
            _autoSave.Register("recipes", () => { if (_recipesPath != null) JsonService.Save(_recipesPath, _recipes); });
            _openFiles["recipes"] = Path.GetFileName(_recipesPath);
            _autoSave.MarkClean("recipes");
        }
        catch { }
    }

    // ── GM Config ────────────────────────────────────────────────────────────

    private void LoadGmCraftConfig()
    {
        if (_craftConfigPath != null) return;
        var path = Path.Combine(GmPathService.CraftTablePath, "GM_CRAFTTABLE_CONFIG.json");
        if (!File.Exists(path)) return;
        try
        {
            _craftConfigPath = path;
            _craftConfig = JsonService.Load<GMCraftTableConfig>(_craftConfigPath);
            CategoriesGrid.ItemsSource = _craftConfig.CATEGORY_LIST;
            SetGmConfigStatus($"GM_CRAFTTABLE_CONFIG загружен. Категорий: {_craftConfig.CATEGORY_LIST.Count}");
        }
        catch { }
    }

    private void LoadGmAccessConfig()
    {
        if (_accessConfigPath != null) return;
        var path = Path.Combine(GmPathService.CraftTablePath, "GM_ACCESS_CONFIG.json");
        if (!File.Exists(path)) return;
        try
        {
            _accessConfigPath = path;
            _accessConfig = JsonService.Load<GMAccessConfig>(_accessConfigPath);
            CraftTablesGrid.ItemsSource = _accessConfig.CRAFT_TABLES;
            AdminIdsList.ItemsSource = _accessConfig.ADMIN_IDS;
        }
        catch { }
    }

    // ── Квесты ───────────────────────────────────────────────────────────────

    private void LoadQuestFile()
    {
        if (_questFilePath != null) return;
        var dir = GmPathService.QuestsPath;
        if (!Directory.Exists(dir)) return;

        var files = Directory.GetFiles(dir, "*.json");
        if (files.Length == 0) return;

        var path = files
            .OrderBy(f => {
                var n = Path.GetFileNameWithoutExtension(f).Replace("id_", "");
                return int.TryParse(n, out var num) ? num : 999;
            })
            .ThenBy(f => f)
            .First();

        try
        {
            _questFilePath = path;
            _quests = JsonService.Load<ObservableCollection<QuestFileItem>>(_questFilePath);
            NormalizeQuests();
            QuestsList.ItemsSource = _quests;
            _autoSave.Register("quests", () => { if (_questFilePath != null) JsonService.Save(_questFilePath, _quests); });
            _openFiles["quests"] = Path.GetFileName(_questFilePath);
            _autoSave.MarkClean("quests");
        }
        catch { }
    }

    private void LoadQuestSystemConfig()
    {
        if (_questConfigPath != null) return;
        var path = Path.Combine(GmPathService.QuestSystemPath, "GM_QuestSystemCFG.json");
        if (!File.Exists(path)) return;
        try
        {
            _questConfigPath = path;
            _questConfig = JsonService.Load<GMQuestSystemConfig>(_questConfigPath);
            NpcList.ItemsSource = _questConfig.NPC;
        }
        catch { }
    }

    private void LoadPresetConfig()
    {
        if (_presetConfigPath != null) return;
        var path = Path.Combine(GmPathService.QuestSystemPath, "GM_PRESET_CONFIG.json");
        if (!File.Exists(path)) return;
        try
        {
            _presetConfigPath = path;
            _presetConfig = JsonService.Load<GMPresetConfig>(_presetConfigPath);
            RefreshLootPresetsList();
            TriggersGrid.ItemsSource = _presetConfig.TRIGGER_SETTINGS;
            MappingGrid.ItemsSource  = _presetConfig.MAPPING_SETTINGS;
        }
        catch { }
    }

    // ── Medicine / Ability ───────────────────────────────────────────────────

    private void LoadMedicineConfig()
    {
        if (_medicinePath != null) return;
        var path = Path.Combine(GmPathService.AbilityPath, "GM_MEDICINE_CONFIG.json");
        if (!File.Exists(path)) return;
        try
        {
            _medicinePath = path;
            _medicineAll  = LoadFlatConfig(_medicinePath);
            LoadMedicineGrid(_medicineAll);
            MedicineSectionCombo.ItemsSource = new[] { LanguageManager.Get("S.Col.All") }.Concat(GetSections(_medicineAll)).ToList();
            MedicineSectionCombo.SelectedIndex = 0;
            _autoSave.Register("medicine", () => { if (_medicinePath != null) SaveFlatConfig(_medicinePath, _medicineAll, _medicinePath); });
            _autoSave.MarkClean("medicine");
        }
        catch { }
    }

    private void LoadAbilityConfig()
    {
        if (_abilityPath != null) return;
        var path = Path.Combine(GmPathService.AbilityPath, "GM_ABILITY_CONFIG.json");
        if (!File.Exists(path)) return;
        try
        {
            _abilityPath   = path;
            _abilityConfig = JsonService.Load<GMAbilityConfig>(_abilityPath);
            LoadAbilityFields();
            _autoSave.Register("ability", () => { if (_abilityPath != null) JsonService.Save(_abilityPath, _abilityConfig!); });
            _autoSave.MarkClean("ability");
        }
        catch { }
    }

    // ── Liquid / Food ────────────────────────────────────────────────────────

    private void LoadLiquidConfig()
    {
        if (_liquidPath != null) return;
        var path = Path.Combine(GmPathService.FoodPath, "GM_LiquidConfig.json");
        if (!File.Exists(path)) return;
        try
        {
            _liquidPath = path;
            _liquidAll  = LoadFlatConfig(_liquidPath);
            LiquidSectionCombo.ItemsSource = new[] { LanguageManager.Get("S.Col.All") }.Concat(GetSections(_liquidAll)).ToList();
            LiquidSectionCombo.SelectedIndex = 0;
            FilterLiquidGrid();
            _autoSave.Register("liquid", () => { if (_liquidPath != null) SaveFlatConfig(_liquidPath, _liquidAll, _liquidPath); });
            _autoSave.MarkClean("liquid");
        }
        catch { }
    }

    private void LoadFoodConfig()
    {
        if (_foodPath != null) return;
        var path = Path.Combine(GmPathService.FoodPath, "GM_FoodConfig.json");
        if (!File.Exists(path)) return;
        try
        {
            _foodPath   = path;
            _foodConfig = JsonService.Load<GMFoodConfig>(_foodPath);
            LoadFoodFields();
            _autoSave.Register("food", () => { if (_foodPath != null) JsonService.Save(_foodPath, _foodConfig!); });
            _autoSave.MarkClean("food");
        }
        catch { }
    }

    // ── MineRock / RockPoints ────────────────────────────────────────────────

    private void LoadMineRockConfig()
    {
        if (_minePath != null) return;
        var path = Path.Combine(GmPathService.MineRockPath, "GM_MineRockConfig.json");
        if (!File.Exists(path)) return;
        try
        {
            _minePath   = path;
            _mineConfig = JsonService.Load<GMMineRockConfig>(_minePath);
            LoadMineFields();
            _autoSave.Register("mine", () => { if (_minePath != null) JsonService.Save(_minePath, _mineConfig!); });
            _autoSave.MarkClean("mine");
        }
        catch { }
    }

    private void LoadRockPointsConfig()
    {
        if (_rockPath != null) return;
        var path = Path.Combine(GmPathService.MineRockPath, "ROCK_POINTS.json");
        if (!File.Exists(path)) return;
        try
        {
            _rockPath   = path;
            _rockConfig = JsonService.Load<RockPointsConfig>(_rockPath);
            RockObjectsList.ItemsSource = _rockConfig.OBJECT_LIST;
            _autoSave.Register("rock", () => { if (_rockPath != null) JsonService.Save(_rockPath, _rockConfig!); });
            _autoSave.MarkClean("rock");
        }
        catch { }
    }
}
