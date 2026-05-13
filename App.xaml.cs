using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace GMCraftTableEditor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Распаковываем встроенные assets при первом запуске
        ExtractEmbeddedAssets();

        // Ловим необработанные исключения в UI-потоке
        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(
                ex.Exception.ToString(),
                "Необработанная ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            ex.Handled = true;
        };

        // Ловим исключения в фоновых потоках
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            MessageBox.Show(
                ex.ExceptionObject?.ToString() ?? "Неизвестная ошибка",
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        try
        {
            // Применяем тёмную тему (цвета уже в App.xaml, это просто синхронизирует ThemeManager)
            ThemeManager.Apply(dark: true);

            var window = new MainWindow();
            window.Show();
        
    /// <summary>
    /// Распаковывает встроенные файлы assets рядом с exe при первом запуске.
    /// </summary>
    private static void ExtractEmbeddedAssets()
    {
        var assetsDir = Path.Combine(AppContext.BaseDirectory, "assets");
        Directory.CreateDirectory(assetsDir);

        var asm = Assembly.GetExecutingAssembly();
        var resources = new[]
        {
            ("assets.map.html",    Path.Combine(assetsDir, "map.html")),
            ("assets.leaflet.js",  Path.Combine(assetsDir, "leaflet.js")),
            ("assets.leaflet.css", Path.Combine(assetsDir, "leaflet.css")),
        };

        foreach (var (resourceName, destPath) in resources)
        {
            try
            {
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                // Обновляем только если изменился размер
                if (File.Exists(destPath) && new FileInfo(destPath).Length == stream.Length)
                    continue;

                using var file = File.Create(destPath);
                stream.CopyTo(file);
            }
            catch { /* не критично */ }
        }
    }
}
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Ошибка запуска GMCraftTableEditor",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
}