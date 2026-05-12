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
    private readonly MainWindow _main;
    private bool _ready;
    private string _currentMapKey = "chernarusplus";
    private readonly Dictionary<string, CustomMapConfig> _customMaps = new();
    private record CustomMapConfig(string Name, string TileUrl, int Size, int MaxZoom);

    private readonly double? _initialX;
    private readonly double? _initialZ;
    private readonly string? _initialLabel;

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

                MapView.CoreWebView2.Settings.IsWebMessageEnabled          = true;
                MapView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                MapView.CoreWebView2.WebMessageReceived += OnWebMessage;

                var htmlPath = Path.Combine(AppContext.BaseDirectory, "Assets", "map.html");
                if (!File.Exists(htmlPath))
                {
                    MessageBox.Show($"Файл не найден:\n{htmlPath}\n\nУбедись что папка Assets с map.html рядом с exe.", "Ошибка");
                    return;
                }
                MapView.CoreWebView2.Navigate("file:///" + htmlPath.Replace('\\', '/'));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка WebView2:\n{ex.Message}\n\nhttps://developer.microsoft.com/microsoft-edge/webview2/",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        };
    }

    private void OnWebMessage(object? s, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var doc  = JsonDocument.Parse(e.TryGetWebMessageAsString());
            var type = doc.RootElement.GetProperty("type").GetString();
            Dispatcher.Invoke(() =>
            {
                switch (type)
                {
                    case "ready":
                        _ready = true;
                        PopulateMapCombo();
                        if (_initialX.HasValue) GotoPosition(_initialX.Value, _initialZ!.Value, 6, _initialLabel);
                        else _ = InjectMarkers();
                        break;
                    case "coords":
                        CoordText.Text = $"X: {doc.RootElement.GetProperty("x").GetDouble():F1}   " +
                                         $"Z: {doc.RootElement.GetProperty("z").GetDouble():F1}";
                        break;
                    case "click":
                        GotoX.Text = doc.RootElement.GetProperty("x").GetDouble()
                            .ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                        GotoZ.Text = doc.RootElement.GetProperty("z").GetDouble()
                            .ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    case "markersAdded":
                        CoordText.Text = $"✓ Маркеров: {doc.RootElement.GetProperty("count").GetInt32()}";
                        break;
                }
            });
        }
        catch { }
    }

    private void PopulateMapCombo()
    {
        MapCombo.SelectionChanged -= MapCombo_Changed;
        MapCombo.Items.Clear();
        foreach (var (key, name) in new[] { ("chernarusplus","Chernarus+"), ("livonia","Livonia"), ("sakhal","Sakhal") })
            MapCombo.Items.Add(new ComboBoxItem { Content = name, Tag = key });
        if (_customMaps.Count > 0)
        {
            MapCombo.Items.Add(new ComboBoxItem { Content = "── Свои ──", IsEnabled = false });
            foreach (var (key, cfg) in _customMaps)
                MapCombo.Items.Add(new ComboBoxItem { Content = cfg.Name, Tag = key });
        }
        MapCombo.SelectedIndex = 0;
        MapCombo.SelectionChanged += MapCombo_Changed;
    }

    private void MapCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || MapCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string key) return;
        _currentMapKey = key;
        if (_customMaps.TryGetValue(key, out var cfg))
        {
            var cfgObj = new { name=cfg.Name, size=cfg.Size, tileUrl=cfg.TileUrl,
                               maxZoom=cfg.MaxZoom, minZoom=2, center=new[]{cfg.Size/2,cfg.Size/2} };
            _ = Exec($"GM.loadMap('{key}', {JsonSerializer.Serialize(JsonSerializer.Serialize(cfgObj))})");
        }
        else _ = Exec($"GM.loadMap('{key}')");
    }

    private void AddCustomMap_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddMapDialog { Owner = this };
        if (dlg.ShowDialog() != true) return;
        _customMaps[dlg.MapKey] = new CustomMapConfig(dlg.MapName, dlg.TileUrl, dlg.MapSize, dlg.MaxZoom);
        PopulateMapCombo();
        foreach (ComboBoxItem it in MapCombo.Items)
            if (it.Tag?.ToString() == dlg.MapKey) { MapCombo.SelectedItem = it; break; }
    }

    // ─── Маркеры ─────────────────────────────────────────────────────────────

    private record MarkerDto(string type, double x, double z, string label, string? extra);

    private async System.Threading.Tasks.Task InjectMarkers()
    {
        if (!_ready) return;
        var list = new List<MarkerDto>();
        if (ChkNPC.IsChecked == true) try
        {
            var (npcs,_) = _main.GetNPCs();
            foreach (var n in npcs??new()) { var p=ParseXZ(n.POSITION); if(p!=null) list.Add(new("npc",p.Value.x,p.Value.z,$"NPC: {n.NPC_NAME??"?"}",$"ID={n.NPC_ID} {n.NPC_ROLE??""}")); }
        } catch {}
        if (ChkTriggers.IsChecked == true) try
        {
            foreach (var t in _main.GetTriggers()??new()) { var p=ParseXZ(t.POSITION); if(p!=null) list.Add(new("trigger",p.Value.x,p.Value.z,$"Триггер ID={t.TRIGGER_ID}",$"Радиус: {t.RADIUS}м")); }
        } catch {}
        if (ChkRocks.IsChecked == true) try
        {
            foreach (var rock in _main.GetRockObjects()??new())
                foreach (var pos in rock.POSITION??new()) { var p=ParseXZ(pos); if(p!=null) list.Add(new("rock",p.Value.x,p.Value.z,$"⛏ {rock.CLASSNAME??"Rock"}",$"Спавн: {rock.MIN_OBJECTS_ON_MAP}-{rock.MAX_OBJECTS_ON_MAP}")); }
        } catch {}
        if (list.Count == 0) { CoordText.Text = "Нет данных — загрузи конфиги"; return; }
        await Exec($"GM.addMarkers({JsonSerializer.Serialize(JsonSerializer.Serialize(list))})");
    }

    private static (double x, double z)? ParseXZ(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var p = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        return p.Length >= 3 &&
               double.TryParse(p[0], System.Globalization.NumberStyles.Any, ci, out var x) &&
               double.TryParse(p[2], System.Globalization.NumberStyles.Any, ci, out var z)
            ? (x, z) : null;
    }

    private void GotoPosition(double x, double z, int zoom=5, string? label=null)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        _ = Exec($"GM.goto({x.ToString(ci)},{z.ToString(ci)},{zoom},{(label!=null?JsonSerializer.Serialize(label):"null")})");
    }

    private void GotoPos_Click(object sender, RoutedEventArgs e)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        if (double.TryParse(GotoX.Text, System.Globalization.NumberStyles.Any, ci, out var x) &&
            double.TryParse(GotoZ.Text, System.Globalization.NumberStyles.Any, ci, out var z))
            GotoPosition(x, z, 6, $"X:{x:F0} Z:{z:F0}");
        else MessageBox.Show("Введи числовые X и Z","Ошибка");
    }

    private async void RefreshMarkers_Click(object s, RoutedEventArgs e) => await InjectMarkers();
    private async void Markers_Changed(object s, RoutedEventArgs e) { if (_ready) await InjectMarkers(); }
    private async System.Threading.Tasks.Task<string?> Exec(string js)
    { try { return await MapView.CoreWebView2.ExecuteScriptAsync(js); } catch { return null; } }
}
