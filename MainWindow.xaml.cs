using System.Collections.ObjectModel;
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

    // ─── Init ────────────────────────────────────────────────────────────────

    public MainWindow()
    {
        InitializeComponent();
        _itemDatabase = ItemDatabaseService.Load(_itemDbPath);
        ItemsGrid.ItemsSource   = _itemDatabase;
        RecipesList.ItemsSource = _recipes;

        // Тёмная тема по умолчанию (уже установлена в App.xaml → Dark.xaml)
        ThemeManager.Apply(dark: true);
        SetupAutoCompletes();
        Loaded += (_, _) => InitNavigation();
    }

    private void SetStatus(string text) => StatusText.Text = text;

    // ─── Theme ───────────────────────────────────────────────────────────────

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var content = (ThemeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        ThemeManager.Apply(content.Contains("Тём"));
    }

    // ─── Navigation ──────────────────────────────────────────────────────────

    // All nav borders in order matching page grids
    private Border[] _navItems = null!;
    private UIElement[] _pages  = null!;

    private readonly string[] _pageIds = { "Recipes","Items","Quests","NPC","Loot","Triggers","Player","GmConfig" };
    private readonly string[] _pageTitles = { "Рецепты","Справочник предметов","Квесты","NPC","Пресеты лута","Триггеры / Маппинг","Прогресс игрока","GM Config" };

    private void InitNavigation()
    {
        _navItems = new[]
        {
            Nav_Recipes, Nav_Items, Nav_Quests, Nav_NPC,
            Nav_Loot, Nav_Triggers, Nav_Player, Nav_GmConfig
        };
        _pages = new UIElement[]
        {
            Page_Recipes, Page_Items, Page_Quests, Page_NPC,
            Page_Loot, Page_Triggers, Page_Player, Page_GmConfig
        };

        // Add toolbar buttons for Recipes page
        AddTopbarButton("Открыть крафт JSON", "OpenRecipes");
        AddTopbarButton("Сохранить крафт JSON", "SaveRecipes");

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
        // GM Config — opens as dialog
        if (idx == 7)
        {
            new GmConfigWindow { Owner = this }.ShowDialog();
            return;
        }

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
        PageTitle.Text = _pageTitles[idx];
        UpdateTopbar(idx);
    }

    private void UpdateTopbar(int idx)
    {
        TopbarActions.Children.Clear();
        switch (idx)
        {
            case 0: // Рецепты
                AddTopbarButton("Открыть JSON", "OpenRecipes");
                AddTopbarButton("Сохранить JSON", "SaveRecipes");
                AddTopbarButton("+ Рецепт", "AddRecipe", accent: true);
                AddTopbarButton("Удалить", "DeleteRecipe", danger: true);
                break;
            case 1: // Справочник
                AddTopbarButton("+ Добавить", "AddItemDb", accent: true);
                AddTopbarButton("Удалить", "DeleteItemDb", danger: true);
                AddTopbarButton("Импорт TXT", "ImportTxt");
                AddTopbarButton("Импорт types.xml", "ImportXml");
                AddTopbarButton("Импорт папки", "ImportFolder");
                AddTopbarButton("Сохранить справочник", "SaveItemDb");
                break;
            case 2: // Квесты
                AddTopbarButton("Открыть GM_QuestSystemCFG", "OpenQuestConfig");
                AddTopbarButton("Сохранить CFG", "SaveQuestConfig");
                AddTopbarButton("Открыть id_*.json", "OpenQuestFile");
                AddTopbarButton("Сохранить квесты", "SaveQuestFile");
                AddTopbarButton("+ Квест", "AddQuest", accent: true);
                AddTopbarButton("Удалить", "DeleteQuest", danger: true);
                break;
            case 3: // NPC
                AddTopbarButton("Открыть GM_QuestSystemCFG", "OpenQuestConfig");
                AddTopbarButton("Сохранить CFG", "SaveQuestConfig");
                AddTopbarButton("+ NPC", "AddNpc", accent: true);
                AddTopbarButton("Удалить", "DeleteNpc", danger: true);
                break;
            case 4: // Пресеты лута
                AddTopbarButton("Открыть GM_PRESET_CONFIG", "OpenPresetConfig");
                AddTopbarButton("Сохранить PRESET", "SavePresetConfig");
                AddTopbarButton("+ Пресет", "AddLootPreset", accent: true);
                break;
            case 5: // Триггеры
                AddTopbarButton("Открыть GM_PRESET_CONFIG", "OpenPresetConfig");
                AddTopbarButton("Сохранить PRESET", "SavePresetConfig");
                AddTopbarButton("+ Триггер", "AddTrigger", accent: true);
                AddTopbarButton("+ Группу маппинга", "AddMappingGroup", accent: true);
                break;
            case 6: // Прогресс игрока
                AddTopbarButton("Открыть SteamID.json", "OpenPlayerData");
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
            case "AddNpc":          AddNpc_Click(sender, e); break;
            case "DeleteNpc":       DeleteNpc_Click(sender, e); break;
            case "OpenPresetConfig":OpenPresetConfig_Click(sender, e); break;
            case "SavePresetConfig":SavePresetConfig_Click(sender, e); break;
            case "AddLootPreset":   AddLootPreset_Click(sender, e); break;
            case "AddTrigger":      AddTrigger_Click(sender, e); break;
            case "AddMappingGroup": AddMappingGroup_Click(sender, e); break;
            case "OpenPlayerData":  OpenPlayerData_Click(sender, e); break;
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

    private void OpenGmConfigWindow_Click(object sender, RoutedEventArgs e)
    {
        new GmConfigWindow { Owner = this }.ShowDialog();
    }

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
    }

    // ─── Recipes: Add / Delete ───────────────────────────────────────────────

    private void AddRecipe_Click(object sender, RoutedEventArgs e)
    {
        var recipe = new Recipe();
        recipe.INGRIDIENTS.Add(new Ingredient());
        _recipes.Add(recipe);
        RecipesList.SelectedItem = recipe;
    }

    private void DeleteRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is Recipe recipe)
            _recipes.Remove(recipe);
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

    private void AddKitItem_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe) return;
        var value = KitItemText.Text.Trim();
        if (value.Length == 0) return;
        recipe.NEEDS_KIT_ITEMS.Add(value);
        KitItemText.Clear();
    }

    private void DeleteKitItem_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is Recipe recipe && KitItemsList.SelectedItem is string item)
            recipe.NEEDS_KIT_ITEMS.Remove(item);
    }

    private void AddSpecialItem_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe) return;
        var value = SpecialItemText.Text.Trim();
        if (value.Length == 0) return;
        recipe.NEEDS_SPECIAL_ITEMS.Add(value);
        SpecialItemText.Clear();
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
        ResultComboBox.ItemsSource          = _itemDatabase;
        PlanComboBox.ItemsSource            = _itemDatabase;
        ToolAutoCompleteBox.ItemsSource     = _itemDatabase;
        IngredientAutoCompleteBox.ItemsSource = _itemDatabase;
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
}
