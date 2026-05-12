using System.Collections.Generic;
using GMCraftTableEditor.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using GMCraftTableEditor.Services;

namespace GMCraftTableEditor;

// ── Mods Models (inline) ──────────────────────────────────────────────────


// ════════════════════════════════════════════════════════════════════════════
// UNIVERSAL: плоский конфиг как список строк ключ-значение
// Используется для GM_MEDICINE_CONFIG, GM_ABILITY_CONFIG, GM_LiquidConfig
// ════════════════════════════════════════════════════════════════════════════

public class FlatConfigRow
{
    public string Key      { get; set; } = "";
    public string Value    { get; set; } = "";
    /// <summary>section / string / int / float / list</summary>
    public string Kind     { get; set; } = "";
    public bool   IsSection => Kind == "section";
}

// ════════════════════════════════════════════════════════════════════════════
// GM_ABILITY_CONFIG
// ════════════════════════════════════════════════════════════════════════════

public class GMAbilityConfig
{
    public string CONFIG_VERSION           { get; set; } = "";
    public string TITLE                    { get; set; } = "";
    public string AUTHOR                   { get; set; } = "";
    public string DISCORD                  { get; set; } = "";
    public string ABILITY_SETTINGS         { get; set; } = "";
    public int    SaveAbilityPointsOnDeath { get; set; }
    public int    Enable_PeriodSave        { get; set; }
    public int    Period_OnlineAutoSave    { get; set; }
    public string MAP                      { get; set; } = "";
}

// ════════════════════════════════════════════════════════════════════════════
// GM_FoodConfig
// ════════════════════════════════════════════════════════════════════════════

public class GMFoodConfig
{
    public string       TITLE                      { get; set; } = "";
    public string       AUTHOR                     { get; set; } = "";
    public string       DISCORD                    { get; set; } = "";
    public string       GENERAL_SETTINGS           { get; set; } = "";
    public string       CONFIG_VERSION             { get; set; } = "";
    public int          CrateCanForEatingCan       { get; set; }
    public string       FRYINGPAN_SETTINGS         { get; set; } = "";
    public List<string> Allowed_Items_to_FryingPan { get; set; } = new();
    public string       IRP_SETTINGS               { get; set; } = "";
    public List<string> IRP_Items                  { get; set; } = new();
    public List<FoodRecipe> RECIPES                { get; set; } = new();
}

public class FoodRecipe
{
    public string            SPECIAL_INGREDIENTS { get; set; } = "";
    public List<FoodIngredient> INGREDIENT_ITEMS { get; set; } = new();
    public string            RESULT              { get; set; } = "";
    public int               RESULT_QUANTITY     { get; set; } = 1;
}

public class FoodIngredient
{
    public string NAME             { get; set; } = "";
    public int    QUANTITY         { get; set; } = 1;
    public int    REQUIRE_BOILED   { get; set; }
    public int    REQUIRE_FRESH    { get; set; }
}

// ════════════════════════════════════════════════════════════════════════════
// GM_MineRockConfig
// ════════════════════════════════════════════════════════════════════════════

public class GMMineRockConfig
{
    public string           CONFIG_VERSION       { get; set; } = "";
    public string           TITLE                { get; set; } = "";
    public string           AUTHOR               { get; set; } = "";
    public string           DISCORD              { get; set; } = "";
    public MineGeneralSettings   GENERAL_SETTINGS    { get; set; } = new();
    public MineExtractionSettings EXTRACTION_SETTINGS { get; set; } = new();
    public MineDigSettings   DIG_SETTINGS        { get; set; } = new();
    public MineWashSettings  WASH_SETTINGS       { get; set; } = new();
    public List<MineRecipe>  RECIPES             { get; set; } = new();
}

public class MineGeneralSettings
{
    public double MELTING_TEMPERATURE { get; set; } = 500.0;
}

public class MineExtractionSettings
{
    public double        CHANCE_EXTRACTION_STONE    { get; set; }
    public double        CHANCE_EXTRACTION_COAL     { get; set; }
    public double        CHANCE_EXTRACTION_RARE     { get; set; }
    public List<string>  EXTRACTION_ARRAY           { get; set; } = new();
    public double        EXTRACTION_MINIMUM_QUANTITY { get; set; } = 0.2;
    public double        EXTRACTION_MAXIMUM_QUANTITY { get; set; } = 1.0;
    public int           DAMAGE_TO_TOOLS            { get; set; } = 10;
    public int           ROCK_BASE_MIN_QUANTITY     { get; set; }
    public int           ROCK_BASE_MAX_QUANTITY     { get; set; }
    public int           SAND_BASE_MIN_QUANTITY     { get; set; }
    public int           SAND_BASE_MAX_QUANTITY     { get; set; }
    public string        ABILITY_SETTINGS           { get; set; } = "";
    public double        BLACKSMITH_CHANCE_BONUS_LEVEL0  { get; set; }
    public double        BLACKSMITH_CHANCE_BONUS_LEVEL1  { get; set; }
    public double        BLACKSMITH_CHANCE_BONUS_LEVEL2  { get; set; }
    public double        BLACKSMITH_CHANCE_BONUS_LEVEL3  { get; set; }
    public double        BLACKSMITH_QUANTITY_BONUS_LEVEL0 { get; set; }
    public double        BLACKSMITH_QUANTITY_BONUS_LEVEL1 { get; set; }
    public double        BLACKSMITH_QUANTITY_BONUS_LEVEL2 { get; set; }
    public double        BLACKSMITH_QUANTITY_BONUS_LEVEL3 { get; set; }
}

public class MineDigSettings
{
    public double CHANCE_OF_DIG      { get; set; } = 0.5;
    public double CHANCE_OF_DIG_SAND { get; set; } = 0.5;
    public int    DAMAGE_TO_TOOLS    { get; set; } = 10;
}

public class MineWashSettings
{
    public double       CHANCE_FOR_WASH          { get; set; } = 0.1;
    public int          DAMAGE_TO_TOOLS          { get; set; } = 10;
    public List<string> WASH_ARRAY               { get; set; } = new();
    public double       WASHED_MINIMUM_QUANTITY  { get; set; } = 0.2;
    public double       WASHED_MAXIMUM_QUANTITY  { get; set; } = 1.0;
}

