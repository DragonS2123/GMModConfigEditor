using System.IO;
using System.Windows;
using GMCraftTableEditor.Services;
using Microsoft.Win32;

namespace GMCraftTableEditor;

public partial class ProjectSetupWindow : Window
{
    public bool Confirmed { get; private set; } = false;

    public ProjectSetupWindow()
    {
        InitializeComponent();

        // Подставляем уже сохранённый путь если есть
        if (!string.IsNullOrEmpty(AppSettingsService.Current.ProjectPath))
            PathBox.Text = AppSettingsService.Current.ProjectPath;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = LanguageManager.Get("S.Setup.SelectFolder")
        };

        if (!string.IsNullOrEmpty(PathBox.Text) && Directory.Exists(PathBox.Text))
            dlg.InitialDirectory = PathBox.Text;

        if (dlg.ShowDialog() == true)
        {
            PathBox.Text = dlg.FolderName;
            StatusText.Visibility = Visibility.Collapsed;
        }
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        var path = PathBox.Text.Trim();

        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            ShowError(LanguageManager.Get("S.Setup.NotFound"));
            return;
        }

        if (!GmPathService.Configure(path, out var error))
        {
            ShowError(string.IsNullOrEmpty(error) ? LanguageManager.Get("S.Setup.NotFound") : error);
            return;
        }

        Confirmed = true;
        DialogResult = true;
        Close();
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
    }
}
