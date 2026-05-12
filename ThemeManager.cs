using System.Windows;
using System.Windows.Media;

namespace GMCraftTableEditor;

/// <summary>
/// Меняет тему всего приложения программно через Application.Resources.
/// Все окна используют DynamicResource → обновляются мгновенно.
/// Не зависит от внешних файлов тем.
/// </summary>
public static class ThemeManager
{
    public static bool IsDark { get; private set; } = true;

    // ── Тёмная палитра ───────────────────────────────────────────────────────
    private static readonly (string Key, Color Value)[] DarkColors =
    {
        ("AppBackground",    Color.FromRgb(18,  23,  31)),
        ("PanelBackground",  Color.FromRgb(28,  34,  44)),
        ("PanelBackground2", Color.FromRgb(38,  45,  57)),
        ("InputBackground",  Color.FromRgb(22,  27,  36)),
        ("TextBrush",        Color.FromRgb(235, 239, 245)),
        ("MutedTextBrush",   Color.FromRgb(138, 149, 163)),
        ("BorderBrushSoft",  Color.FromRgb(55,  65,  80)),
        ("AccentBrush",      Color.FromRgb(21,  154, 140)),
        ("AccentHover",      Color.FromRgb(17,  184, 167)),
        ("DangerBrush",      Color.FromRgb(201, 69,  69)),
        ("SelectionBrush",   Color.FromRgb(37,  99,  235)),
    };

    // ── Светлая палитра ──────────────────────────────────────────────────────
    private static readonly (string Key, Color Value)[] LightColors =
    {
        ("AppBackground",    Color.FromRgb(240, 244, 248)),
        ("PanelBackground",  Color.FromRgb(255, 255, 255)),
        ("PanelBackground2", Color.FromRgb(232, 238, 244)),
        ("InputBackground",  Color.FromRgb(255, 255, 255)),
        ("TextBrush",        Color.FromRgb(21,  26,  32)),
        ("MutedTextBrush",   Color.FromRgb(89,  99,  111)),
        ("BorderBrushSoft",  Color.FromRgb(200, 208, 218)),
        ("AccentBrush",      Color.FromRgb(14,  138, 126)),
        ("AccentHover",      Color.FromRgb(11,  181, 166)),
        ("DangerBrush",      Color.FromRgb(201, 69,  69)),
        ("SelectionBrush",   Color.FromRgb(37,  99,  235)),
    };

    public static void Apply(bool dark)
    {
        IsDark = dark;
        var res    = Application.Current.Resources;
        var colors = dark ? DarkColors : LightColors;

        foreach (var (key, color) in colors)
        {
            // Всегда создаём новый brush — старый из XAML может быть заморожен (Frozen)
            res[key] = new SolidColorBrush(color);
        }
    }

    public static void Toggle() => Apply(!IsDark);
}
