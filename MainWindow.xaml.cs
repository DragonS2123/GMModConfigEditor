using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using GMCraftTableEditor.Models;
using GMCraftTableEditor.Services;

namespace GMCraftTableEditor;

public partial class MainWindow : Window
{
    // ─── State ──────────────────────────────────────────────────────────────

    private ObservableCollection<Recipe> _recipes = new();
    private string? _recipesPath;

    private List<ItemDatabaseEntry> _itemDatabase = new();
    private readonly string _itemDbPath = Path.Combine(AppContext.BaseDirectory, "items_database.json");

    // ─── AutoSave & UndoRedo ────────────────────────────────────────────────────

    private readonly AutoSaveManager _autoSave;
    private readonly UndoRedoManager _undoRedo;
    private readonly SnapshotUndoStack _snapshots = new(50);
    private readonly Dictionary<string, string> _openFiles = new();

    // ─── Init ────────────────────────────────────────────────────────────────

    public MainWindow()
    {
        // AutoSave и UndoRedo инициализируем до InitializeComponent
        _autoSave = new AutoSaveManager(OnDirtyChanged);
        _undoRedo = new UndoRedoManager();

        InitializeComponent();
        _itemDatabase = ItemDatabaseService.Load(_itemDbPath);
        ItemsGrid.ItemsSource   = _itemDatabase;
        RecipesList.ItemsSource = _recipes;

        // Тёмная тема по умолчанию (уже установлена в App.xaml → Dark.xaml)
        ThemeManager.Apply(dark: true);
        SetupAutoCompletes();
        Loaded += (_, _) => { InitNavigation(); InitModsAutoCompletes(); WireUpUndoRedo(); WireUpKeyBindings(); AutoLoadFromGmConfig(); };
    }

    private void SetStatus(string text) => StatusText.Text = text;

    private void OnDirtyChanged(string key, bool dirty)
    {
        var dotFiles = _autoSave.AnyDirty
            ? string.Join(", ", _openFiles.Where(kv => true).Select(kv => "● " + kv.Value))
            : "";
        DirtyIndicator.Text    = _autoSave.AnyDirty ? "● Несохранённые изменения" : "";
        DirtyIndicator.Opacity = _autoSave.AnyDirty ? 1 : 0;
    }

    private void OnUndoRedoChanged()
    {
        // Could update undo/redo button states here if we add toolbar buttons
    }

    // ─── UndoRedo wiring ────────────────────────────────────────────────────

    private void WireUpUndoRedo()
    {
        // Используем EventManager для перехвата фокуса ВСЕХ TextBox в окне,
        // включая те что внутри Collapsed страниц — они попадают в визуальное дерево
        // только когда становятся Visible, поэтому используем bubbling события
        AddHandler(UIElement.GotKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler(AnyTextBox_GotFocus), handledEventsToo: true);
        AddHandler(UIElement.LostKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler(AnyTextBox_LostFocus), handledEventsToo: true);
    }

    // Значение TextBox при получении фокуса
    private string _focusSnapshot = "";
    private TextBox? _focusedTextBox;
    private bool _applyingUndo;

