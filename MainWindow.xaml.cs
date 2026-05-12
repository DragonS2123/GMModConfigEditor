using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows;
using Microsoft.Win32;
using GMCraftTableEditor.Models;
using GMCraftTableEditor.Services;
using System.IO;

namespace GMCraftTableEditor;

public class GMAccessConfig
{
        public string TITLE { get; set; }
    public string AUTHOR { get; set; }
    public string DISCORD { get; set; }
    public string CONFIG_VERSION { get; set; }
    public string GENERAL_SETTINGS { get; set; }
    public List<string> ADMIN_IDS { get; set; } = new();
    public List<CraftTableAccess> CRAFT_TABLES { get; set; } = new();
}
public class CraftTableAccess
{
        public int TABLE_ID { get; set; }
    public int USE_WHITELIST { get; set; }
    public List<int> ARRAY_CATEGORY { get; set; } = new();
    public List<string> ALLOWED_PLAYERS { get; set; } = new();
    public int STATUS { get; set; }
}

public class GMCraftTableConfig
{
    public string TITLE { get; set; }
    public string AUTHOR { get; set; }
    public string DISCORD { get; set; }
    public string CONFIG_VERSION { get; set; }
    public string GENERAL_SETTINGS { get; set; }

    public int CAN_BE_REPAIR_TO_PRISTINE { get; set; }
    public List<CategoryItem> CATEGORY_LIST { get; set; } = new();
    public List<object> RECIPE_LIST { get; set; } = new();
}

public class CategoryItem
{
    public int TYPE { get; set; }
    public string NAME { get; set; }
    public string RECIPE_FILENAME { get; set; }
    public string ICON_PATH { get; set; }
    public int PRIVATE { get; set; }
}

public partial class MainWindow : Window
{
    private ObservableCollection<Recipe> _recipes = new();
    private string? _recipesPath;

    private List<ItemDatabaseEntry> _itemDatabase = new();
    private readonly string _itemDbPath = Path.Combine(AppContext.BaseDirectory, "items_database.json");

    public MainWindow()
    {
        InitializeComponent();
        _itemDatabase = ItemDatabaseService.Load(_itemDbPath);
        ItemsGrid.ItemsSource = _itemDatabase;
        RecipesList.ItemsSource = _recipes;
        ApplyTheme(true);
    }

    private void SetStatus(string text) => StatusText.Text = text;

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = (ThemeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Тёмная";
        ApplyTheme(selected.Contains("Тём"));
    }

    private void ApplyTheme(bool dark)
    {
        Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(21, 154, 140));
        Resources["DangerBrush"] = new SolidColorBrush(Color.FromRgb(201, 69, 69));

