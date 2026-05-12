using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using GMCraftTableEditor.Services;

namespace GMCraftTableEditor;

public partial class GmConfigWindow : Window
{
    private GMCraftTableConfig? _craftConfig;
    private GMAccessConfig? _accessConfig;

    private string? _craftConfigPath;
    private string? _accessConfigPath;

    public GmConfigWindow()
    {
        InitializeComponent();
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
    }

    private void OpenCraftConfig_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            FileName = "GM_CRAFTTABLE_CONFIG.json"
        };

        if (dlg.ShowDialog() != true)
            return;

        try
        {
            _craftConfigPath = dlg.FileName;
            _craftConfig = JsonService.Load<GMCraftTableConfig>(_craftConfigPath);

            CategoriesGrid.ItemsSource = _craftConfig.CATEGORY_LIST;

            SetStatus($"GM_CRAFTTABLE_CONFIG загружен. Категорий: {_craftConfig.CATEGORY_LIST.Count}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось открыть GM_CRAFTTABLE_CONFIG.\n\n{ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SaveCraftConfig_Click(object sender, RoutedEventArgs e)
    {
        if (_craftConfig == null)
        {
            MessageBox.Show("Сначала открой GM_CRAFTTABLE_CONFIG.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_craftConfigPath))
        {
            var dlg = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = "GM_CRAFTTABLE_CONFIG.json"
            };

            if (dlg.ShowDialog() != true)
                return;

            _craftConfigPath = dlg.FileName;
        }

        JsonService.Save(_craftConfigPath, _craftConfig);
        SetStatus($"GM_CRAFTTABLE_CONFIG сохранён: {_craftConfigPath}");
    }

    private void OpenAccessConfig_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            FileName = "GM_ACCESS_CONFIG.json"
        };

        if (dlg.ShowDialog() != true)
            return;

        try
        {
            _accessConfigPath = dlg.FileName;
            _accessConfig = JsonService.Load<GMAccessConfig>(_accessConfigPath);

            CraftTablesGrid.ItemsSource = _accessConfig.CRAFT_TABLES;
            AdminIdsList.ItemsSource = _accessConfig.ADMIN_IDS;

            SetStatus($"GM_ACCESS_CONFIG загружен. Столов: {_accessConfig.CRAFT_TABLES.Count}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Не удалось открыть GM_ACCESS_CONFIG.\n\n{ex.Message}",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SaveAccessConfig_Click(object sender, RoutedEventArgs e)
    {
        if (_accessConfig == null)
        {
            MessageBox.Show("Сначала открой GM_ACCESS_CONFIG.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_accessConfigPath))
        {
            var dlg = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = "GM_ACCESS_CONFIG.json"
            };

            if (dlg.ShowDialog() != true)
                return;

            _accessConfigPath = dlg.FileName;
        }

        JsonService.Save(_accessConfigPath, _accessConfig);
        SetStatus($"GM_ACCESS_CONFIG сохранён: {_accessConfigPath}");
    }

    private void AddAdminId_Click(object sender, RoutedEventArgs e)
    {
        if (_accessConfig == null)
        {
            MessageBox.Show("Сначала открой GM_ACCESS_CONFIG.");
            return;
        }

        var id = AdminIdText.Text.Trim();

        if (string.IsNullOrWhiteSpace(id))
            return;

        _accessConfig.ADMIN_IDS.Add(id);
        AdminIdText.Clear();

        AdminIdsList.ItemsSource = null;
        AdminIdsList.ItemsSource = _accessConfig.ADMIN_IDS;
    }

    private void DeleteAdminId_Click(object sender, RoutedEventArgs e)
    {
        if (_accessConfig == null)
            return;

        if (AdminIdsList.SelectedItem is not string id)
            return;

        _accessConfig.ADMIN_IDS.Remove(id);

        AdminIdsList.ItemsSource = null;
        AdminIdsList.ItemsSource = _accessConfig.ADMIN_IDS;
    }
    private void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_craftConfig == null)
        {
            MessageBox.Show("Сначала открой GM_CRAFTTABLE_CONFIG.");
            return;
        }
    
        _craftConfig.CATEGORY_LIST.Add(new CategoryItem
        {
            TYPE = GetNextCategoryType(),
            NAME = "Новая категория",
            RECIPE_FILENAME = "new_recipes.json",
            ICON_PATH = "GM_CraftTable_PUBLIC/gui/images/ICONS/img_craft.edds",
            PRIVATE = 0
        });
    
        CategoriesGrid.Items.Refresh();
    }
    
    private void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_craftConfig == null)
            return;
    
        if (CategoriesGrid.SelectedItem is not CategoryItem item)
            return;
    
        _craftConfig.CATEGORY_LIST.Remove(item);
        CategoriesGrid.Items.Refresh();
    }
    
    private int GetNextCategoryType()
    {
        if (_craftConfig == null || _craftConfig.CATEGORY_LIST.Count == 0)
            return 1;
    
        return _craftConfig.CATEGORY_LIST.Max(x => x.TYPE) + 1;
    }
    
    private void AddCraftTable_Click(object sender, RoutedEventArgs e)
    {
        if (_accessConfig == null)
        {
            MessageBox.Show("Сначала открой GM_ACCESS_CONFIG.");
            return;
        }
    
        _accessConfig.CRAFT_TABLES.Add(new CraftTableAccess
        {
            TABLE_ID = GetNextTableId(),
            USE_WHITELIST = 0,
            ARRAY_CATEGORY = new List<int>(),
            ALLOWED_PLAYERS = new List<string>(),
            STATUS = 0
        });
    
        CraftTablesGrid.Items.Refresh();
    }
    
    private void DeleteCraftTable_Click(object sender, RoutedEventArgs e)
    {
        if (_accessConfig == null)
            return;
    
        if (CraftTablesGrid.SelectedItem is not CraftTableAccess table)
            return;
    
        _accessConfig.CRAFT_TABLES.Remove(table);
        CraftTablesGrid.Items.Refresh();
    }
    
    private int GetNextTableId()
    {
        if (_accessConfig == null || _accessConfig.CRAFT_TABLES.Count == 0)
            return 1;
    
        return _accessConfig.CRAFT_TABLES.Max(x => x.TABLE_ID) + 1;
    }
}