    private void AnyTextBox_GotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (_applyingUndo) return;
        if (e.NewFocus is TextBox tb)
        {
            _focusedTextBox = tb;
            _focusSnapshot  = tb.Text;
        }
    }

    private void AnyTextBox_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (_applyingUndo) return;
        if (e.OldFocus is not TextBox tb) return;
        if (tb != _focusedTextBox) return;
        if (tb.Text == _focusSnapshot) return;
        _undoRedo.RecordChange(tb, _focusSnapshot, tb.Text);
    }

    private void WireUpKeyBindings()
    {
        // Перехватываем PreviewKeyDown чтобы работало даже когда TextBox
        // обрабатывает Ctrl+Z сам (мы отменяем стандартное поведение)
        PreviewKeyDown += Window_PreviewKeyDown;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.Z:
                    // Сначала пробуем снапшот (удаления коллекций важнее)
                    if (_snapshots.CanUndo)
                    {
                        var desc = _snapshots.Undo();
                        e.Handled = true;
                        SetStatus($"Отменено: {desc} (Ctrl+Z)");
                    }
                    else if (_undoRedo.CanUndo)
                    {
                        _applyingUndo = true;
                        _undoRedo.Undo();
                        _applyingUndo = false;
                        e.Handled = true;
                        SetStatus("Отменено (Ctrl+Z)");
                    }
                    break;
                case Key.Y:
                    if (_undoRedo.CanRedo)
                    {
                        _applyingUndo = true;
                        _undoRedo.Redo(tb => {
                            tb.Text = tb.Text;
                            tb.CaretIndex = tb.Text.Length;
                        });
                        _applyingUndo = false;
                        e.Handled = true;
                        SetStatus("Повторено (Ctrl+Y)");
                    }
                    break;
                case Key.F:
                    OpenGlobalSearch();
                    e.Handled = true;
                    break;
                case Key.E:
                    OpenValidation();
                    e.Handled = true;
                    break;
                case Key.S:
                    _autoSave.SaveAll();
                    SetStatus("Всё сохранено (Ctrl+S)");
                    e.Handled = true;
                    break;
            }
        }
    }

    private void GlobalSearch_Click(object sender, System.Windows.RoutedEventArgs e)
        => OpenGlobalSearch();

    private void OpenMapBtn_Click(object sender, RoutedEventArgs e)
        => OpenMapWindow();

    private void Validate_Click(object sender, System.Windows.RoutedEventArgs e)
        => OpenValidation();

    private void OpenValidation()
    {
        try
        {
            var win = new ValidationWindow(this) { Owner = this };
            win.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка:\n{ex}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─── Right-click copy ───────────────────────────────────────────────────

    private void DataGrid_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid) return;
        var item = grid.SelectedItem;
        if (item == null) return;
        var className = ExtractClassName(item);
        if (className != null) ShowCopyMenu(grid, className);
    }

    private void ListBox_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not ListBox list) return;
        var item = list.SelectedItem;
        if (item == null) return;
        var className = item is string s ? s : ExtractClassName(item);
        if (className != null) ShowCopyMenu(list, className);
    }

    private static string? ExtractClassName(object item) => item switch
    {
        // Справочник
        ItemDatabaseEntry db  => db.ClassName,
        // Рецепты
        Recipe r              => r.RESULT,
        Ingredient ing        => ing.CLASSNAME,
        // Квесты
        QuestTarget t         => t.TYPE_OBJECT,
        QuestReward rw        => rw.TYPE_OBJECT,
        QuestCostItem ci      => ci.TYPE_OBJECT,
        // Лут
        LootPresetItem li     => li.CLASSNAME,
        // Майнинг (IngredientRow - private nested class)
        _ when item.GetType().Name == "IngredientRow" =>
            item.GetType().GetProperty("Name")?.GetValue(item) as string,
        // Строки (ListBox items)
        string str            => str,
        _                     => null
    };

    private void ShowCopyMenu(FrameworkElement target, string className)
    {
        var menu = new ContextMenu
        {
            Background  = (System.Windows.Media.Brush)Application.Current.Resources["PanelBackground"],
            BorderBrush = (System.Windows.Media.Brush)Application.Current.Resources["BorderBrushSoft"],
            Foreground  = (System.Windows.Media.Brush)Application.Current.Resources["TextBrush"],
        };

        var item = new MenuItem
        {
            Header     = $"📋  Копировать: {className}",
            Background = (System.Windows.Media.Brush)Application.Current.Resources["PanelBackground"],
            Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextBrush"],
        };
        item.Click += (_, _) =>
        {
            Clipboard.SetText(className);
            SetStatus($"Скопировано: {className}");
        };
        menu.Items.Add(item);

        // Также добавляем "Найти в справочнике"
        var findItem = new MenuItem
        {
            Header     = $"🔍  Найти в справочнике",
            Background = (System.Windows.Media.Brush)Application.Current.Resources["PanelBackground"],
            Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextBrush"],
        };
        findItem.Click += (_, _) =>
        {
            NavigateTo(Array.IndexOf(_pageIds, "Items"));
            ItemDbSearchBox.Text = className;
            RefreshItemDatabase();
        };
        menu.Items.Add(findItem);

        target.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private void OpenGlobalSearch()
    {
        try
        {
            var win = new GlobalSearchWindow(this) { Owner = this };
            win.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка открытия поиска:\n{ex}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ─── Public API for GlobalSearchWindow ──────────────────────────────────

    public ObservableCollection<Recipe>         GetRecipes()       => _recipes;
    public List<ItemDatabaseEntry>              GetItemDatabase()  => _itemDatabase;
    public string                               GetRecipesFileName() => Path.GetFileName(_recipesPath ?? "рецепты");
    public string[]                             GetPageIds()       => _pageIds;
    public void                                 NavigateToPublic(int idx) => NavigateTo(idx);

    /// <summary>После навигации выделяет нужный элемент в списке по имени/objectName.</summary>
    public void SelectItemByName(string navKey, string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName) || objectName == "—") return;

        // Небольшая задержка чтобы страница успела стать Visible
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            switch (navKey)
            {
                case "Recipes":
                    var recipe = _recipes.FirstOrDefault(r =>
                        r.RECIPE_NAME?.Contains(objectName, StringComparison.OrdinalIgnoreCase) == true ||
                        r.RESULT?.Contains(objectName, StringComparison.OrdinalIgnoreCase) == true);
                    if (recipe != null)
                    {
                        RecipesList.SelectedItem = recipe;
                        RecipesList.ScrollIntoView(recipe);
                    }
                    break;

                case "Quests":
                    var quest = _quests?.FirstOrDefault(q =>
                        q.QUEST_NAME?.Contains(objectName, StringComparison.OrdinalIgnoreCase) == true ||
                        objectName.Contains($"ID={q.ID}"));
                    if (quest != null)
                    {
                        QuestsList.SelectedItem = quest;
                        QuestsList.ScrollIntoView(quest);
                    }
                    break;

                case "NPC":
                    var npc = _questConfig?.NPC.FirstOrDefault(n =>
                        objectName.Contains(n.NPC_NAME, StringComparison.OrdinalIgnoreCase) ||
                        objectName.Contains($"ID={n.NPC_ID}"));
                    if (npc != null)
                    {
                        NpcList.SelectedItem = npc;
                        NpcList.ScrollIntoView(npc);
                    }
                    break;

                case "Loot":
                    // Находим пресет по номеру из строки "Пресет N"
                    if (int.TryParse(objectName.Replace("Пресет ", ""), out var presetId))
                    {
                        var preset = _presetConfig?.PRESET_SETTINGS.FirstOrDefault(p => p.LOOT_PRESET == presetId);
                        if (preset != null)
                        {
                            LootPresetsList.SelectedItem = preset;
                            LootPresetsList.ScrollIntoView(preset);
                        }
                    }
                    break;

                case "MineRock":
                    var mineRecipe = _mineConfig?.RECIPES.FirstOrDefault(r =>
                        r.RECIPE_NAME?.Contains(objectName, StringComparison.OrdinalIgnoreCase) == true ||
                        r.RESULT?.Contains(objectName, StringComparison.OrdinalIgnoreCase) == true);
                    if (mineRecipe != null)
                    {
                        MineRecipesList.SelectedItem = mineRecipe;
                        MineRecipesList.ScrollIntoView(mineRecipe);
                    }
                    break;

                case "Food":
                    var foodRecipe = _foodConfig?.RECIPES.FirstOrDefault(r =>
                        r.RESULT?.Contains(objectName, StringComparison.OrdinalIgnoreCase) == true);
                    if (foodRecipe != null)
                    {
                        FoodRecipesList.SelectedItem = foodRecipe;
                        FoodRecipesList.ScrollIntoView(foodRecipe);
                    }
                    break;

                case "RockPoints":
                    var rock = _rockConfig?.OBJECT_LIST.FirstOrDefault(r =>
                        r.CLASSNAME?.Contains(objectName, StringComparison.OrdinalIgnoreCase) == true);
                    if (rock != null)
                    {
                        RockObjectsList.SelectedItem = rock;
                        RockObjectsList.ScrollIntoView(rock);
                    }
                    break;
            }
        });
    }

    // These are forwarded from partial classes (QuestLogic, ModsLogic)
    public (List<QuestFileItem> quests, string file) GetQuests() =>
        (_quests?.ToList() ?? new(), Path.GetFileName(_questFilePath ?? "id_*.json"));

    public (List<QuestNpc> npcs, string file) GetNPCs() =>
        (_questConfig?.NPC ?? new(), Path.GetFileName(_questConfigPath ?? "GM_QuestSystemCFG.json"));

    public (List<LootPreset> presets, string file) GetLootPresets() =>
        (_presetConfig?.PRESET_SETTINGS ?? new(), Path.GetFileName(_presetConfigPath ?? "GM_PRESET_CONFIG.json"));

    public List<FlatConfigRow> GetMedicineRows() => _medicineAll;
    public List<FlatConfigRow> GetLiquidRows()   => _liquidAll;
    public GMFoodConfig?       GetFoodConfig()   => _foodConfig;
    public GMMineRockConfig?   GetMineConfig()   => _mineConfig;
    public List<RockObject>    GetRockObjects()  => _rockConfig?.OBJECT_LIST ?? new();
    public List<TriggerPreset> GetTriggers()     => _presetConfig?.TRIGGER_SETTINGS ?? new List<TriggerPreset>();

    private void OpenMapWindow(double? x = null, double? z = null, string? label = null)
    {
        try
        {
            var win = new MapWindow(this, x, z, label) { Owner = this };
            win.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка открытия карты:\n{ex}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Кнопки "Показать на карте" рядом с POSITION полями
    public void ShowOnMap_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var posStr = btn.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(posStr)) return;
        var parts = posStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 &&
            double.TryParse(parts[0], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var x) &&
            double.TryParse(parts[2], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var z))
        {
            OpenMapWindow(x, z, posStr);
        }
    }

    // ─── Theme ───────────────────────────────────────────────────────────────

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // ThemeCombo index 0 = Dark, index 1 = Light (language-independent)
        ThemeManager.Apply(dark: ThemeCombo.SelectedIndex != 1);
        AppSettingsService.Current.Theme = ThemeManager.IsDark ? "dark" : "light";
        AppSettingsService.Save();
    }

    // ─── Navigation ──────────────────────────────────────────────────────────

    // All nav borders in order matching page grids
    private Border[] _navItems = null!;
    private UIElement[] _pages  = null!;

    private readonly string[] _pageIds = { "Recipes","GmConfig","Quests","NPC","Loot","Triggers","Player","Items","Medicine","Ability","Liquid","Food","MineRock","RockPoints","Map" };
    private readonly string[] _pageTitles = { "Рецепты","GM Config","Квесты","NPC","Пресеты лута","Триггеры / Маппинг","Прогресс игрока","Справочник предметов","Medicine Config","Ability Config","Liquid Config","Food Config","MineRock Config","Rock Points","Карта" };

    private void InitNavigation()
    {
        _navItems = new[]
        {
            Nav_Recipes, Nav_GmConfig, Nav_Quests, Nav_NPC,
            Nav_Loot, Nav_Triggers, Nav_Player, Nav_Items,
            Nav_Medicine, Nav_Ability, Nav_Liquid, Nav_Food,
            Nav_MineRock, Nav_RockPoints, Nav_Map,
            
        };
        _pages = new UIElement[]
        {
            Page_Recipes, Page_GmConfig, Page_Quests, Page_NPC,
            Page_Loot, Page_Triggers, Page_Player, Page_Items,
            Page_Medicine, Page_Ability, Page_Liquid, Page_Food,
            Page_MineRock, Page_RockPoints, Page_Map,
            
        };

        // Add toolbar buttons for Recipes page
        AddTopbarButton(LanguageManager.Get("S.Btn.OpenJson"), "OpenRecipes");
        AddTopbarButton(LanguageManager.Get("S.Btn.SaveJson"), "SaveRecipes");

        NavigateTo(0);
    }

    private void NavItem_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Border b) return;
        var tag = b.Tag?.ToString();
        var idx = Array.IndexOf(_pageIds, tag);
        if (idx < 0) return;
        NavigateTo(idx);
    }

    private void NavigateTo(int idx)
    {

        // Highlight nav
        for (int i = 0; i < _navItems.Length; i++)
        {
            _navItems[i].Style = (Style)FindResource(i == idx ? "NavItemActive" : "NavItem");
            var tb = FindDescendantTextBlock(_navItems[i]);
            if (tb != null)
                tb.Foreground = i == idx
                    ? (System.Windows.Media.Brush)Application.Current.Resources["AccentBrush"]
                    : (System.Windows.Media.Brush)Application.Current.Resources["TextBrush"];
        }

        // Show page
        for (int i = 0; i < _pages.Length; i++)
            _pages[i].Visibility = i == idx ? Visibility.Visible : Visibility.Collapsed;

        // Update title and toolbar
        // Устанавливаем заголовок через DynamicResource для поддержки смены языка
var pageResourceKeys = new[] {
    "S.Nav.Recipes","S.Nav.GmConfig","S.Nav.Quests","S.Nav.NPC","S.Nav.Loot",
    "S.Nav.Triggers","S.Nav.Player","S.Items.Title","S.Nav.MedicineConfig",
    "S.Nav.AbilityConfig","S.Nav.LiquidConfig","S.Nav.FoodConfig",
    "S.Nav.MineRock","S.Nav.RockPoints","S.Nav.Map",
    
};
if (idx < pageResourceKeys.Length)
    PageTitle.SetResourceReference(TextBlock.TextProperty, pageResourceKeys[idx]);
else
    PageTitle.Text = _pageTitles[idx];
        UpdateTopbar(idx);
    }

    private void UpdateTopbar(int idx)
    {
        TopbarActions.Children.Clear();
        switch (idx)
        {
            case 0: // Рецепты
                AddTopbarButton(LanguageManager.Get("S.Btn.OpenJson"), "OpenRecipes");
                AddTopbarButton(LanguageManager.Get("S.Btn.SaveJson"), "SaveRecipes");
                break;
            case 1: // GM Config
                AddTopbarButton("GM_CRAFTTABLE_CONFIG ↑", "OpenCraftConfig");
                AddTopbarButton("GM_CRAFTTABLE_CONFIG ↓", "SaveCraftConfig");
                AddTopbarButton("GM_ACCESS_CONFIG ↑", "OpenAccessConfig");
                AddTopbarButton("GM_ACCESS_CONFIG ↓", "SaveAccessConfig");
                break;
            case 2: // Квесты
                AddTopbarButton(LanguageManager.Get("S.Btn.Open") + " id_*.json", "OpenQuestFile");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save"), "SaveQuestFile");
                break;
            case 3: // NPC
                AddTopbarButton("GM_QuestSystemCFG ↑", "OpenQuestConfig");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save"), "SaveQuestConfig");
                break;
            case 4: // Пресеты лута
                AddTopbarButton("GM_PRESET_CONFIG ↑", "OpenPresetConfig");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save"), "SavePresetConfig");
                break;
            case 5: // Триггеры
                AddTopbarButton("GM_PRESET_CONFIG ↑", "OpenPresetConfig");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save"), "SavePresetConfig");
                break;
            case 6: // Прогресс игрока
                AddTopbarButton("SteamID.json ↑", "OpenPlayerData");
                break;
            case 8: // Medicine Config
                AddTopbarButton("GM_MEDICINE_CONFIG ↑", "OpenMedicineConfig");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save"), "SaveMedicineConfig");
                break;
            case 9: // Ability Config
                AddTopbarButton("GM_ABILITY_CONFIG ↑", "OpenAbilityConfig");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save"), "SaveAbilityConfig");
                break;
            case 10: // Liquid Config
                AddTopbarButton("GM_LiquidConfig ↑", "OpenLiquidConfig");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save"), "SaveLiquidConfig");
                break;
            case 11: // Food Config
                AddTopbarButton("GM_FoodConfig ↑", "OpenFoodConfig");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save"), "SaveFoodConfig");
                break;
            case 12: // MineRock Config
                AddTopbarButton("GM_MineRockConfig ↑", "OpenMineRockConfig");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save"), "SaveMineRockConfig");
                break;
            case 13: // Rock Points
                AddTopbarButton("ROCK_POINTS ↑", "OpenRockPoints");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save"), "SaveRockPoints");
                break;
            case 14: // Карта
                AddTopbarButton(LanguageManager.Get("S.Btn.OpenMap"), "OpenMap", accent: true);
                break;
            case 7: // Справочник предметов
                AddTopbarButton(LanguageManager.Get("S.Btn.Add"), "AddItemDb", accent: true);
                AddTopbarButton(LanguageManager.Get("S.Btn.Delete"), "DeleteItemDb", danger: true);
                AddTopbarButton("Import TXT", "ImportTxt");
                AddTopbarButton("Import types.xml", "ImportXml");
                AddTopbarButton(LanguageManager.Get("S.Btn.Open") + " folder", "ImportFolder");
                AddTopbarButton(LanguageManager.Get("S.Btn.Save") + " DB", "SaveItemDb");
                break;
        }
    }

    private void AddTopbarButton(string label, string tag, bool accent = false, bool danger = false)
    {
        var btn = new Button { Content = label, Tag = tag, Margin = new Thickness(0, 0, 6, 0) };
        if (accent) btn.Style = (Style)FindResource("AccentButton");
        if (danger) btn.Style = (Style)FindResource("DangerButton");
        btn.Click += TopbarButton_Click;
        TopbarActions.Children.Add(btn);
    }

    private void TopbarButton_Click(object sender, RoutedEventArgs e)
    {
        var tag = (sender as Button)?.Tag?.ToString();
        switch (tag)
        {
            case "OpenRecipes":     OpenRecipes_Click(sender, e); break;
            case "SaveRecipes":     SaveRecipes_Click(sender, e); break;
            case "AddRecipe":       AddRecipe_Click(sender, e); break;
            case "DeleteRecipe":    DeleteRecipe_Click(sender, e); break;
            case "AddItemDb":       AddItemDb_Click(sender, e); break;
            case "DeleteItemDb":    DeleteItemDb_Click(sender, e); break;
            case "ImportTxt":       ImportItemsTxt_Click(sender, e); break;
            case "ImportXml":       ImportTypesXml_Click(sender, e); break;
            case "ImportFolder":    ImportTypesFolder_Click(sender, e); break;
            case "SaveItemDb":      SaveItemDb_Click(sender, e); break;
            case "OpenQuestConfig": OpenQuestConfig_Click(sender, e); break;
            case "SaveQuestConfig": SaveQuestConfig_Click(sender, e); break;
            case "OpenQuestFile":   OpenQuestFile_Click(sender, e); break;
            case "SaveQuestFile":   SaveQuestFile_Click(sender, e); break;
            case "AddQuest":        AddQuest_Click(sender, e); break;
            case "DeleteQuest":     DeleteQuest_Click(sender, e); break;
            case "DuplicateQuest":  DuplicateQuest_Click(sender, e); break;
            case "AddNpc":          AddNpc_Click(sender, e); break;
            case "DeleteNpc":       DeleteNpc_Click(sender, e); break;
            case "OpenPresetConfig":OpenPresetConfig_Click(sender, e); break;
            case "SavePresetConfig":SavePresetConfig_Click(sender, e); break;
            case "AddLootPreset":   AddLootPreset_Click(sender, e); break;
            case "AddTrigger":      AddTrigger_Click(sender, e); break;
            case "AddMappingGroup": AddMappingGroup_Click(sender, e); break;
            case "OpenPlayerData":   OpenPlayerData_Click(sender, e); break;
            case "OpenMedicineConfig": OpenMedicineConfig_Click(sender, e); break;
            case "SaveMedicineConfig": SaveMedicineConfig_Click(sender, e); break;
            case "OpenAbilityConfig":  OpenAbilityConfig_Click(sender, e); break;
            case "SaveAbilityConfig":  SaveAbilityConfig_Click(sender, e); break;
            case "OpenLiquidConfig":   OpenLiquidConfig_Click(sender, e); break;
            case "SaveLiquidConfig":   SaveLiquidConfig_Click(sender, e); break;
            case "OpenFoodConfig":     OpenFoodConfig_Click(sender, e); break;
            case "SaveFoodConfig":     SaveFoodConfig_Click(sender, e); break;
            case "OpenMineRockConfig": OpenMineRockConfig_Click(sender, e); break;
            case "SaveMineRockConfig": SaveMineRockConfig_Click(sender, e); break;
            case "OpenRockPoints":     OpenRockPoints_Click(sender, e); break;
            case "SaveRockPoints":     SaveRockPoints_Click(sender, e); break;
            case "ValidateAll":        OpenValidation(); break;
            case "AddRockObject":      AddRockObject_Click(sender, e); break;
            case "OpenMap":            OpenMapWindow(); break;
            case "OpenCraftConfig":  OpenCraftConfig_Click(sender, e); break;
            case "SaveCraftConfig":  SaveCraftConfig_Click(sender, e); break;
            case "OpenAccessConfig": OpenAccessConfig_Click(sender, e); break;
            case "SaveAccessConfig": SaveAccessConfig_Click(sender, e); break;
        }
    }

    private static TextBlock? FindDescendantTextBlock(DependencyObject parent)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is TextBlock tb) return tb;
            var found = FindDescendantTextBlock(child);
            if (found != null) return found;
        }
        return null;
    }

    // ─── Navigation ──────────────────────────────────────────────────────────

    // ─── Recipes: Load / Save ────────────────────────────────────────────────

    private void OpenRecipes_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _recipesPath = dlg.FileName;
            _recipes = JsonService.Load<ObservableCollection<Recipe>>(_recipesPath);
            NormalizeRecipes();
            RecipesList.ItemsSource = _recipes;
            SetStatus($"Крафт загружен: {_recipes.Count} рецептов");
            _autoSave.Register("recipes", () => { if (_recipesPath != null) JsonService.Save(_recipesPath, _recipes); });
            _openFiles["recipes"] = Path.GetFileName(_recipesPath!);
            _autoSave.MarkClean("recipes");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть JSON крафта.\n\nФайл: {dlg.FileName}\n\nОшибка: {ex.Message}",
                "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Ошибка загрузки крафта");
        }
    }

    private void SaveRecipes_Click(object sender, RoutedEventArgs e)
    {
        var path = _recipesPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var dlg = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = "newplayer_recipes.json" };
            if (dlg.ShowDialog() != true) return;
            path = _recipesPath = dlg.FileName;
        }
        JsonService.Save(path, _recipes);
        SetStatus($"Крафт сохранён: {path}");
        _autoSave.MarkClean("recipes");
    }

    // ─── Recipes: Add / Delete ───────────────────────────────────────────────

    private void AddRecipe_Click(object sender, RoutedEventArgs e)
    {
        var recipe = new Recipe();
        recipe.INGRIDIENTS.Add(new Ingredient());
        _recipes.Add(recipe);
        RecipesList.SelectedItem = recipe;
        _autoSave.MarkDirty("recipes");
    }

    private void DeleteRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe) return;
        // Сохраняем снапшот ПЕРЕД удалением
        var json = System.Text.Json.JsonSerializer.Serialize(_recipes);
        var recipeName = recipe.RECIPE_NAME;
        _snapshots.Push(
            $"Удаление рецепта «{recipeName}»",
            json,
            j => {
                var restored = System.Text.Json.JsonSerializer.Deserialize<System.Collections.ObjectModel.ObservableCollection<Recipe>>(j)!;
                _recipes.Clear();
                foreach (var r in restored) _recipes.Add(r);
                RecipesList.ItemsSource = null;
                RecipesList.ItemsSource = _recipes;
                SetupAutoCompletes();
                RefreshRecipeDetails();
            }
        );
        _recipes.Remove(recipe);
        _autoSave.MarkDirty("recipes");
    }

    private void RecipesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshRecipeDetails();
        if (RecipesList.SelectedItem is Recipe r)
        {
            ResultComboBox.Text = r.RESULT;
            PlanComboBox.Text   = r.PLAN;
        }
    }

    // ─── Recipes: Search ─────────────────────────────────────────────────────

    private void RecipeSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var view = CollectionViewSource.GetDefaultView(RecipesList.ItemsSource);
        var q    = RecipeSearchBox.Text?.Trim() ?? string.Empty;
        if (view == null) return;

        view.Filter = o =>
        {
            if (string.IsNullOrWhiteSpace(q)) return true;
            if (o is not Recipe r) return false;
            return (r.RECIPE_NAME?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (r.CATEGORY?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (r.RESULT?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);
        };
    }

    // ─── Tools ──────────────────────────────────────────────────────────────

    private void AddTool_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe) return;
        var text = ToolAutoCompleteBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;
        recipe.TOOLS.Add(text);
        ToolAutoCompleteBox.Text = "";
        RefreshRecipeDetails();
    }

    private void DeleteTool_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is Recipe recipe && ToolsList.SelectedItem is string tool)
            recipe.TOOLS.Remove(tool);
    }

    // ─── Ingredients ────────────────────────────────────────────────────────

    private void DeleteIngredient_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is Recipe recipe && IngredientsGrid.SelectedItem is Ingredient ing)
            recipe.INGRIDIENTS.Remove(ing);
    }

    // ─── Kit / Special items ─────────────────────────────────────────────────

    private void KitItemCombo_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        => FilterComboBox(KitItemCombo, KitItemCombo.Text?.Trim() ?? "");

    private void AddKitItem_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe) return;
        var value = KitItemCombo.Text?.Trim() ?? "";
        if (value.Length == 0) return;
        recipe.NEEDS_KIT_ITEMS.Add(value);
        KitItemCombo.Text = "";
    }

    private void DeleteKitItem_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is Recipe recipe && KitItemsList.SelectedItem is string item)
            recipe.NEEDS_KIT_ITEMS.Remove(item);
    }

    private void SpecialItemCombo_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        => FilterComboBox(SpecialItemCombo, SpecialItemCombo.Text?.Trim() ?? "");

    private void AddSpecialItem_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe) return;
        var value = SpecialItemCombo.Text?.Trim() ?? "";
        if (value.Length == 0) return;
        recipe.NEEDS_SPECIAL_ITEMS.Add(value);
        SpecialItemCombo.Text = "";
    }

    private void DeleteSpecialItem_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is Recipe recipe && SpecialItemsList.SelectedItem is string item)
            recipe.NEEDS_SPECIAL_ITEMS.Remove(item);
    }

    // ─── Autocomplete: Result ────────────────────────────────────────────────

    private void ResultComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        => FilterComboBox(ResultComboBox, ResultComboBox.Text?.Trim() ?? "");

    private void ResultComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe) return;
        if (ResultComboBox.SelectedItem is not ItemDatabaseEntry item) return;
        recipe.RESULT      = item.ClassName;
        ResultComboBox.Text = item.ClassName;
    }

    // ─── Autocomplete: Plan ──────────────────────────────────────────────────

    private void PlanComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        => FilterComboBox(PlanComboBox, PlanComboBox.Text?.Trim() ?? "");

    private void PlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe) return;
        if (PlanComboBox.SelectedItem is not ItemDatabaseEntry item) return;
        recipe.PLAN      = item.ClassName;
        PlanComboBox.Text = item.ClassName;
    }

    // ─── Autocomplete: Tool ──────────────────────────────────────────────────

    private void ToolAutoCompleteBox_PreviewKeyUp(object sender, KeyEventArgs e)
        => FilterComboBox(ToolAutoCompleteBox, ToolAutoCompleteBox.Text?.Trim() ?? "");

    // ─── Autocomplete: Ingredient ────────────────────────────────────────────

    private void IngredientAutoCompleteBox_PreviewKeyUp(object sender, KeyEventArgs e)
        => FilterComboBox(IngredientAutoCompleteBox, IngredientAutoCompleteBox.Text?.Trim() ?? "");

    private void AddIngredientFromAutoComplete_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe) return;
        var value = IngredientAutoCompleteBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(value)) return;

        recipe.INGRIDIENTS.Add(new Ingredient { CLASSNAME = value, ITEM_AMOUNT = 1, DESTROY_ITEM = 1 });
        IngredientAutoCompleteBox.Text = "";
        RefreshRecipeDetails();
    }

    // ─── Autocomplete: shared helper ────────────────────────────────────────

    private void SetupAutoCompletes()
    {
        ResultComboBox.ItemsSource            = _itemDatabase;
        PlanComboBox.ItemsSource              = _itemDatabase;
        ToolAutoCompleteBox.ItemsSource       = _itemDatabase;
        IngredientAutoCompleteBox.ItemsSource = _itemDatabase;
        KitItemCombo.ItemsSource              = _itemDatabase;
        SpecialItemCombo.ItemsSource          = _itemDatabase;
        RewardItemCombo.ItemsSource           = _itemDatabase;
        CostItemCombo.ItemsSource             = _itemDatabase;
        NpcAttachCombo.ItemsSource            = _itemDatabase;
        LootItemCombo.ItemsSource             = _itemDatabase;
        // Mods autocompletes - initialized in ModsLogic
    }

    private void FilterComboBox(ComboBox box, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            box.ItemsSource    = _itemDatabase;
            box.IsDropDownOpen = false;
            return;
        }

        var filtered = _itemDatabase
            .Where(x =>
                x.ClassName.Contains(text, StringComparison.OrdinalIgnoreCase)   ||
                x.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase)  ||
                x.SourceMod.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(60)
            .ToList();

        box.ItemsSource    = filtered;
        box.IsDropDownOpen = filtered.Count > 0;

        if (box.Template.FindName("PART_EditableTextBox", box) is TextBox editBox)
        {
            editBox.Text       = text;
            editBox.CaretIndex = text.Length;
        }
    }

    // ─── Item Database ───────────────────────────────────────────────────────

    private void AddItemDb_Click(object sender, RoutedEventArgs e)
    {
        var item = new ItemDatabaseEntry { ClassName = "NewItem", Category = "Без категории" };
        _itemDatabase.Add(item);
        RefreshItemDatabase();
        ItemsGrid.SelectedItem = item;
        ItemsGrid.ScrollIntoView(item);
        SetStatus($"Добавлен предмет: {item.ClassName}");
    }

    private void DeleteItemDb_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsGrid.SelectedItem is not ItemDatabaseEntry item)
        {
            MessageBox.Show("Сначала выбери строку в справочнике.", "Удаление");
            return;
        }
        _itemDatabase.Remove(item);
        RefreshItemDatabase();
        SetStatus($"Удалён предмет: {item.ClassName}");
    }

    private void SaveItemDb_Click(object sender, RoutedEventArgs e)
    {
        ItemDatabaseService.Save(_itemDbPath, _itemDatabase);
        SetStatus($"Справочник сохранён: {_itemDatabase.Count} предметов");
    }

    private void ImportItemsTxt_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
        if (dlg.ShowDialog() != true) return;
        var imported = ItemDatabaseService.ImportFromTxt(dlg.FileName);
        AddImportedItems(imported);
        SetStatus($"Импортировано из TXT: {imported.Count} предметов");
    }

    private void ImportTypesXml_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*" };
        if (dlg.ShowDialog() != true) return;
        var imported = ItemDatabaseService.ImportFromTypesXml(dlg.FileName);
        AddImportedItems(imported);
        SetStatus($"Импортировано из types.xml: {imported.Count}");
    }

    private void ImportTypesFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Выбери папку с types.xml / custom types" };
        if (dlg.ShowDialog() != true) return;
        var imported = ItemDatabaseService.ImportFromTypesFolder(dlg.FolderName);
        AddImportedItems(imported);
        SetStatus($"Импортировано из папки types: {imported.Count}");
    }

    private void AddImportedItems(List<ItemDatabaseEntry> imported)
    {
        var added = 0;
        foreach (var item in imported)
        {
            if (_itemDatabase.Any(x => x.ClassName.Equals(item.ClassName, StringComparison.OrdinalIgnoreCase)))
                continue;
            _itemDatabase.Add(item);
            added++;
        }
        RefreshItemDatabase();
        SetStatus($"Добавлено новых предметов: {added}");
    }

    private void ItemDbSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        => RefreshItemDatabase();

    private void RefreshItemDatabase()
    {
        var query = ItemDbSearchBox?.Text?.Trim() ?? "";
        IEnumerable<ItemDatabaseEntry> items = _itemDatabase;

        if (!string.IsNullOrWhiteSpace(query))
            items = items.Where(x =>
                x.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase)  ||
                x.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Category.Contains(query, StringComparison.OrdinalIgnoreCase)    ||
                x.SourceMod.Contains(query, StringComparison.OrdinalIgnoreCase));

        ItemsGrid.ItemsSource = null;
        ItemsGrid.ItemsSource = items.ToList();
    }

    // ─── "Use as…" from Item Database ───────────────────────────────────────

    private ItemDatabaseEntry? SelectedDbItem() => ItemsGrid.SelectedItem as ItemDatabaseEntry;
    private Recipe? SelectedRecipe() => RecipesList.SelectedItem as Recipe;

    private void UseAsResult_Click(object sender, RoutedEventArgs e)
    {
        var (item, recipe) = (SelectedDbItem(), SelectedRecipe());
        if (item == null || recipe == null) return;
        recipe.RESULT = item.ClassName;
        ResultComboBox.Text = item.ClassName;
        RecipesList.Items.Refresh();
        SetStatus($"RESULT = {item.ClassName}");
    }

    private void UseAsPlan_Click(object sender, RoutedEventArgs e)
    {
        var (item, recipe) = (SelectedDbItem(), SelectedRecipe());
        if (item == null || recipe == null) return;
        recipe.PLAN = item.ClassName;
        PlanComboBox.Text = item.ClassName;
        RecipesList.Items.Refresh();
        SetStatus($"PLAN = {item.ClassName}");
    }

    private void UseAsTool_Click(object sender, RoutedEventArgs e)
    {
        var (item, recipe) = (SelectedDbItem(), SelectedRecipe());
        if (item == null || recipe == null) return;
        recipe.TOOLS.Add(item.ClassName);
        RefreshRecipeDetails();
        SetStatus($"Добавлен инструмент: {item.ClassName}");
    }

    private void UseAsIngredient_Click(object sender, RoutedEventArgs e)
    {
        var (item, recipe) = (SelectedDbItem(), SelectedRecipe());
        if (item == null || recipe == null) return;
        recipe.INGRIDIENTS.Add(new Ingredient { CLASSNAME = item.ClassName, ITEM_AMOUNT = 1, DESTROY_ITEM = 1 });
        RefreshRecipeDetails();
        SetStatus($"Добавлен ингредиент: {item.ClassName}");
    }

    // ─── Validation ──────────────────────────────────────────────────────────

    private void ValidateRecipes_Click(object sender, RoutedEventArgs e)
    {
        OpenValidation();
    }

    private void ValidateRecipes_FullCheck()
    {
        var errors = new List<string>();

        foreach (var recipe in _recipes)
        {
            if (string.IsNullOrWhiteSpace(recipe.RESULT))
                errors.Add($"[{recipe.RECIPE_NAME}] RESULT пустой");
            else if (!ClassNameExists(recipe.RESULT))
                errors.Add($"[{recipe.RECIPE_NAME}] RESULT не найден: {recipe.RESULT}");

            foreach (var tool in recipe.TOOLS)
            {
                if (string.IsNullOrWhiteSpace(tool)) errors.Add($"[{recipe.RECIPE_NAME}] Пустой TOOL");
                else if (!ClassNameExists(tool))     errors.Add($"[{recipe.RECIPE_NAME}] TOOL не найден: {tool}");
            }

            foreach (var ing in recipe.INGRIDIENTS)
            {
                if (string.IsNullOrWhiteSpace(ing.CLASSNAME)) errors.Add($"[{recipe.RECIPE_NAME}] Пустой INGREDIENT");
                else if (!ClassNameExists(ing.CLASSNAME))     errors.Add($"[{recipe.RECIPE_NAME}] INGREDIENT не найден: {ing.CLASSNAME}");
            }
        }

        if (errors.Count == 0)
        {
            MessageBox.Show("Ошибок не найдено.", "Проверка рецептов", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(string.Join(Environment.NewLine, errors),
            $"Найдено ошибок: {errors.Count}", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private bool ClassNameExists(string className) =>
        _itemDatabase.Any(x => x.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase));

    // ─── Helpers ────────────────────────────────────────────────────────────

    private void NormalizeRecipes()
    {
        foreach (var recipe in _recipes)
        {
            recipe.NEEDS_KIT_ITEMS    ??= new ObservableCollection<string>();
            recipe.TOOLS              ??= new ObservableCollection<string>();
            recipe.NEEDS_SPECIAL_ITEMS??= new ObservableCollection<string>();
            recipe.INGRIDIENTS        ??= new ObservableCollection<Ingredient>();
        }
    }

    private void RefreshRecipeDetails()
    {
        if (RecipesList.SelectedItem is not Recipe recipe)
        {
            IngredientsGrid.ItemsSource = null;
            ToolsList.ItemsSource       = null;
            KitItemsList.ItemsSource    = null;
            SpecialItemsList.ItemsSource= null;
            return;
        }

        IngredientsGrid.ItemsSource  = null; IngredientsGrid.ItemsSource  = recipe.INGRIDIENTS;
        ToolsList.ItemsSource        = null; ToolsList.ItemsSource        = recipe.TOOLS;
        KitItemsList.ItemsSource     = null; KitItemsList.ItemsSource     = recipe.NEEDS_KIT_ITEMS;
        SpecialItemsList.ItemsSource = null; SpecialItemsList.ItemsSource = recipe.NEEDS_SPECIAL_ITEMS;
    }
    public void ImportItemDatabaseEntries(List<ItemDatabaseEntry> items)
    {
        AddImportedItems(items);

        ItemDatabaseService.Save(
            _itemDbPath,
            _itemDatabase.ToList()
        );

        RefreshItemDatabase();
    }
    private void ImportWorkshopClassnames_Click(object sender, RoutedEventArgs e)
    {
        var win = new WorkshopImportWindow(this)
        {
            Owner = this
        };
    
        win.ShowDialog();
    }
}
