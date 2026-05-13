using System.IO.Compression;
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

        // Распаковываем Python и скрипты если нужно
        ExtractPythonTools();

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

    /// <summary>
    /// Возвращает папку реального .exe (работает и при PublishSingleFile).
    /// </summary>
    public static string GetExeDirectory()
    {
        var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        return !string.IsNullOrEmpty(exe)
            ? Path.GetDirectoryName(exe)!
            : AppContext.BaseDirectory;
    }

    /// <summary>
    /// Распаковывает embeddable Python и workshop_scanner.py рядом с exe
    /// при первом запуске (или если файлы отсутствуют).
    /// </summary>
    private static void ExtractPythonTools()
    {
        var exeDir     = GetExeDirectory();
        var pythonExe  = Path.Combine(exeDir, "Tools", "Python", "python.exe");
        var scriptPath = Path.Combine(exeDir, "Tools", "Scripts", "workshop_scanner.py");

        var asm = Assembly.GetExecutingAssembly();

        // ── Распаковываем Python (zip) ────────────────────────────────────
        if (!File.Exists(pythonExe))
        {
            try
            {
                using var zipStream = asm.GetManifestResourceStream("tools.python-embed.zip");
                if (zipStream != null)
                {
                    var pythonDir = Path.Combine(exeDir, "Tools", "Python");
                    Directory.CreateDirectory(pythonDir);

                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue; // папка
                        var dest = Path.Combine(pythonDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        using var src  = entry.Open();
                        using var file = File.Create(dest);
                        src.CopyTo(file);
                    }
                }
            }
            catch { /* тихо — при следующем запуске попробуем снова */ }
        }

        // ── Распаковываем workshop_scanner.py ────────────────────────────
        if (!File.Exists(scriptPath))
        {
            try
            {
                using var stream = asm.GetManifestResourceStream("tools.workshop_scanner.py");
                if (stream != null)
                {
                    var scriptsDir = Path.Combine(exeDir, "Tools", "Scripts");
                    Directory.CreateDirectory(scriptsDir);
                    using var file = File.Create(scriptPath);
                    stream.CopyTo(file);
                }
            }
            catch { }
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
