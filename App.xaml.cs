using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using GMCraftTableEditor.Services;

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
            // 1. Загружаем настройки (тема, язык, путь к проекту)
            var settings = AppSettingsService.Load();

            // 2. Применяем тему
            ThemeManager.Apply(dark: settings.Theme != "light");

            // 3. Применяем язык
            LanguageManager.Apply(russian: settings.Language != "en");

            // 4. Если путь к проекту не настроен — показываем окно настройки
            if (!GmPathService.IsConfigured)
            {
                // Пробуем загрузить из сохранённых настроек
                GmPathService.LoadFromSettings();
            }

            if (!GmPathService.IsConfigured)
            {
                var setup = new ProjectSetupWindow();
                setup.ShowDialog();
            }

            // 5. Открываем главное окно (всегда, даже если путь не настроен)
            var window = new MainWindow();
            window.Show();
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

    private static void ExtractEmbeddedAssets()
    {
        var assetsDir = Path.Combine(AppContext.BaseDirectory, "assets");
        Directory.CreateDirectory(assetsDir);

        var asm = Assembly.GetExecutingAssembly();
        var files = new[]
        {
            ("assets.map.html",    "map.html"),
            ("assets.leaflet.js",  "leaflet.js"),
            ("assets.leaflet.css", "leaflet.css"),
        };

        foreach (var (resourceName, fileName) in files)
        {
            try
            {
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null) continue;
                var destPath = Path.Combine(assetsDir, fileName);
                if (File.Exists(destPath) && new FileInfo(destPath).Length == stream.Length)
                    continue;
                using var file = File.Create(destPath);
                stream.CopyTo(file);
            }
            catch { }
        }
    }
}
