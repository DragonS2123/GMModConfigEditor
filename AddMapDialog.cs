using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace GMCraftTableEditor;

/// <summary>Диалог добавления пользовательской карты.</summary>
public class AddMapDialog : Window
{
    public string MapKey  { get; private set; } = "";
    public string MapName { get; private set; } = "";
    public string TileUrl { get; private set; } = ""; // не используется в новой версии
    public int    MapSize { get; private set; } = 15360;
    public int    MaxZoom { get; private set; } = 7;

    private readonly System.Windows.Controls.TextBox _tbName    = new() { Margin = new System.Windows.Thickness(0,0,0,8) };
    private readonly System.Windows.Controls.TextBox _tbSize    = new() { Text = "15360", Margin = new System.Windows.Thickness(0,0,0,8) };
    private readonly System.Windows.Controls.TextBox _tbMaxZoom = new() { Text = "7", Margin = new System.Windows.Thickness(0,0,0,16) };

    public AddMapDialog()
    {
        Title  = "Добавить свою карту";
        Width  = 480;
        Height = 280;
        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var stack = new StackPanel { Margin = new System.Windows.Thickness(16) };

        stack.Children.Add(MakeLabel("Название карты (для отображения в ComboBox)"));
        stack.Children.Add(_tbName);

        var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        var left = new StackPanel { Width = 210, Margin = new System.Windows.Thickness(0,0,14,0) };
        left.Children.Add(MakeLabel("Размер мира (единиц DayZ)"));
        left.Children.Add(_tbSize);
        var right = new StackPanel { Width = 180 };
        right.Children.Add(MakeLabel("Макс. зум (обычно 7)"));
        right.Children.Add(_tbMaxZoom);
        row.Children.Add(left); row.Children.Add(right);
        stack.Children.Add(row);

        stack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Папка с тайлами должна лежать в:\n" +
                   "  assets\\maps\\НазваниеПапки\\tiles\\{z}\\{x}\\{y}.webp",
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new System.Windows.Thickness(0,0,0,14),
        });

        var btnRow = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
        };
        var ok     = new System.Windows.Controls.Button { Content="Добавить", Width=90, Margin=new System.Windows.Thickness(0,0,8,0), IsDefault=true };
        var cancel = new System.Windows.Controls.Button { Content="Отмена",   Width=80, IsCancel=true };
        ok.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_tbName.Text))
            { System.Windows.MessageBox.Show("Введи название", "Ошибка"); return; }
            MapName = _tbName.Text.Trim();
            MapKey  = MapName.ToLower().Replace(" ", "_");
            MapSize = int.TryParse(_tbSize.Text, out var s) ? s : 15360;
            MaxZoom = int.TryParse(_tbMaxZoom.Text, out var z) ? z : 7;
            DialogResult = true;
        };
        cancel.Click += (_, _) => DialogResult = false;
        btnRow.Children.Add(ok); btnRow.Children.Add(cancel);
        stack.Children.Add(btnRow);

        Content = stack;
    }

    private static System.Windows.Controls.TextBlock MakeLabel(string text) =>
        new() { Text=text, Margin=new System.Windows.Thickness(0,0,0,4), FontSize=12 };
}