public class MineRecipe
{
    public string       RECIPE_NAME                { get; set; } = "";
    public List<string> INGREDIENTS                { get; set; } = new();
    public List<int>    QUANTITIES                 { get; set; } = new();
    public string       RESULT                     { get; set; } = "";
    public int          RESULT_QUANTITY            { get; set; } = 1;
    public string       SPECIAL_INGREDIENTS        { get; set; } = "";
    public int          DELETE_SPECIAL_INGREDIENTS { get; set; }
    public int          DAMAGE_SPECIAL_INGREDIENTS { get; set; } = 1;
    public int          DAMAGE_TO_INGREDIENT       { get; set; } = 20;
}

// ════════════════════════════════════════════════════════════════════════════
// ROCK_POINTS
// ════════════════════════════════════════════════════════════════════════════

public class RockPointsConfig
{
    public int            LOGS_ENABLE    { get; set; } = 1;
    public int            CLEAN_ON_START { get; set; } = 1;
    public List<RockObject> OBJECT_LIST  { get; set; } = new();
}

public class RockObject
{
    public string       CLASSNAME              { get; set; } = "";
    public int          MIN_OBJECTS_ON_MAP     { get; set; }
    public int          MAX_OBJECTS_ON_MAP     { get; set; } = 2;
    public List<string> POSITION               { get; set; } = new();
    public double       RESPAWN_TIME           { get; set; } = 120.0;
    public double       SPAWN_CHANCE           { get; set; } = 100.0;
    public int          CORRECT_TO_GROUND      { get; set; } = 1;
    public int          MIN_EXTRACTION_AMOUNT  { get; set; } = 1;
    public int          MAX_EXTRACTION_AMOUNT  { get; set; } = 2;
}


// ══════════════════════════════════════════════════════════════════════════════
// Логика вкладок Медицина / Еда / Майнинг
// ══════════════════════════════════════════════════════════════════════════════
public partial class MainWindow
{
    // ─── State ──────────────────────────────────────────────────────────────

    // Medicine (flat config)
    private List<FlatConfigRow> _medicineRows = new();
    private List<FlatConfigRow> _medicineAll  = new();
    private string? _medicinePath;

    // Ability
    private GMAbilityConfig? _abilityConfig;
    private string? _abilityPath;
    private bool _abilityLoading;

    // Liquid (flat config)
    private List<FlatConfigRow> _liquidRows = new();
    private List<FlatConfigRow> _liquidAll  = new();
    private string? _liquidPath;

    // Food
    private GMFoodConfig? _foodConfig;
    private string? _foodPath;
    private bool _foodLoading;

    // MineRock
    private GMMineRockConfig? _mineConfig;
    private string? _minePath;
    private bool _mineLoading;

    // Rock Points
    private RockPointsConfig? _rockConfig;
    private string? _rockPath;
    private bool _rockLoading;

    // Helper: ingredient pair for MineRock recipe grid
    private class IngredientRow
    {
        public string Name { get; set; } = "";
        public int    Qty  { get; set; } = 1;
    }

    // ─── Mods autocomplete init (called from InitNavigation) ────────────────

    private void InitModsAutoCompletes()
    {
        FoodFryCombo.ItemsSource    = _itemDatabase;
        FoodIRPCombo.ItemsSource    = _itemDatabase;
        FR_Result.ItemsSource       = _itemDatabase;
        FR_Special.ItemsSource      = _itemDatabase;
        FR_IngCombo.ItemsSource     = _itemDatabase;
        MineExtCombo.ItemsSource    = _itemDatabase;
        MineWashCombo.ItemsSource   = _itemDatabase;
        MR_Result.ItemsSource       = _itemDatabase;
        MR_Special.ItemsSource      = _itemDatabase;
        MR_IngCombo.ItemsSource     = _itemDatabase;
        RO_Class.ItemsSource        = _itemDatabase;
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS — плоский JSON → FlatConfigRow[]
    // ════════════════════════════════════════════════════════════════════════

    private static List<FlatConfigRow> LoadFlatConfig(string path)
    {
        var rows = new List<FlatConfigRow>();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var kind = prop.Value.ValueKind switch
            {
                JsonValueKind.Number => prop.Value.TryGetInt32(out _) ? "int" : "float",
                JsonValueKind.String => "string",
                JsonValueKind.Array  => "list",
                JsonValueKind.Object => "object",
                _                   => "string"
            };

            var val = prop.Value.ValueKind switch
            {
                JsonValueKind.Array  => $"[{prop.Value.GetArrayLength()} items]",
                JsonValueKind.Object => "{object}",
                _                   => prop.Value.ToString()
            };

            // Строки-разделители — секции
            // Секция если:
            // 1. Значение состоит из тире/пробелов (Medicine: "----------")
            // 2. Ключ заканчивается на _SETTINGS (Medicine/Liquid: ALCOHOL_SETTINGS)
            // 3. Ключ начинается с SETTINGS_ (Liquid: SETTINGS_FOR_LIQUID_OTVAR)
            // 4. Ключ — LEVEL_N (Medicine: LEVEL_0, LEVEL_1...)
            var isSep = kind == "string" && (
                val.All(ch => ch == '-' || ch == ' ') ||
                prop.Name.EndsWith("_SETTINGS", StringComparison.OrdinalIgnoreCase) ||
                prop.Name.StartsWith("SETTINGS_", StringComparison.OrdinalIgnoreCase) ||
                System.Text.RegularExpressions.Regex.IsMatch(prop.Name, @"^LEVEL_\d+$")
            );
            if (isSep) kind = "section";

            rows.Add(new FlatConfigRow { Key = prop.Name, Value = val, Kind = kind });
        }
        return rows;
    }

    private static void SaveFlatConfig(string path, List<FlatConfigRow> rows, string originalPath)
    {
        // Load original to preserve arrays/objects, only update scalar values
        var original = JsonDocument.Parse(File.ReadAllText(originalPath));
        var dict = new Dictionary<string, JsonElement>();
        foreach (var prop in original.RootElement.EnumerateObject())
            dict[prop.Name] = prop.Value;

        using var ms     = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();

        foreach (var row in rows)
        {
            if (!dict.TryGetValue(row.Key, out var orig))
            {
                writer.WriteString(row.Key, row.Value);
                continue;
            }

            switch (orig.ValueKind)
            {
                case JsonValueKind.Array:
                case JsonValueKind.Object:
                    writer.WritePropertyName(row.Key);
                    orig.WriteTo(writer);
                    break;
                case JsonValueKind.Number:
                    if (int.TryParse(row.Value, out var i))
                        writer.WriteNumber(row.Key, i);
                    else if (double.TryParse(row.Value, System.Globalization.NumberStyles.Any,
                             System.Globalization.CultureInfo.InvariantCulture, out var d))
                        writer.WriteNumber(row.Key, d);
                    else
                        writer.WriteString(row.Key, row.Value);
                    break;
                default:
                    writer.WriteString(row.Key, row.Value);
                    break;
            }
        }

        writer.WriteEndObject();
        writer.Flush();
        File.WriteAllBytes(path, ms.ToArray());
    }