        if (dark)
        {
            Resources["AppBackground"] = new SolidColorBrush(Color.FromRgb(18, 23, 31));
            Resources["PanelBackground"] = new SolidColorBrush(Color.FromRgb(28, 34, 44));
            Resources["PanelBackground2"] = new SolidColorBrush(Color.FromRgb(38, 45, 57));
            Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(235, 239, 245));
            Resources["MutedTextBrush"] = new SolidColorBrush(Color.FromRgb(166, 176, 190));
            Resources["BorderBrushSoft"] = new SolidColorBrush(Color.FromRgb(55, 65, 80));
            Resources["InputBackground"] = new SolidColorBrush(Color.FromRgb(22, 27, 36));
        }
        else
        {
            Resources["AppBackground"] = new SolidColorBrush(Color.FromRgb(244, 246, 248));
            Resources["PanelBackground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            Resources["PanelBackground2"] = new SolidColorBrush(Color.FromRgb(238, 242, 246));
            Resources["TextBrush"] = new SolidColorBrush(Color.FromRgb(21, 26, 32));
            Resources["MutedTextBrush"] = new SolidColorBrush(Color.FromRgb(89, 99, 111));
            Resources["BorderBrushSoft"] = new SolidColorBrush(Color.FromRgb(208, 215, 223));
            Resources["InputBackground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }
    }

    private void RecipeSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var view = CollectionViewSource.GetDefaultView(RecipesList.ItemsSource);
        var q = RecipeSearchBox.Text?.Trim() ?? string.Empty;
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
            MessageBox.Show(
                $"Не удалось открыть JSON крафта.\n\nФайл: {dlg.FileName}\n\nОшибка: {ex.Message}",
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
            path = dlg.FileName;
            _recipesPath = path;
        }
        JsonService.Save(path, _recipes);
        SetStatus($"Крафт сохранён: {path}");
    }

    private void AddRecipe_Click(object sender, RoutedEventArgs e)
    {
        var recipe = new Recipe();
        recipe.INGRIDIENTS.Add(new Ingredient());
        _recipes.Add(recipe);
        RecipesList.SelectedItem = recipe;
    }

    private void DeleteRecipe_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is Recipe recipe) _recipes.Remove(recipe);
    }

    private void RecipesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        IngredientsGrid.ItemsSource = (RecipesList.SelectedItem as Recipe)?.INGRIDIENTS;
        ToolsList.ItemsSource = (RecipesList.SelectedItem as Recipe)?.TOOLS;
        KitItemsList.ItemsSource = (RecipesList.SelectedItem as Recipe)?.NEEDS_KIT_ITEMS;
        SpecialItemsList.ItemsSource = (RecipesList.SelectedItem as Recipe)?.NEEDS_SPECIAL_ITEMS;
    }

    private void AddTool_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe)
            return;

        var value = ToolAutoCompleteBox.Text.Trim();

        if (value.Length == 0)
            return;

        recipe.TOOLS.Add(value);

        ToolAutoCompleteBox.Text = "";
        RefreshRecipeDetails();
    }

    private void DeleteTool_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is Recipe recipe && ToolsList.SelectedItem is string tool)
            recipe.TOOLS.Remove(tool);
    }

    private void AddIngredient_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is Recipe recipe)
            recipe.INGRIDIENTS.Add(new Ingredient());
    }

    private void DeleteIngredient_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is Recipe recipe && IngredientsGrid.SelectedItem is Ingredient ing)
            recipe.INGRIDIENTS.Remove(ing);
    }


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

    private void NormalizeRecipes()
    {
        foreach (var recipe in _recipes)
        {
            recipe.NEEDS_KIT_ITEMS ??= new ObservableCollection<string>();
            recipe.TOOLS ??= new ObservableCollection<string>();
            recipe.NEEDS_SPECIAL_ITEMS ??= new ObservableCollection<string>();
            recipe.INGRIDIENTS ??= new ObservableCollection<Ingredient>();
        }
    }
    private void AddItemDb_Click(object sender, RoutedEventArgs e)
    {
        var item = new ItemDatabaseEntry
        {
            ClassName = "NewItem",
            DisplayName = "",
            Category = "Без категории",
            SourceMod = "",
            Comment = "",
            Favorite = false
        };

        _itemDatabase.Add(item);

        RefreshItemDatabase();

        ItemsGrid.SelectedItem = item;
        ItemsGrid.ScrollIntoView(item);

        StatusText.Text = $"Добавлен предмет: {item.ClassName}";
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

        StatusText.Text = $"Удалён предмет: {item.ClassName}";
    }

    private void SaveItemDb_Click(object sender, RoutedEventArgs e)
    {
        ItemDatabaseService.Save(_itemDbPath, _itemDatabase);
        StatusText.Text = $"Справочник сохранён: {_itemDatabase.Count} предметов";
    }

    private void ImportItemsTxt_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        var imported = ItemDatabaseService.ImportFromTxt(dialog.FileName);

        foreach (var item in imported)
        {
            if (_itemDatabase.Any(x => x.ClassName.Equals(item.ClassName, StringComparison.OrdinalIgnoreCase)))
                continue;

            _itemDatabase.Add(item);
        }

        RefreshItemDatabase();
        StatusText.Text = $"Импортировано предметов: {imported.Count}";
    }

    private void ItemDbSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshItemDatabase();
    }

    private void RefreshItemDatabase()
    {
        var query = ItemDbSearchBox?.Text?.Trim() ?? "";

        IEnumerable<ItemDatabaseEntry> items = _itemDatabase;

        if (!string.IsNullOrWhiteSpace(query))
        {
            items = items.Where(x =>
                x.ClassName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.SourceMod.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        ItemsGrid.ItemsSource = null;
        ItemsGrid.ItemsSource = items.ToList();
    }
    private ItemDatabaseEntry? SelectedDbItem()
    {
        return ItemsGrid.SelectedItem as ItemDatabaseEntry;
    }

    private Recipe? SelectedRecipe()
    {
        return RecipesList.SelectedItem as Recipe;
    }

    private void UseAsResult_Click(object sender, RoutedEventArgs e)
    {
        var item = SelectedDbItem();
        var recipe = SelectedRecipe();

        if (item == null || recipe == null)
            return;

        recipe.RESULT = item.ClassName;
        RecipesList.Items.Refresh();
        StatusText.Text = $"RESULT = {item.ClassName}";
    }

    private void UseAsPlan_Click(object sender, RoutedEventArgs e)
    {
        var item = SelectedDbItem();
        var recipe = SelectedRecipe();

        if (item == null || recipe == null)
            return;

        recipe.PLAN = item.ClassName;
        RecipesList.Items.Refresh();
        StatusText.Text = $"PLAN = {item.ClassName}";
    }

    private void UseAsTool_Click(object sender, RoutedEventArgs e)
    {
        var item = SelectedDbItem();
        var recipe = SelectedRecipe();

        if (item == null || recipe == null)
            return;

        recipe.TOOLS.Add(item.ClassName);
        RefreshRecipeDetails();
        StatusText.Text = $"Добавлен инструмент: {item.ClassName}";
    }

    private void UseAsIngredient_Click(object sender, RoutedEventArgs e)
    {
        var item = SelectedDbItem();
        var recipe = SelectedRecipe();

        if (item == null || recipe == null)
            return;

        recipe.INGRIDIENTS.Add(new Ingredient
        {
            CLASSNAME = item.ClassName,
            ITEM_AMOUNT = 1,
            USE_ENERGY = 0,
            NEED_LIQUID = 0,
            LIQUID_TYPE = 0,
            DESTROY_ITEM = 1,
            CHANGE_HEALTH_ITEM_BY_CRAFT = 0
        });

        RefreshRecipeDetails();
        StatusText.Text = $"Добавлен ингредиент: {item.ClassName}";
    }
    private void RefreshRecipeDetails()
    {
        if (RecipesList.SelectedItem is not Recipe recipe)
            return;
    
        IngredientsGrid.ItemsSource = null;
        IngredientsGrid.ItemsSource = recipe.INGRIDIENTS;
    
        ToolsList.ItemsSource = null;
        ToolsList.ItemsSource = recipe.TOOLS;
    
        KitItemsList.ItemsSource = null;
        KitItemsList.ItemsSource = recipe.NEEDS_KIT_ITEMS;
    
        SpecialItemsList.ItemsSource = null;
        SpecialItemsList.ItemsSource = recipe.NEEDS_SPECIAL_ITEMS;
    }
    private void ImportTypesXml_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        var imported = ItemDatabaseService.ImportFromTypesXml(dialog.FileName);

        AddImportedItems(imported);

        StatusText.Text = $"Импортировано из types.xml: {imported.Count}";
    }
    private void ImportTypesFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Выбери папку с types.xml / custom types"
        };

        if (dialog.ShowDialog() != true)
            return;

        var imported = ItemDatabaseService.ImportFromTypesFolder(dialog.FolderName);

        AddImportedItems(imported);

        StatusText.Text = $"Импортировано из папки types: {imported.Count}";
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

        StatusText.Text = $"Добавлено новых предметов: {added}";
    }
    private void ValidateRecipes_Click(object sender, RoutedEventArgs e)
    {
        var errors = new List<string>();

        foreach (var recipe in _recipes)
        {
            if (string.IsNullOrWhiteSpace(recipe.RESULT))
            {
                errors.Add($"[{recipe.RECIPE_NAME}] RESULT пустой");
            }
            else if (!ClassNameExists(recipe.RESULT))
            {
                errors.Add($"[{recipe.RECIPE_NAME}] RESULT не найден: {recipe.RESULT}");
            }

            foreach (var tool in recipe.TOOLS)
            {
                if (string.IsNullOrWhiteSpace(tool))
                {
                    errors.Add($"[{recipe.RECIPE_NAME}] Пустой TOOL");
                }
                else if (!ClassNameExists(tool))
                {
                    errors.Add($"[{recipe.RECIPE_NAME}] TOOL не найден: {tool}");
                }
            }

            foreach (var ingredient in recipe.INGRIDIENTS)
            {
                if (string.IsNullOrWhiteSpace(ingredient.CLASSNAME))
                {
                    errors.Add($"[{recipe.RECIPE_NAME}] Пустой INGREDIENT");
                }
                else if (!ClassNameExists(ingredient.CLASSNAME))
                {
                    errors.Add($"[{recipe.RECIPE_NAME}] INGREDIENT не найден: {ingredient.CLASSNAME}");
                }
            }
        }

        if (errors.Count == 0)
        {
            MessageBox.Show(
                "Ошибок не найдено.",
                "Проверка рецептов",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        var text = string.Join(Environment.NewLine, errors);

        MessageBox.Show(
            text,
            $"Найдено ошибок: {errors.Count}",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private bool ClassNameExists(string className)
    {
        return _itemDatabase.Any(x =>
            x.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase));
    }

    private void SetupResultAutoComplete()
    {
        ResultComboBox.ItemsSource = _itemDatabase;
    }

    private void ResultComboBox_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        var text = ResultComboBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(text))
        {
            ResultComboBox.ItemsSource = _itemDatabase;
            ResultComboBox.IsDropDownOpen = false;
            return;
        }

        var filtered = _itemDatabase
            .Where(x =>
                x.ClassName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                x.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                x.Category.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                x.SourceMod.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(50)
            .ToList();

        ResultComboBox.ItemsSource = filtered;
        ResultComboBox.IsDropDownOpen = filtered.Count > 0;

        var editableTextBox = ResultComboBox.Template.FindName("PART_EditableTextBox", ResultComboBox)
            as TextBox;

        if (editableTextBox != null)
        {
            editableTextBox.Text = text;
            editableTextBox.CaretIndex = text.Length;
        }
    }

    private void ResultComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe)
            return;

        if (ResultComboBox.SelectedItem is ItemDatabaseEntry item)
        {
            recipe.RESULT = item.ClassName;
            ResultComboBox.Text = item.ClassName;
        }
    }
    private void SetupPlanAutoComplete()
    {
        PlanComboBox.ItemsSource = _itemDatabase;
    }

    private void PlanComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        var text = PlanComboBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(text))
        {
            PlanComboBox.ItemsSource = _itemDatabase;
            PlanComboBox.IsDropDownOpen = false;
            return;
        }

        var filtered = _itemDatabase
            .Where(x =>
                x.ClassName.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(50)
            .ToList();

        PlanComboBox.ItemsSource = filtered;
        PlanComboBox.IsDropDownOpen = filtered.Count > 0;
    }

    private void PlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe)
            return;

        if (PlanComboBox.SelectedItem is ItemDatabaseEntry item)
        {
            recipe.PLAN = item.ClassName;
            PlanComboBox.Text = item.ClassName;
        }
    }
    private void SetupToolAutoComplete()
    {
        ToolAutoCompleteBox.ItemsSource = _itemDatabase;
    }
    private void ToolAutoCompleteBox_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        var text = ToolAutoCompleteBox.Text?.Trim() ?? "";

        var filtered = _itemDatabase
            .Where(x =>
                x.ClassName.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(50)
            .ToList();

        ToolAutoCompleteBox.ItemsSource = filtered;
        ToolAutoCompleteBox.IsDropDownOpen = filtered.Count > 0;
    }
    private void AddToolFromAutoComplete_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe)
            return;

        var text = ToolAutoCompleteBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return;

        recipe.TOOLS.Add(text);

        RefreshRecipeDetails();

        ToolAutoCompleteBox.Text = "";
    }
    private void SetupIngredientAutoComplete()
    {
        IngredientAutoCompleteBox.ItemsSource = _itemDatabase;
    }
    private void IngredientAutoCompleteBox_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        var text = IngredientAutoCompleteBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(text))
        {
            IngredientAutoCompleteBox.IsDropDownOpen = false;
            return;
        }

        var filtered = _itemDatabase
            .Where(x =>
                x.ClassName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                x.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(50)
            .ToList();

        IngredientAutoCompleteBox.ItemsSource = filtered;
        IngredientAutoCompleteBox.IsDropDownOpen = filtered.Count > 0;
    }
    private void AddIngredientFromAutoComplete_Click(object sender, RoutedEventArgs e)
    {
        if (RecipesList.SelectedItem is not Recipe recipe)
            return;

        var value = IngredientAutoCompleteBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(value))
            return;

        recipe.INGRIDIENTS.Add(new Ingredient
        {
            CLASSNAME = value,
            ITEM_AMOUNT = 1,
            USE_ENERGY = 0,
            NEED_LIQUID = 0,
            LIQUID_TYPE = 0,
            DESTROY_ITEM = 1,
            CHANGE_HEALTH_ITEM_BY_CRAFT = 0
        });

        IngredientAutoCompleteBox.Text = "";

        RefreshRecipeDetails();
    }
    private void OpenGmConfigWindow_Click(object sender, RoutedEventArgs e)
    {
        var window = new GmConfigWindow();
        window.Owner = this;
        window.ShowDialog();
    }
}
