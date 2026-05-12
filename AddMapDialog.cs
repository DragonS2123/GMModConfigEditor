using System.Windows;
using System.Windows.Controls;

namespace GMCraftTableEditor;

/// <summary>Диалог добавления пользовательской карты.</summary>
public class AddMapDialog : Window
{
    public string MapKey  { get; private set; } = "";
    public string MapName { get; private set; } = "";
    public string TileUrl { get; private set; } = "";
    public int    MapSize { get; private set; } = 15360;
    public int    MaxZoom { get; private set; } = 7;

    private readonly TextBox _tbName    = new() { Margin = new Thickness(0,0,0,8) };
    private readonly TextBox _tbTileUrl = new() { Margin = new Thickness(0,0,0,8) };
    private readonly TextBox _tbSize    = new() { Text = "15360", Margin = new Thickness(0,0,0,8) };
    private readonly TextBox _tbMaxZoom = new() { Text = "7",     Margin = new Thickness(0,0,0,16) };

    public AddMapDialog()
    {
        Title  = "Добавить свою карту";
        Width  = 500;
        Height = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var stack = new StackPanel { Margin = new Thickness(16) };

        stack.Children.Add(new TextBlock { Text = "Название карты", Margin = new Thickness(0,0,0,4) });
        stack.Children.Add(_tbName);

        stack.Children.Add(new TextBlock { Text = "URL тайлов ({z}/{x}/{y}.webp или .png)", Margin = new Thickness(0,0,0,4) });
        stack.Children.Add(_tbTileUrl);
        _tbTileUrl.Text = "https://static.xam.nu/dayz/maps/chernarusplus/1.27/topographic/{z}/{x}/{y}.webp";

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        var left = new StackPanel { Width = 220, Margin = new Thickness(0,0,16,0) };
        left.Children.Add(new TextBlock { Text = "Размер карты (единиц)", Margin = new Thickness(0,0,0,4) });
        left.Children.Add(_tbSize);
        var right = new StackPanel { Width = 200 };
        right.Children.Add(new TextBlock { Text = "Макс. зум (обычно 7)", Margin = new Thickness(0,0,0,4) });
        right.Children.Add(_tbMaxZoom);
        row.Children.Add(left);
        row.Children.Add(right);
        stack.Children.Add(row);

        stack.Children.Add(new TextBlock
        {
            Text = "Примеры тайлов с xam.nu:\n" +
                   "Chernarus: .../chernarusplus/1.27/topographic/{z}/{x}/{y}.webp\n" +
                   "Livonia:   .../livonia/1.25/topographic/{z}/{x}/{y}.webp",
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0,0,0,12)
        });

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var ok  = new Button { Content = "Добавить", Width = 90, Margin = new Thickness(0,0,8,0), IsDefault = true };
        var cancel = new Button { Content = "Отмена", Width = 80, IsCancel = true };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_tbName.Text) || string.IsNullOrWhiteSpace(_tbTileUrl.Text))
            { MessageBox.Show("Заполни название и URL", "Ошибка"); return; }
            MapName = _tbName.Text.Trim();
            MapKey  = MapName.ToLower().Replace(" ", "_");
            TileUrl = _tbTileUrl.Text.Trim();
            MapSize = int.TryParse(_tbSize.Text, out var s) ? s : 15360;
            MaxZoom = int.TryParse(_tbMaxZoom.Text, out var z) ? z : 7;
            DialogResult = true;
        };
        cancel.Click += (_, _) => DialogResult = false;
        btnRow.Children.Add(ok);
        btnRow.Children.Add(cancel);
        stack.Children.Add(btnRow);

        Content = stack;
    }
}
