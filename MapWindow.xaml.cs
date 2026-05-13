using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using GMCraftTableEditor.Models;

namespace GMCraftTableEditor;

public partial class MapWindow : Window
{
    private bool _mapUiInitialized;
    private string? _currentMapKey;
    private readonly MainWindow _main;
    private readonly string _logPath = Path.Combine(AppContext.BaseDirectory, "map_debug.log");
    private void Log(string msg) {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        try { File.AppendAllText(_logPath, line + Environment.NewLine); } catch { }
    }
    private bool _ready;
    private readonly Dictionary<string, CustomMapCfg> _customMaps = new();
    private record CustomMapCfg(string Name, string TileFolder, int WorldSize, int TileCount, int MaxZoom);

    private readonly double? _initialX;
    private readonly double? _initialZ;
    private readonly string? _initialLabel;

    // Папка с тайлами рядом с exe
    private string AssetsDir
    {
        get
        {
            // Ищем папку assets в нескольких местах:
            // 1. Рядом с exe (production)
            // 2. В корне проекта (dotnet run из Debug)
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "assets"),
                Path.Combine(AppContext.BaseDirectory, "Assets"),
                // Поднимаемся из bin/Debug/net8.0-windows/ к корню проекта
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "assets"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets"),
            };
            foreach (var p in candidates)
            {
                var full = Path.GetFullPath(p);
                if (Directory.Exists(full)) return full;
            }
            // Fallback — рядом с exe
            return Path.Combine(AppContext.BaseDirectory, "assets");
        }
    }

    public MapWindow(MainWindow main, double? x = null, double? z = null, string? label = null)
    {
        InitializeComponent();
        _main = main; _initialX = x; _initialZ = z; _initialLabel = label;

        Loaded += async (_, _) =>
        {
            try
            {
                var dataDir = Path.Combine(AppContext.BaseDirectory, "webview2_data");
                Directory.CreateDirectory(dataDir);
                var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
                await MapView.EnsureCoreWebView2Async(env);

                var wv = MapView.CoreWebView2;
                wv.Settings.IsWebMessageEnabled             = true;
                wv.Settings.IsBuiltInErrorPageEnabled       = false;
                wv.Settings.AreDefaultContextMenusEnabled   = true;  // включаем для отладки
                wv.Settings.AreDevToolsEnabled              = true;
                wv.WebMessageReceived += OnWebMessage;

                File.WriteAllText(_logPath, $"=== MAP LOG {DateTime.Now} ==={Environment.NewLine}");
                Log($"AppBase: {AppContext.BaseDirectory}");
                var assetsPath = AssetsDir;
                var mapHtmlPath = Path.Combine(assetsPath, "map.html");

                // Показываем путь в заголовке для диагностики
                Dispatcher.Invoke(() => Title = $"Карта — assets: {assetsPath}");

                if (!File.Exists(mapHtmlPath))
                {
                    MessageBox.Show(
                        $"Файл map.html не найден:\n{mapHtmlPath}\n\n" +
                        "Убедись что map.html лежит в папке Assets проекта.",
                        "Ошибка");
                    return;
                }

                // Монтируем папку assets как виртуальный хост
                Log($"VirtualHost: gmmap.local -> {assetsPath}");
                wv.SetVirtualHostNameToFolderMapping(
                    "gmmap.local",
                    assetsPath,
                    CoreWebView2HostResourceAccessKind.Allow
                );

                wv.NavigationCompleted += async (_, args) =>
                {
                    if (!args.IsSuccess) return;
                    await System.Threading.Tasks.Task.Delay(1500);
                    var gmReady = await MapView.CoreWebView2.ExecuteScriptAsync("typeof window.GM !== 'undefined' ? 'yes' : 'no'");
                    Log($"GM ready: {gmReady}");
                    if (gmReady.Contains("no"))
                        await System.Threading.Tasks.Task.Delay(1000);
                    // Устанавливаем _ready и загружаем карту
                    Dispatcher.Invoke(() =>
                    {
                        Title = "GM Mod Config Editor — Карта";
                        _ready = true;
                        if (!_mapUiInitialized)
                        {
                            _mapUiInitialized = true;
                            PopulateMapCombo();
                        }
                    });
                };

                wv.Navigate($"https://gmmap.local/map.html?v={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка WebView2:\n{ex.Message}\n\n" +
                    "Скачай WebView2 Runtime:\nhttps://developer.microsoft.com/microsoft-edge/webview2/",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        };
    }

    // ─── Сообщения от JS ─────────────────────────────────────────────────────

    private void OnWebMessage(object? s, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var doc  = JsonDocument.Parse(e.TryGetWebMessageAsString());
            var type = doc.RootElement.GetProperty("type").GetString();
            Log($"WebMsg: {type}");
            Dispatcher.Invoke(() =>
            {
                switch (type)
                {
                    case "ready":
                        _ready = true;

                        if (!_mapUiInitialized)
                        {
                            _mapUiInitialized = true;
                            PopulateMapCombo();
                        }

                        break;

                    case "coords":
                        var x = doc.RootElement.GetProperty("x").GetDouble();
                        var z = doc.RootElement.GetProperty("z").GetDouble();
                        CoordText.Text = $"X: {x:F3}   Z: {z:F3}";
                        break;

                    case "click":
                        // Заполняем поля Перейти при клике на карту
                        GotoX.Text = doc.RootElement.GetProperty("x").GetDouble().ToString("F0");
                        GotoZ.Text = doc.RootElement.GetProperty("z").GetDouble().ToString("F0");
                        break;

                    case "markersAdded":
                        var cnt = doc.RootElement.GetProperty("count").GetInt32();
                        CoordText.Text = $"✓ Маркеров: {cnt}";
                        break;
                }
            });
        }
        catch { }
    }

    // ─── ComboBox карт ───────────────────────────────────────────────────────

    private void PopulateMapCombo()
    {
        MapCombo.SelectionChanged -= MapCombo_Changed;
        MapCombo.Items.Clear();

        // Сканируем папку assets/maps/ — каждая подпапка = карта
        var mapsDir = Path.Combine(AssetsDir, "maps");
        Log($"mapsDir={mapsDir} exists={Directory.Exists(mapsDir)}");
        if (Directory.Exists(mapsDir))
        {
            // GetDirectories возвращает только папки (не файлы)
            foreach (var dir in Directory.GetDirectories(mapsDir))
            {
                var folderName  = Path.GetFileName(dir); // только имя папки, без пути
                var displayName = folderName;

                // Читаем map.json для красивого названия
                foreach (var cfgName in new[] { "map.json", "config.json" })
                {
                    var cfgFile = Path.Combine(dir, cfgName);
                    if (!File.Exists(cfgFile)) continue;
                    try
                    {
                        var doc = JsonDocument.Parse(File.ReadAllText(cfgFile));
                        if (doc.RootElement.TryGetProperty("name", out var n))
                            displayName = n.GetString() ?? folderName;
                        break;
                    }
                    catch { }
                }

                // Tag = folderName (просто имя папки, без пути)
                Log($"Map found: {displayName}");
                MapCombo.Items.Add(new ComboBoxItem { Content = displayName, Tag = folderName });
            }
        }

        // Пользовательские карты с кастомным URL
        foreach (var (key, cfg) in _customMaps)
            MapCombo.Items.Add(new ComboBoxItem { Content = $"★ {cfg.Name}", Tag = $"custom:{key}" });

        if (MapCombo.Items.Count == 0)
            MapCombo.Items.Add(new ComboBoxItem
            {
                Content = "Нет карт — добавь папку в assets/maps/",
                IsEnabled = false
            });

        MapCombo.SelectionChanged += MapCombo_Changed;

        if (MapCombo.Items.Count > 0)
        {
            MapCombo.SelectedIndex = 0;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MapCombo_Changed(MapCombo, null!);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private async void MapCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || MapCombo.SelectedItem is not ComboBoxItem item || item.Tag == null)
            return;

        var tag = item.Tag.ToString() ?? "";
        var folderName = tag;

        string? cfgContent = null;
        foreach (var fn in new[] { "map.json", "config.json" })
        {
            var p = Path.Combine(AssetsDir, "maps", folderName, fn);
            if (File.Exists(p))
            {
                cfgContent = File.ReadAllText(p);
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(cfgContent))
        {
            MessageBox.Show($"Не найден map.json:\n{Path.Combine(AssetsDir, "maps", folderName)}");
            return;
        }

        var jsKey = folderName.Replace("\\", "\\\\").Replace("'", "\\'");

        // Передаём cfg через глобальную переменную чтобы избежать проблем с экранированием
        await Exec($"window.__gmCfg = {cfgContent};");
        var script = $"GM.loadMap('{jsKey}', JSON.stringify(window.__gmCfg))";
        Log($"SCRIPT: {script}");
        var r = await Exec(script);
        Log($"GM.loadMap returned: {r}");

        _currentMapKey = tag;

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            async () => await InjectMarkers());
    }

    // ─── Своя карта ──────────────────────────────────────────────────────────

    private void AddCustomMap_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddMapDialog { Owner = this };
        if (dlg.ShowDialog() != true) return;
        _customMaps[dlg.MapKey] = new CustomMapCfg(
            dlg.MapName, dlg.MapKey, dlg.MapSize, 128, dlg.MaxZoom);
        PopulateMapCombo();
        foreach (ComboBoxItem it in MapCombo.Items)
            if (it.Tag?.ToString() == $"custom:{dlg.MapKey}") { MapCombo.SelectedItem = it; break; }
    }

    // ─── Маркеры ─────────────────────────────────────────────────────────────

    private record MarkerDto(string type, double x, double z, string label, string? extra);

    private async System.Threading.Tasks.Task InjectMarkers()
    {
        if (!_ready) return;
        var list = new List<MarkerDto>();

        if (ChkNPC.IsChecked == true) try
        {
            var (npcs, _) = _main.GetNPCs();
            foreach (var n in npcs ?? new())
            {
                var p = ParseXZ(n.POSITION);
                if (p == null) continue;
                list.Add(new("npc", p.Value.x, p.Value.z,
                    $"NPC: {n.NPC_NAME ?? "?"}", $"ID={n.NPC_ID}  {n.NPC_ROLE ?? ""}"));
            }
        }
        catch { }

        if (ChkTriggers.IsChecked == true) try
        {
            foreach (var t in _main.GetTriggers() ?? new())
            {
                var p = ParseXZ(t.POSITION);
                if (p == null) continue;
                list.Add(new("trigger", p.Value.x, p.Value.z,
                    $"Триггер ID={t.TRIGGER_ID}", $"Радиус: {t.RADIUS}м"));
            }
        }
        catch { }

        if (ChkRocks.IsChecked == true) try
        {
            foreach (var rock in _main.GetRockObjects() ?? new())
                foreach (var pos in rock.POSITION ?? new())
                {
                    var p = ParseXZ(pos);
                    if (p == null) continue;
                    list.Add(new("rock", p.Value.x, p.Value.z,
                        $"⛏ {rock.CLASSNAME ?? "Rock"}",
                        $"Спавн: {rock.MIN_OBJECTS_ON_MAP}-{rock.MAX_OBJECTS_ON_MAP}"));
                }
        }
        catch { }

        if (list.Count == 0)
        {
            CoordText.Text = "Нет данных — загрузи NPC, триггеры или Rock Points";
            return;
        }

        await Exec($"GM.addMarkers({JsonSerializer.Serialize(JsonSerializer.Serialize(list))})");
    }

    private static (double x, double z)? ParseXZ(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var p  = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        return p.Length >= 3 &&
               double.TryParse(p[0], System.Globalization.NumberStyles.Any, ci, out var x) &&
               double.TryParse(p[2], System.Globalization.NumberStyles.Any, ci, out var z)
            ? (x, z) : null;
    }

    // ─── Навигация ───────────────────────────────────────────────────────────

    private void GotoPos_Click(object sender, RoutedEventArgs e)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        if (double.TryParse(GotoX.Text, System.Globalization.NumberStyles.Any, ci, out var x) &&
            double.TryParse(GotoZ.Text, System.Globalization.NumberStyles.Any, ci, out var z))
        {
            var lbl = JsonSerializer.Serialize($"X:{x:F0} Z:{z:F0}");
            _ = Exec($"GM.goto({x},{z},{7},{lbl})");
        }
        else MessageBox.Show("Введи числовые X и Z", "Ошибка");
    }

    private async void RefreshMarkers_Click(object s, RoutedEventArgs e)
        => await InjectMarkers();

    private async void Markers_Changed(object s, RoutedEventArgs e)
    {
        if (_ready) await InjectMarkers();
    }

    private async System.Threading.Tasks.Task<string?> Exec(string js)
    {
        try { return await MapView.CoreWebView2.ExecuteScriptAsync(js); }
        catch { return null; }
    }
}