    private List<string> GetSections(List<FlatConfigRow> rows)
        => rows.Where(r => r.Kind == "section").Select(r => r.Key).ToList();

    // ════════════════════════════════════════════════════════════════════════
    // MEDICINE CONFIG
    // ════════════════════════════════════════════════════════════════════════

    private void OpenMedicineConfig_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json", FileName = "GM_MEDICINE_CONFIG.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _medicinePath = dlg.FileName;
            _medicineAll  = LoadFlatConfig(_medicinePath);
            LoadMedicineGrid(_medicineAll);

            // Populate section filter
            MedicineSectionCombo.ItemsSource = new[] { "Все" }.Concat(GetSections(_medicineAll)).ToList();
            MedicineSectionCombo.SelectedIndex = 0;
            SetStatus($"GM_MEDICINE_CONFIG загружен: {_medicineAll.Count} ключей");
            _autoSave.Register("medicine", () => { if (_medicinePath != null) SaveFlatConfig(_medicinePath, _medicineAll, _medicinePath); });
            _autoSave.MarkClean("medicine");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    private void SaveMedicineConfig_Click(object sender, RoutedEventArgs e)
    {
        if (_medicinePath == null || _medicineAll.Count == 0) return;
        try
        {
            SaveFlatConfig(_medicinePath, _medicineAll, _medicinePath);
            SetStatus("GM_MEDICINE_CONFIG сохранён");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    private void LoadMedicineGrid(List<FlatConfigRow> rows)
    {
        _medicineRows = rows;
        MedicineGrid.ItemsSource = null;
        MedicineGrid.ItemsSource = _medicineRows;
    }

    private void MedicineSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        => FilterMedicineGrid();

    private void MedicineSectionCombo_Changed(object sender, SelectionChangedEventArgs e)
        => FilterMedicineGrid();

    private void FilterMedicineGrid()
    {
        var q    = MedicineSearchBox.Text?.Trim() ?? "";
        var sect = MedicineSectionCombo.SelectedItem?.ToString() ?? "Все";

        IEnumerable<FlatConfigRow> rows = _medicineAll;

        // Section filter: show from section header to next section
        if (sect != "Все")
        {
            var inSect  = false;
            var filtered = new List<FlatConfigRow>();
            foreach (var row in _medicineAll)
            {
                if (row.Kind == "section" && row.Key == sect)  { inSect = true; filtered.Add(row); continue; }
                if (row.Kind == "section" && inSect)           { break; }
                if (inSect)                                    { filtered.Add(row); }
            }
            rows = filtered;
        }

        if (!string.IsNullOrWhiteSpace(q))
            rows = rows.Where(r => r.Key.Contains(q, StringComparison.OrdinalIgnoreCase));

        LoadMedicineGrid(rows.ToList());
    }

    private void MedicineGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        _autoSave.MarkDirty("medicine");
        if (e.Row.Item is not FlatConfigRow row) return;
        if (e.EditingElement is not TextBox tb) return;
        // Find in master list and update
        var master = _medicineAll.FirstOrDefault(r => r.Key == row.Key);
        if (master != null) master.Value = tb.Text;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ABILITY CONFIG
    // ════════════════════════════════════════════════════════════════════════

    private void OpenAbilityConfig_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json", FileName = "GM_ABILITY_CONFIG.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _abilityPath   = dlg.FileName;
            _abilityConfig = JsonService.Load<GMAbilityConfig>(_abilityPath);
            LoadAbilityFields();
            SetStatus("GM_ABILITY_CONFIG загружен");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    private void SaveAbilityConfig_Click(object sender, RoutedEventArgs e)
    {
        if (_abilityConfig == null || _abilityPath == null) return;
        JsonService.Save(_abilityPath, _abilityConfig);
        SetStatus("GM_ABILITY_CONFIG сохранён");
    }

    private void LoadAbilityFields()
    {
        _abilityLoading = true;
        try
        {
            if (_abilityConfig == null) return;
            AB_Version.Text     = _abilityConfig.CONFIG_VERSION;
            AB_Map.Text         = _abilityConfig.MAP;
            AB_SaveOnDeath.Text  = _abilityConfig.SaveAbilityPointsOnDeath.ToString();
            AB_EnablePeriod.Text = _abilityConfig.Enable_PeriodSave.ToString();
            AB_PeriodSave.Text   = _abilityConfig.Period_OnlineAutoSave.ToString();
        }
        finally { _abilityLoading = false; }
    }

    private void AbilityField_Changed(object sender, TextChangedEventArgs e)
    {
        _autoSave.MarkDirty("ability");
        if (_abilityLoading || _abilityConfig == null) return;
        if (sender is not TextBox tb) return;
        switch (tb.Tag?.ToString())
        {
            case "CONFIG_VERSION":         _abilityConfig.CONFIG_VERSION           = tb.Text; break;
            case "MAP":                    _abilityConfig.MAP                      = tb.Text; break;
            case "SaveAbilityPointsOnDeath": if (int.TryParse(tb.Text, out var v1)) _abilityConfig.SaveAbilityPointsOnDeath = v1; break;
            case "Enable_PeriodSave":      if (int.TryParse(tb.Text, out var v2)) _abilityConfig.Enable_PeriodSave         = v2; break;
            case "Period_OnlineAutoSave":  if (int.TryParse(tb.Text, out var v3)) _abilityConfig.Period_OnlineAutoSave     = v3; break;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // LIQUID CONFIG
    // ════════════════════════════════════════════════════════════════════════

    private void OpenLiquidConfig_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json", FileName = "GM_LiquidConfig.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _liquidPath = dlg.FileName;
            _liquidAll  = LoadFlatConfig(_liquidPath);

            LiquidSectionCombo.ItemsSource = new[] { "Все" }.Concat(GetSections(_liquidAll)).ToList();
            LiquidSectionCombo.SelectedIndex = 0;
            FilterLiquidGrid();
            SetStatus($"GM_LiquidConfig загружен: {_liquidAll.Count} ключей");
            _autoSave.Register("liquid", () => { if (_liquidPath != null) SaveFlatConfig(_liquidPath, _liquidAll, _liquidPath); });
            _autoSave.MarkClean("liquid");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    private void SaveLiquidConfig_Click(object sender, RoutedEventArgs e)
    {
        if (_liquidPath == null || _liquidAll.Count == 0) return;
        try
        {
            SaveFlatConfig(_liquidPath, _liquidAll, _liquidPath);
            SetStatus("GM_LiquidConfig сохранён");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    private void LiquidSearchBox_TextChanged(object sender, TextChangedEventArgs e) => FilterLiquidGrid();
    private void LiquidSectionCombo_Changed(object sender, SelectionChangedEventArgs e) => FilterLiquidGrid();

    private void FilterLiquidGrid()
    {
        var q    = LiquidSearchBox.Text?.Trim() ?? "";
        var sect = LiquidSectionCombo.SelectedItem?.ToString() ?? "Все";

        IEnumerable<FlatConfigRow> rows = _liquidAll;
        if (sect != "Все")
        {
            var inSect   = false;
            var filtered = new List<FlatConfigRow>();
            foreach (var row in _liquidAll)
            {
                if (row.Kind == "section" && row.Key == sect) { inSect = true; filtered.Add(row); continue; }
                if (row.Kind == "section" && inSect)          { break; }
                if (inSect)                                   { filtered.Add(row); }
            }
            rows = filtered;
        }
        if (!string.IsNullOrWhiteSpace(q))
            rows = rows.Where(r => r.Key.Contains(q, StringComparison.OrdinalIgnoreCase));

        _liquidRows = rows.ToList();
        LiquidGrid.ItemsSource = null;
        LiquidGrid.ItemsSource = _liquidRows;
    }

    private void LiquidGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        _autoSave.MarkDirty("liquid");
        if (e.Row.Item is not FlatConfigRow row) return;
        if (e.EditingElement is not TextBox tb) return;
        var master = _liquidAll.FirstOrDefault(r => r.Key == row.Key);
        if (master != null) master.Value = tb.Text;
    }

    // ════════════════════════════════════════════════════════════════════════
    // FOOD CONFIG
    // ════════════════════════════════════════════════════════════════════════

    private void OpenFoodConfig_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json", FileName = "GM_FoodConfig.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _foodPath   = dlg.FileName;
            _foodConfig = JsonService.Load<GMFoodConfig>(_foodPath);
            LoadFoodFields();
            SetStatus($"GM_FoodConfig загружен: рецептов {_foodConfig.RECIPES.Count}");
            _autoSave.Register("food", () => { if (_foodPath != null) JsonService.Save(_foodPath, _foodConfig!); });
            _autoSave.MarkClean("food");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    private void SaveFoodConfig_Click(object sender, RoutedEventArgs e)
    {
        if (_foodConfig == null || _foodPath == null) return;
        JsonService.Save(_foodPath, _foodConfig);
        SetStatus("GM_FoodConfig сохранён");
    }

    private void LoadFoodFields()
    {
        if (_foodConfig == null) return;
        _foodLoading = true;
        try
        {
            Food_CrateCan.Text = _foodConfig.CrateCanForEatingCan.ToString();
            Food_Version.Text  = _foodConfig.CONFIG_VERSION;
        }
        finally { _foodLoading = false; }

        FoodFryList.ItemsSource    = _foodConfig.Allowed_Items_to_FryingPan;
        FoodIRPList.ItemsSource    = _foodConfig.IRP_Items;
        FoodRecipesList.ItemsSource = _foodConfig.RECIPES;
    }

    private void FoodField_Changed(object sender, TextChangedEventArgs e)
    {
        _autoSave.MarkDirty("food");
        if (_foodLoading || _foodConfig == null) return;
        if (sender is not TextBox tb) return;
        if (tb.Tag?.ToString() == "CrateCanForEatingCan" && int.TryParse(tb.Text, out var v))
            _foodConfig.CrateCanForEatingCan = v;
    }

    // FryingPan
    private void FoodFryCombo_PreviewKeyUp(object sender, KeyEventArgs e)
        => FilterComboBox(FoodFryCombo, FoodFryCombo.Text?.Trim() ?? "");

    private void AddFoodFryItem_Click(object sender, RoutedEventArgs e)
    {
        if (_foodConfig == null) return;
        var val = FoodFryCombo.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(val)) return;
        _foodConfig.Allowed_Items_to_FryingPan.Add(val);
        FoodFryCombo.Text = "";
        FoodFryList.ItemsSource = null; FoodFryList.ItemsSource = _foodConfig.Allowed_Items_to_FryingPan;
    }
    private void DeleteFoodFryItem_Click(object sender, RoutedEventArgs e)
    {
        if (_foodConfig == null || FoodFryList.SelectedItem is not string item) return;
        _foodConfig.Allowed_Items_to_FryingPan.Remove(item);
        FoodFryList.ItemsSource = null; FoodFryList.ItemsSource = _foodConfig.Allowed_Items_to_FryingPan;
    }

    // IRP Items
    private void FoodIRPCombo_PreviewKeyUp(object sender, KeyEventArgs e)
        => FilterComboBox(FoodIRPCombo, FoodIRPCombo.Text?.Trim() ?? "");
    private void AddFoodIRPItem_Click(object sender, RoutedEventArgs e)
    {
        if (_foodConfig == null) return;
        var val = FoodIRPCombo.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(val)) return;
        _foodConfig.IRP_Items.Add(val);
        FoodIRPCombo.Text = "";
        FoodIRPList.ItemsSource = null; FoodIRPList.ItemsSource = _foodConfig.IRP_Items;
    }
    private void DeleteFoodIRPItem_Click(object sender, RoutedEventArgs e)
    {
        if (_foodConfig == null || FoodIRPList.SelectedItem is not string item) return;
        _foodConfig.IRP_Items.Remove(item);
        FoodIRPList.ItemsSource = null; FoodIRPList.ItemsSource = _foodConfig.IRP_Items;
    }

    // Food Recipes
    private void AddFoodRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (_foodConfig == null) return;
        _foodConfig.RECIPES.Add(new FoodRecipe { RESULT = "new_item", RESULT_QUANTITY = 1 });
        FoodRecipesList.ItemsSource = null; FoodRecipesList.ItemsSource = _foodConfig.RECIPES;
    }
    private void DeleteFoodRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (_foodConfig == null || FoodRecipesList.SelectedItem is not FoodRecipe r) return;
        _foodConfig.RECIPES.Remove(r);
        FoodRecipesList.ItemsSource = null; FoodRecipesList.ItemsSource = _foodConfig.RECIPES;
    }
    private void FoodRecipesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var r = FoodRecipesList.SelectedItem as FoodRecipe;
        FoodRecipeDetail.IsEnabled = r != null;
        if (r == null) return;
        _foodLoading = true;
        FR_Result.Text  = r.RESULT;
        FR_Special.Text = r.SPECIAL_INGREDIENTS;
        FR_Qty.Text     = r.RESULT_QUANTITY.ToString();
        FoodIngredientsGrid.ItemsSource = r.INGREDIENT_ITEMS;
        _foodLoading = false;
    }
    private void FoodRecipeField_Changed(object sender, TextChangedEventArgs e)
    {
        if (_foodLoading || FoodRecipesList.SelectedItem is not FoodRecipe r) return;
        if (sender is not TextBox tb) return;
        if (tb.Tag?.ToString() == "RESULT_QUANTITY" && int.TryParse(tb.Text, out var v)) r.RESULT_QUANTITY = v;
    }
    private void FR_Result_PreviewKeyUp(object sender, KeyEventArgs e) => FilterComboBox(FR_Result, FR_Result.Text?.Trim() ?? "");
    private void FR_Result_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FR_Result.SelectedItem is not ItemDatabaseEntry item) return;
        if (FoodRecipesList.SelectedItem is FoodRecipe r) r.RESULT = item.ClassName;
        FR_Result.Text = item.ClassName;
    }
    private void FR_Special_PreviewKeyUp(object sender, KeyEventArgs e) => FilterComboBox(FR_Special, FR_Special.Text?.Trim() ?? "");
    private void FR_Special_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FR_Special.SelectedItem is not ItemDatabaseEntry item) return;
        if (FoodRecipesList.SelectedItem is FoodRecipe r) r.SPECIAL_INGREDIENTS = item.ClassName;
        FR_Special.Text = item.ClassName;
    }
    private void FR_IngCombo_PreviewKeyUp(object sender, KeyEventArgs e) => FilterComboBox(FR_IngCombo, FR_IngCombo.Text?.Trim() ?? "");
    private void AddFoodIngredient_Click(object sender, RoutedEventArgs e)
    {
        if (FoodRecipesList.SelectedItem is not FoodRecipe r) return;
        var val = FR_IngCombo.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(val)) return;
        r.INGREDIENT_ITEMS.Add(new FoodIngredient { NAME = val, QUANTITY = 1 });
        FR_IngCombo.Text = "";
        FoodIngredientsGrid.ItemsSource = null; FoodIngredientsGrid.ItemsSource = r.INGREDIENT_ITEMS;
    }
    private void DeleteFoodIngredient_Click(object sender, RoutedEventArgs e)
    {
        if (FoodRecipesList.SelectedItem is not FoodRecipe r) return;
        if (FoodIngredientsGrid.SelectedItem is not FoodIngredient ing) return;
        r.INGREDIENT_ITEMS.Remove(ing);
        FoodIngredientsGrid.ItemsSource = null; FoodIngredientsGrid.ItemsSource = r.INGREDIENT_ITEMS;
    }

    // ════════════════════════════════════════════════════════════════════════
    // MINEROCK CONFIG
    // ════════════════════════════════════════════════════════════════════════

    private void OpenMineRockConfig_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json", FileName = "GM_MineRockConfig.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _minePath   = dlg.FileName;
            _mineConfig = JsonService.Load<GMMineRockConfig>(_minePath);
            LoadMineFields();
            SetStatus($"GM_MineRockConfig загружен: рецептов {_mineConfig.RECIPES.Count}");
            _autoSave.Register("mine", () => { if (_minePath != null) JsonService.Save(_minePath, _mineConfig!); });
            _autoSave.MarkClean("mine");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    private void SaveMineRockConfig_Click(object sender, RoutedEventArgs e)
    {
        if (_mineConfig == null || _minePath == null) return;
        JsonService.Save(_minePath, _mineConfig);
        SetStatus("GM_MineRockConfig сохранён");
    }

    private void LoadMineFields()
    {
        if (_mineConfig == null) return;
        _mineLoading = true;
        try
        {
            var ex = _mineConfig.EXTRACTION_SETTINGS;
            var di = _mineConfig.DIG_SETTINGS;
            var wa = _mineConfig.WASH_SETTINGS;

            Mine_MeltTemp.Text   = _mineConfig.GENERAL_SETTINGS.MELTING_TEMPERATURE.ToString();
            Mine_ChanceStone.Text = ex.CHANCE_EXTRACTION_STONE.ToString();
            Mine_ChanceCoal.Text  = ex.CHANCE_EXTRACTION_COAL.ToString();
            Mine_ChanceRare.Text  = ex.CHANCE_EXTRACTION_RARE.ToString();
            Mine_DmgTools.Text    = ex.DAMAGE_TO_TOOLS.ToString();
            Mine_ExtMin.Text      = ex.EXTRACTION_MINIMUM_QUANTITY.ToString();
            Mine_ExtMax.Text      = ex.EXTRACTION_MAXIMUM_QUANTITY.ToString();
            Mine_RockMin.Text     = ex.ROCK_BASE_MIN_QUANTITY.ToString();
            Mine_RockMax.Text     = ex.ROCK_BASE_MAX_QUANTITY.ToString();
            Mine_BC0.Text = ex.BLACKSMITH_CHANCE_BONUS_LEVEL0.ToString();
            Mine_BC1.Text = ex.BLACKSMITH_CHANCE_BONUS_LEVEL1.ToString();
            Mine_BC2.Text = ex.BLACKSMITH_CHANCE_BONUS_LEVEL2.ToString();
            Mine_BC3.Text = ex.BLACKSMITH_CHANCE_BONUS_LEVEL3.ToString();
            Mine_BQ0.Text = ex.BLACKSMITH_QUANTITY_BONUS_LEVEL0.ToString();
            Mine_BQ1.Text = ex.BLACKSMITH_QUANTITY_BONUS_LEVEL1.ToString();
            Mine_BQ2.Text = ex.BLACKSMITH_QUANTITY_BONUS_LEVEL2.ToString();
            Mine_BQ3.Text = ex.BLACKSMITH_QUANTITY_BONUS_LEVEL3.ToString();
            Mine_DigChance.Text = di.CHANCE_OF_DIG.ToString();
            Mine_DigSand.Text   = di.CHANCE_OF_DIG_SAND.ToString();
            Mine_DigDmg.Text    = di.DAMAGE_TO_TOOLS.ToString();
            Mine_WashChance.Text = wa.CHANCE_FOR_WASH.ToString();
            Mine_WashMin.Text    = wa.WASHED_MINIMUM_QUANTITY.ToString();
            Mine_WashMax.Text    = wa.WASHED_MAXIMUM_QUANTITY.ToString();
        }
        finally { _mineLoading = false; }

        MineExtList.ItemsSource  = _mineConfig.EXTRACTION_SETTINGS.EXTRACTION_ARRAY;
        MineWashList.ItemsSource = _mineConfig.WASH_SETTINGS.WASH_ARRAY;
        MineRecipesList.ItemsSource = _mineConfig.RECIPES;
    }

    private void MineField_Changed(object sender, TextChangedEventArgs e)
    {
        _autoSave.MarkDirty("mine");
        if (_mineLoading || _mineConfig == null) return;
        if (sender is not TextBox tb) return;
        var ex = _mineConfig.EXTRACTION_SETTINGS;
        var di = _mineConfig.DIG_SETTINGS;
        var wa = _mineConfig.WASH_SETTINGS;
        double d; int i;
        switch (tb.Tag?.ToString())
        {
            case "MELTING_TEMPERATURE":         if (double.TryParse(tb.Text, out d)) _mineConfig.GENERAL_SETTINGS.MELTING_TEMPERATURE = d; break;
            case "CHANCE_EXTRACTION_STONE":     if (double.TryParse(tb.Text, out d)) ex.CHANCE_EXTRACTION_STONE = d; break;
            case "CHANCE_EXTRACTION_COAL":      if (double.TryParse(tb.Text, out d)) ex.CHANCE_EXTRACTION_COAL = d; break;
            case "CHANCE_EXTRACTION_RARE":      if (double.TryParse(tb.Text, out d)) ex.CHANCE_EXTRACTION_RARE = d; break;
            case "EX_DAMAGE_TO_TOOLS":          if (int.TryParse(tb.Text, out i)) ex.DAMAGE_TO_TOOLS = i; break;
            case "EXTRACTION_MINIMUM_QUANTITY": if (double.TryParse(tb.Text, out d)) ex.EXTRACTION_MINIMUM_QUANTITY = d; break;
            case "EXTRACTION_MAXIMUM_QUANTITY": if (double.TryParse(tb.Text, out d)) ex.EXTRACTION_MAXIMUM_QUANTITY = d; break;
            case "ROCK_BASE_MIN_QUANTITY":      if (int.TryParse(tb.Text, out i)) ex.ROCK_BASE_MIN_QUANTITY = i; break;
            case "ROCK_BASE_MAX_QUANTITY":      if (int.TryParse(tb.Text, out i)) ex.ROCK_BASE_MAX_QUANTITY = i; break;
            case "BLACKSMITH_CHANCE_BONUS_LEVEL0": if (double.TryParse(tb.Text, out d)) ex.BLACKSMITH_CHANCE_BONUS_LEVEL0 = d; break;
            case "BLACKSMITH_CHANCE_BONUS_LEVEL1": if (double.TryParse(tb.Text, out d)) ex.BLACKSMITH_CHANCE_BONUS_LEVEL1 = d; break;
            case "BLACKSMITH_CHANCE_BONUS_LEVEL2": if (double.TryParse(tb.Text, out d)) ex.BLACKSMITH_CHANCE_BONUS_LEVEL2 = d; break;
            case "BLACKSMITH_CHANCE_BONUS_LEVEL3": if (double.TryParse(tb.Text, out d)) ex.BLACKSMITH_CHANCE_BONUS_LEVEL3 = d; break;
            case "BLACKSMITH_QUANTITY_BONUS_LEVEL0": if (double.TryParse(tb.Text, out d)) ex.BLACKSMITH_QUANTITY_BONUS_LEVEL0 = d; break;
            case "BLACKSMITH_QUANTITY_BONUS_LEVEL1": if (double.TryParse(tb.Text, out d)) ex.BLACKSMITH_QUANTITY_BONUS_LEVEL1 = d; break;
            case "BLACKSMITH_QUANTITY_BONUS_LEVEL2": if (double.TryParse(tb.Text, out d)) ex.BLACKSMITH_QUANTITY_BONUS_LEVEL2 = d; break;
            case "BLACKSMITH_QUANTITY_BONUS_LEVEL3": if (double.TryParse(tb.Text, out d)) ex.BLACKSMITH_QUANTITY_BONUS_LEVEL3 = d; break;
            case "CHANCE_OF_DIG":      if (double.TryParse(tb.Text, out d)) di.CHANCE_OF_DIG = d; break;
            case "CHANCE_OF_DIG_SAND": if (double.TryParse(tb.Text, out d)) di.CHANCE_OF_DIG_SAND = d; break;
            case "DIG_DAMAGE_TO_TOOLS":if (int.TryParse(tb.Text, out i)) di.DAMAGE_TO_TOOLS = i; break;
            case "CHANCE_FOR_WASH":         if (double.TryParse(tb.Text, out d)) wa.CHANCE_FOR_WASH = d; break;
            case "WASHED_MINIMUM_QUANTITY": if (double.TryParse(tb.Text, out d)) wa.WASHED_MINIMUM_QUANTITY = d; break;
            case "WASHED_MAXIMUM_QUANTITY": if (double.TryParse(tb.Text, out d)) wa.WASHED_MAXIMUM_QUANTITY = d; break;
        }
    }

    // Extraction Array
    private void MineExtCombo_PreviewKeyUp(object sender, KeyEventArgs e) => FilterComboBox(MineExtCombo, MineExtCombo.Text?.Trim() ?? "");
    private void AddMineExtItem_Click(object sender, RoutedEventArgs e)
    {
        if (_mineConfig == null) return;
        var val = MineExtCombo.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(val)) return;
        _mineConfig.EXTRACTION_SETTINGS.EXTRACTION_ARRAY.Add(val);
        MineExtCombo.Text = "";
        MineExtList.ItemsSource = null; MineExtList.ItemsSource = _mineConfig.EXTRACTION_SETTINGS.EXTRACTION_ARRAY;
    }
    private void DeleteMineExtItem_Click(object sender, RoutedEventArgs e)
    {
        if (_mineConfig == null || MineExtList.SelectedItem is not string item) return;
        _mineConfig.EXTRACTION_SETTINGS.EXTRACTION_ARRAY.Remove(item);
        MineExtList.ItemsSource = null; MineExtList.ItemsSource = _mineConfig.EXTRACTION_SETTINGS.EXTRACTION_ARRAY;
    }

    // Wash Array
    private void MineWashCombo_PreviewKeyUp(object sender, KeyEventArgs e) => FilterComboBox(MineWashCombo, MineWashCombo.Text?.Trim() ?? "");
    private void AddMineWashItem_Click(object sender, RoutedEventArgs e)
    {
        if (_mineConfig == null) return;
        var val = MineWashCombo.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(val)) return;
        _mineConfig.WASH_SETTINGS.WASH_ARRAY.Add(val);
        MineWashCombo.Text = "";
        MineWashList.ItemsSource = null; MineWashList.ItemsSource = _mineConfig.WASH_SETTINGS.WASH_ARRAY;
    }
    private void DeleteMineWashItem_Click(object sender, RoutedEventArgs e)
    {
        if (_mineConfig == null || MineWashList.SelectedItem is not string item) return;
        _mineConfig.WASH_SETTINGS.WASH_ARRAY.Remove(item);
        MineWashList.ItemsSource = null; MineWashList.ItemsSource = _mineConfig.WASH_SETTINGS.WASH_ARRAY;
    }

    // Mine Recipes
    private void AddMineRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (_mineConfig == null) return;
        _mineConfig.RECIPES.Add(new MineRecipe { RECIPE_NAME = "Новый рецепт", RESULT_QUANTITY = 1, DAMAGE_SPECIAL_INGREDIENTS = 1, DAMAGE_TO_INGREDIENT = 20 });
        MineRecipesList.ItemsSource = null; MineRecipesList.ItemsSource = _mineConfig.RECIPES;
    }
    private void DeleteMineRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (_mineConfig == null || MineRecipesList.SelectedItem is not MineRecipe r) return;
        _mineConfig.RECIPES.Remove(r);
        MineRecipesList.ItemsSource = null; MineRecipesList.ItemsSource = _mineConfig.RECIPES;
    }
    private void MineRecipesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var r = MineRecipesList.SelectedItem as MineRecipe;
        MineRecipeDetail.IsEnabled = r != null;
        if (r == null) return;
        _mineLoading = true;
        MR_Name.Text       = r.RECIPE_NAME;
        MR_Result.Text     = r.RESULT;
        MR_ResultQty.Text  = r.RESULT_QUANTITY.ToString();
        MR_Special.Text    = r.SPECIAL_INGREDIENTS;
        MR_DelSpecial.Text = r.DELETE_SPECIAL_INGREDIENTS.ToString();
        MR_DmgSpecial.Text = r.DAMAGE_SPECIAL_INGREDIENTS.ToString();
        MR_DmgIng.Text     = r.DAMAGE_TO_INGREDIENT.ToString();
        _mineLoading = false;

        // Build ingredient rows
        var rows = r.INGREDIENTS.Zip(r.QUANTITIES, (n, q) => new IngredientRow { Name = n, Qty = q }).ToList();
        MineIngredientsGrid.ItemsSource = rows;
    }
    private void MineRecipeField_Changed(object sender, TextChangedEventArgs e)
    {
        if (_mineLoading || MineRecipesList.SelectedItem is not MineRecipe r) return;
        if (sender is not TextBox tb) return;
        int i;
        switch (tb.Tag?.ToString())
        {
            case "RECIPE_NAME":                r.RECIPE_NAME = tb.Text; MineRecipesList.Items.Refresh(); break;
            case "RESULT_QUANTITY":            if (int.TryParse(tb.Text, out i)) r.RESULT_QUANTITY = i; break;
            case "DELETE_SPECIAL_INGREDIENTS": if (int.TryParse(tb.Text, out i)) r.DELETE_SPECIAL_INGREDIENTS = i; break;
            case "DAMAGE_SPECIAL_INGREDIENTS": if (int.TryParse(tb.Text, out i)) r.DAMAGE_SPECIAL_INGREDIENTS = i; break;
            case "DAMAGE_TO_INGREDIENT":       if (int.TryParse(tb.Text, out i)) r.DAMAGE_TO_INGREDIENT = i; break;
        }
    }
    private void MR_Result_PreviewKeyUp(object sender, KeyEventArgs e) => FilterComboBox(MR_Result, MR_Result.Text?.Trim() ?? "");
    private void MR_Result_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MR_Result.SelectedItem is not ItemDatabaseEntry item) return;
        if (MineRecipesList.SelectedItem is MineRecipe r) r.RESULT = item.ClassName;
        MR_Result.Text = item.ClassName;
    }
    private void MR_Special_PreviewKeyUp(object sender, KeyEventArgs e) => FilterComboBox(MR_Special, MR_Special.Text?.Trim() ?? "");
    private void MR_Special_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MR_Special.SelectedItem is not ItemDatabaseEntry item) return;
        if (MineRecipesList.SelectedItem is MineRecipe r) r.SPECIAL_INGREDIENTS = item.ClassName;
        MR_Special.Text = item.ClassName;
    }
    private void MR_IngCombo_PreviewKeyUp(object sender, KeyEventArgs e) => FilterComboBox(MR_IngCombo, MR_IngCombo.Text?.Trim() ?? "");
    private void AddMineIngredient_Click(object sender, RoutedEventArgs e)
    {
        if (MineRecipesList.SelectedItem is not MineRecipe r) return;
        var name = MR_IngCombo.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name)) return;
        int qty = int.TryParse(MR_IngQty.Text, out var q) ? q : 1;
        r.INGREDIENTS.Add(name); r.QUANTITIES.Add(qty);
        MR_IngCombo.Text = ""; MR_IngQty.Text = "";
        var rows = r.INGREDIENTS.Zip(r.QUANTITIES, (n, qv) => new IngredientRow { Name = n, Qty = qv }).ToList();
        MineIngredientsGrid.ItemsSource = null; MineIngredientsGrid.ItemsSource = rows;
    }
    private void DeleteMineIngredient_Click(object sender, RoutedEventArgs e)
    {
        if (MineRecipesList.SelectedItem is not MineRecipe r) return;
        if (MineIngredientsGrid.SelectedItem is not IngredientRow row) return;
        var idx = r.INGREDIENTS.IndexOf(row.Name);
        if (idx >= 0) { r.INGREDIENTS.RemoveAt(idx); r.QUANTITIES.RemoveAt(idx); }
        var rows = r.INGREDIENTS.Zip(r.QUANTITIES, (n, q) => new IngredientRow { Name = n, Qty = q }).ToList();
        MineIngredientsGrid.ItemsSource = null; MineIngredientsGrid.ItemsSource = rows;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ROCK POINTS
    // ════════════════════════════════════════════════════════════════════════

    private void OpenRockPoints_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON|*.json", FileName = "ROCK_POINTS.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _rockPath   = dlg.FileName;
            _rockConfig = JsonService.Load<RockPointsConfig>(_rockPath);
            RockObjectsList.ItemsSource = _rockConfig.OBJECT_LIST;
            SetStatus($"ROCK_POINTS загружен: объектов {_rockConfig.OBJECT_LIST.Count}");
            _autoSave.Register("rock", () => { if (_rockPath != null) JsonService.Save(_rockPath, _rockConfig!); });
            _autoSave.MarkClean("rock");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    private void SaveRockPoints_Click(object sender, RoutedEventArgs e)
    {
        if (_rockConfig == null || _rockPath == null) return;
        JsonService.Save(_rockPath, _rockConfig);
        SetStatus("ROCK_POINTS сохранён");
    }

    private void AddRockObject_Click(object sender, RoutedEventArgs e)
    {
        if (_rockConfig == null) { SetStatus("Сначала открой ROCK_POINTS.json"); return; }
        var obj = new RockObject { CLASSNAME = "GM_NewROCK", MAX_OBJECTS_ON_MAP = 2, RESPAWN_TIME = 120, SPAWN_CHANCE = 100 };
        _rockConfig.OBJECT_LIST.Add(obj);
        RockObjectsList.ItemsSource = null; RockObjectsList.ItemsSource = _rockConfig.OBJECT_LIST;
        RockObjectsList.SelectedItem = obj;
    }
    private void DeleteRockObject_Click(object sender, RoutedEventArgs e)
    {
        if (_rockConfig == null || RockObjectsList.SelectedItem is not RockObject obj) return;
        _rockConfig.OBJECT_LIST.Remove(obj);
        RockObjectsList.ItemsSource = null; RockObjectsList.ItemsSource = _rockConfig.OBJECT_LIST;
    }
    private void RockObjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var obj = RockObjectsList.SelectedItem as RockObject;
        RockObjectDetail.IsEnabled = obj != null;
        if (obj == null) return;
        _rockLoading = true;
        RO_Class.Text        = obj.CLASSNAME;
        RO_Min.Text          = obj.MIN_OBJECTS_ON_MAP.ToString();
        RO_Max.Text          = obj.MAX_OBJECTS_ON_MAP.ToString();
        RO_Respawn.Text      = obj.RESPAWN_TIME.ToString();
        RO_SpawnChance.Text  = obj.SPAWN_CHANCE.ToString();
        RO_ExtMin.Text       = obj.MIN_EXTRACTION_AMOUNT.ToString();
        RO_ExtMax.Text       = obj.MAX_EXTRACTION_AMOUNT.ToString();
        _rockLoading = false;
        RockPosList.ItemsSource = obj.POSITION;
    }
    private void RockField_Changed(object sender, TextChangedEventArgs e)
    {
        _autoSave.MarkDirty("rock");
        if (_rockLoading || RockObjectsList.SelectedItem is not RockObject obj) return;
        if (sender is not TextBox tb) return;
        int i; double d;
        switch (tb.Tag?.ToString())
        {
            case "MIN_OBJECTS_ON_MAP":    if (int.TryParse(tb.Text, out i)) obj.MIN_OBJECTS_ON_MAP = i; break;
            case "MAX_OBJECTS_ON_MAP":    if (int.TryParse(tb.Text, out i)) obj.MAX_OBJECTS_ON_MAP = i; break;
            case "RESPAWN_TIME":          if (double.TryParse(tb.Text, out d)) obj.RESPAWN_TIME = d; break;
            case "SPAWN_CHANCE":          if (double.TryParse(tb.Text, out d)) obj.SPAWN_CHANCE = d; break;
            case "MIN_EXTRACTION_AMOUNT": if (int.TryParse(tb.Text, out i)) obj.MIN_EXTRACTION_AMOUNT = i; break;
            case "MAX_EXTRACTION_AMOUNT": if (int.TryParse(tb.Text, out i)) obj.MAX_EXTRACTION_AMOUNT = i; break;
        }
    }
    private void RO_Class_PreviewKeyUp(object sender, KeyEventArgs e) => FilterComboBox(RO_Class, RO_Class.Text?.Trim() ?? "");
    private void RO_Class_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RO_Class.SelectedItem is not ItemDatabaseEntry item) return;
        if (RockObjectsList.SelectedItem is RockObject obj)
        {
            obj.CLASSNAME = item.ClassName;
            RockObjectsList.Items.Refresh();
        }
        RO_Class.Text = item.ClassName;
    }
    private void AddRockPos_Click(object sender, RoutedEventArgs e)
    {
        if (RockObjectsList.SelectedItem is not RockObject obj) return;
        var x = RO_PosX.Text.Trim(); var y = RO_PosY.Text.Trim(); var z = RO_PosZ.Text.Trim();
        if (string.IsNullOrWhiteSpace(x) || string.IsNullOrWhiteSpace(y) || string.IsNullOrWhiteSpace(z))
        { SetStatus("Заполни X, Y, Z"); return; }
        obj.POSITION.Add($"{x} {y} {z}");
        RO_PosX.Clear(); RO_PosY.Clear(); RO_PosZ.Clear();
        RockPosList.ItemsSource = null; RockPosList.ItemsSource = obj.POSITION;
    }
    private void DeleteRockPos_Click(object sender, RoutedEventArgs e)
    {
        if (RockObjectsList.SelectedItem is not RockObject obj) return;
        if (RockPosList.SelectedItem is not string pos) return;
        obj.POSITION.Remove(pos);
        RockPosList.ItemsSource = null; RockPosList.ItemsSource = obj.POSITION;
    }
}
