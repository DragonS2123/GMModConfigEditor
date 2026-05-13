using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using GMCraftTableEditor.Models;
using GMCraftTableEditor.Services;

namespace GMCraftTableEditor;

public partial class WorkshopImportWindow : Window
{
    private readonly MainWindow _main;
    private readonly ObservableCollection<WorkshopModItem> _mods = new();

    public WorkshopImportWindow(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        ModsList.ItemsSource = _mods;
    }

    private record WorkshopModItem(string Name, string Path);

    private void Log(string text)
    {
        Dispatcher.Invoke(() =>
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        });
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Выбери папку !Workshop"
        };

        if (dlg.ShowDialog() == true)
        {
            WorkshopPathBox.Text = dlg.FolderName;
        }
    }

    private void LoadMods_Click(object sender, RoutedEventArgs e)
    {
        _mods.Clear();

        var path = WorkshopPathBox.Text.Trim();

        if (!Directory.Exists(path))
        {
            MessageBox.Show("Папка !Workshop не найдена.");
            return;
        }

        var dirs = Directory.GetDirectories(path)
            .Where(x => Path.GetFileName(x).StartsWith("@"))
            .OrderBy(x => Path.GetFileName(x))
            .ToList();

        foreach (var dir in dirs)
        {
            _mods.Add(new WorkshopModItem(Path.GetFileName(dir), dir));
        }

        Log($"Найдено модов: {_mods.Count}");
    }

    private async void ScanSelected_Click(object sender, RoutedEventArgs e)
    {
        if (ModsList.SelectedItems.Count == 0)
        {
            MessageBox.Show("Выбери хотя бы один мод.");
            return;
        }

        Progress.Value = 0;

        var selected = ModsList.SelectedItems
            .Cast<WorkshopModItem>()
            .ToList();

        var tempDir = Path.Combine(AppContext.BaseDirectory, "temp_workshop_import");
        Directory.CreateDirectory(tempDir);

        var inputJson = Path.Combine(tempDir, "selected_mods.json");
        var outputJson = Path.Combine(tempDir, "workshop_items.json");

        var payload = selected.Select(x => new
        {
            name = x.Name,
            path = x.Path
        }).ToList();

        await File.WriteAllTextAsync(
            inputJson,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })
        );

        var pythonExe = Path.Combine(AppContext.BaseDirectory, "Tools", "Python", "python.exe");
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Tools", "Scripts", "workshop_scanner.py");

        if (!File.Exists(pythonExe))
        {
            MessageBox.Show($"Не найден Python:\n{pythonExe}");
            return;
        }

        if (!File.Exists(scriptPath))
        {
            MessageBox.Show($"Не найден скрипт:\n{scriptPath}");
            return;
        }

        Log($"Выбрано модов: {selected.Count}");
        Log("Запуск Python-сканера...");

        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{scriptPath}\" \"{inputJson}\" \"{outputJson}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi };

        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
                Log(args.Data);
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
                Log("ERR: " + args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            Log($"Python завершился с ошибкой: {process.ExitCode}");
            return;
        }

        if (!File.Exists(outputJson))
        {
            Log("JSON результата не найден.");
            return;
        }

        var json = await File.ReadAllTextAsync(outputJson);

        var items = JsonSerializer.Deserialize<List<ItemDatabaseEntry>>(json)
                    ?? new List<ItemDatabaseEntry>();

        _main.ImportItemDatabaseEntries(items);

        Progress.Value = 100;

        Log($"Готово. Импортировано classnames: {items.Count}");
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}