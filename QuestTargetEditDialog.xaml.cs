using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GMCraftTableEditor.Models;

namespace GMCraftTableEditor;

/// <summary>
/// Диалог редактирования одной цели квеста (TARGETS[i]).
/// Получает ссылку на базу предметов из MainWindow для автодополнения.
/// </summary>
public partial class QuestTargetEditDialog : Window
{
    // ─── Данные типов ────────────────────────────────────────────────────────

    private static readonly QuestTypeInfo[] QuestTypes =
    {
        new(1,  "Убийство",                  "Убить N персонажей/игроков указанного типа.",
            TypeObjectKind.Classname,
            "«steamId|weapon|dist» — напр. «-|-|0» (любой игрок, любым оружием, с любого расстояния)"),
        new(2,  "Поиск",                     "Найти и взять указанный предмет.",
            TypeObjectKind.Classname,
            "«health|quantity» — напр. «-1|-1» (любое состояние, любое количество)"),
        new(3,  "Поиск и доставка NPC",      "Найти предмет и доставить указанному NPC.",
            TypeObjectKind.Classname,
            "«health|quantity» — напр. «-1|-1»"),
        new(4,  "Доставка от NPC",           "Получить предмет у NPC и доставить другому.",
            TypeObjectKind.Classname,
            "«health|quantity» — напр. «-1|-1»"),
        new(5,  "Крафт",                     "Скрафтить указанный предмет.",
            TypeObjectKind.Classname,
            "Оставить пустым «»"),
        new(6,  "Рыболовство",               "Поймать указанный вид рыбы.",
            TypeObjectKind.Classname,
            "Оставить «-1|-1»"),
        new(7,  "Охота",                     "Убить указанное животное.",
            TypeObjectKind.Classname,
            "«weapon|dist» — напр. «-|0» (любым оружием, с любого расстояния)"),
        new(8,  "Садоводство",               "Собрать урожай с грядки.",
            TypeObjectKind.Garden,
            "Оставить пустым"),
        new(9,  "Исследование",              "Посетить указанные координаты.",
            TypeObjectKind.Coordinates,
            "«radius|groundLevel|showOnMap» — напр. «10|0|1» (радиус 10м, по земле, показать на карте)"),
        new(10, "Поиск схрона",              "Найти автоспавненный схрон с лутом.",
            TypeObjectKind.Classname,
            "«presetId|-|buried» — напр. «1|-|0» (пресет №1, не закопан)"),
        new(11, "Действие",                  "Выполнить указанное действие (Action).",
            TypeObjectKind.Action,
            "Оставить пустым"),
        new(12, "Зачистка зоны",             "Прийти в зону и убить всех зомби.",
            TypeObjectKind.Classname,
            "«triggerId|showOnMap» — напр. «1|1»"),
        new(13, "Зачистка объекта",          "Спавн маппинга + зачистка зомби.",
            TypeObjectKind.Classname,
            "«triggerId|mappingGroupId|showOnMap» — напр. «1|1|1»"),
        new(14, "Слесарный стол",            "Крафт/ремонт/разборка на слесарном столе.",
            TypeObjectKind.Classname,
            "«recipeName|recipeType» — recipeType: 1=крафт, 2=ремонт, 3=разборка"),
    };

    // ─── Виды панели TYPE_OBJECT ─────────────────────────────────────────────

    private enum TypeObjectKind { Classname, Coordinates, Garden, Action }

    private record QuestTypeInfo(
        int Id, string Name, string Description,
        TypeObjectKind ObjectKind, string SettingsHint)
    {
        public string Display => $"{Id} — {Name}";
    }

    // ─── State ───────────────────────────────────────────────────────────────

    private readonly List<ItemDatabaseEntry> _itemDb;
    public  QuestTarget Result { get; private set; }

    // Контролы TYPE_OBJECT (создаются динамически)
    private ComboBox?  _classnameBox;
    private TextBox?   _coordXBox, _coordYBox, _coordZBox;

    // ─── Init ────────────────────────────────────────────────────────────────

    public QuestTargetEditDialog(QuestTarget target, List<ItemDatabaseEntry> itemDb)
    {
        InitializeComponent();
        _itemDb = itemDb;
        Result  = target;

        // Заполнить список типов
        TypeCombo.ItemsSource = QuestTypes;

        LoadFromTarget(target);
    }

    // ─── Загрузка данных из Target ───────────────────────────────────────────

    private void LoadFromTarget(QuestTarget t)
    {
        // Выбрать тип (1-14 → индекс 0-13)
        var info = QuestTypes.FirstOrDefault(x => x.Id == t.TYPE_QUEST) ?? QuestTypes[0];
        TypeCombo.SelectedItem = info;   // Вызовет TypeCombo_SelectionChanged

        // Заполнить общие поля
        CountBox.Text         = t.COUNT.ToString();
        NeedLiquidBox.Text    = t.NEED_LIQUID.ToString();
        SettingsBox.Text      = t.SETTINGS;
        NameOverrideBox.Text  = t.TYPE_NAME_OVERRIDE;
        ImageOverrideBox.Text = t.IMAGE_OVERRIDE;

        // TYPE_OBJECT — после того как панель создана
        SetTypeObjectValue(info.ObjectKind, t.TYPE_OBJECT);
    }

    // ─── Смена типа → перестроить панель TYPE_OBJECT ─────────────────────────

    private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TypeCombo.SelectedItem is not QuestTypeInfo info) return;

        TypeHintText.Text      = info.Description;
        SettingsHintText.Text  = $"SETTINGS для типа {info.Id}: {info.SettingsHint}";

        RebuildTypeObjectPanel(info.ObjectKind);
    }

    private void RebuildTypeObjectPanel(TypeObjectKind kind)
    {
        TypeObjectPanel.Children.Clear();
        _classnameBox = null;
        _coordXBox = _coordYBox = _coordZBox = null;

        switch (kind)
        {
            case TypeObjectKind.Classname:
                BuildClassnamePanel();
                break;

            case TypeObjectKind.Coordinates:
                BuildCoordinatesPanel();
                break;

            case TypeObjectKind.Garden:
                BuildGardenPanel();
                break;

            case TypeObjectKind.Action:
                BuildActionPanel();
                break;
        }
    }

    // ── Панель: classname с автодополнением ──────────────────────────────────

    private void BuildClassnamePanel()
    {
        var label = MakeLabel("Classname предмета / цели:");
        TypeObjectPanel.Children.Add(label);

        _classnameBox = new ComboBox
        {
            IsEditable         = true,
            IsTextSearchEnabled= false,
            StaysOpenOnEdit    = true,
            DisplayMemberPath  = "ClassName",
            ItemsSource        = _itemDb,
            Height             = 30,
            Margin             = new Thickness(0, 4, 0, 0),
            Background         = (System.Windows.Media.Brush)Application.Current.Resources["PanelBackground"],
            Foreground         = (System.Windows.Media.Brush)Application.Current.Resources["TextBrush"],
            BorderBrush        = (System.Windows.Media.Brush)Application.Current.Resources["BorderBrushSoft"],
        };
        _classnameBox.PreviewKeyUp += ClassnameBox_PreviewKeyUp;
        TypeObjectPanel.Children.Add(_classnameBox);

        // Подсказка
        var hint = new TextBlock
        {
            Text       = "Начните вводить classname — список отфильтруется автоматически.",
            Foreground = (System.Windows.Media.Brush)Application.Current.Resources["MutedTextBrush"],
            FontSize   = 11,
            Margin     = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        TypeObjectPanel.Children.Add(hint);
    }

    private void ClassnameBox_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_classnameBox == null) return;

        var text = _classnameBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(text))
        {
            _classnameBox.ItemsSource    = _itemDb;
            _classnameBox.IsDropDownOpen = false;
            return;
        }

        var filtered = _itemDb
            .Where(x =>
                x.ClassName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                x.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                x.SourceMod.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(60)
            .ToList();

        _classnameBox.ItemsSource    = filtered;
        _classnameBox.IsDropDownOpen = filtered.Count > 0;

        if (_classnameBox.Template.FindName("PART_EditableTextBox", _classnameBox) is TextBox tb)
        {
            tb.Text       = text;
            tb.CaretIndex = text.Length;
        }
    }

    // ── Панель: координаты X Y Z ─────────────────────────────────────────────

    private void BuildCoordinatesPanel()
    {
        TypeObjectPanel.Children.Add(MakeLabel("Координаты точки (X  Y  Z):"));

        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };

        _coordXBox = MakeCoordBox("X");
        _coordYBox = MakeCoordBox("Y");
        _coordZBox = MakeCoordBox("Z");

        row.Children.Add(LabeledCoord("X:", _coordXBox));
        row.Children.Add(LabeledCoord("Y:", _coordYBox));
        row.Children.Add(LabeledCoord("Z:", _coordZBox));

        TypeObjectPanel.Children.Add(row);

        var hint = new TextBlock
        {
            Text         = "Формат DayZ: «6648.264 16.240 11136.043» (X=восток, Y=высота, Z=север).",
            Foreground   = (System.Windows.Media.Brush)Application.Current.Resources["MutedTextBrush"],
            FontSize     = 11,
            Margin       = new Thickness(0, 6, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        TypeObjectPanel.Children.Add(hint);
    }

    private static TextBox MakeCoordBox(string tag) => new()
    {
        Width  = 140,
        Height = 30,
        Tag    = tag,
        Background  = new SolidColorBrush(Color.FromRgb(22, 27, 36)),
        Foreground  = new SolidColorBrush(Color.FromRgb(235, 239, 245)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(55, 65, 80)),
        Padding     = new Thickness(6, 4, 6, 4),
    };

    private static UIElement LabeledCoord(string labelText, TextBox box)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 12, 0) };
        sp.Children.Add(new TextBlock
        {
            Text                = labelText,
            Foreground          = new SolidColorBrush(Color.FromRgb(138, 149, 163)),
            VerticalAlignment   = VerticalAlignment.Center,
            Margin              = new Thickness(0, 0, 4, 0)
        });
        sp.Children.Add(box);
        return sp;
    }

    // ── Панель: грядки (тип 8) ───────────────────────────────────────────────

    private static readonly string[] GardenClassnames =
    {
        "Plant_Pepper", "Plant_Potato", "Plant_Pumpkin", "Plant_Tomato", "Plant_Zucchini"
    };

    private void BuildGardenPanel()
    {
        TypeObjectPanel.Children.Add(MakeLabel("Тип грядки:"));

        _classnameBox = new ComboBox
        {
            Height      = 30,
            Margin      = new Thickness(0, 4, 0, 0),
            ItemsSource = GardenClassnames,
            Background  = (System.Windows.Media.Brush)Application.Current.Resources["PanelBackground"],
            Foreground  = (System.Windows.Media.Brush)Application.Current.Resources["TextBrush"],
            BorderBrush = (System.Windows.Media.Brush)Application.Current.Resources["BorderBrushSoft"],
        };
        TypeObjectPanel.Children.Add(_classnameBox);
    }

    // ── Панель: действие (тип 11) ────────────────────────────────────────────

    private void BuildActionPanel()
    {
        TypeObjectPanel.Children.Add(MakeLabel("Название экшена/действия (Action):"));

        _classnameBox = new ComboBox
        {
            IsEditable         = true,
            Height             = 30,
            Margin             = new Thickness(0, 4, 0, 0),
            Background         = (System.Windows.Media.Brush)Application.Current.Resources["PanelBackground"],
            Foreground         = (System.Windows.Media.Brush)Application.Current.Resources["TextBrush"],
            BorderBrush        = (System.Windows.Media.Brush)Application.Current.Resources["BorderBrushSoft"],
        };
        TypeObjectPanel.Children.Add(_classnameBox);

        TypeObjectPanel.Children.Add(new TextBlock
        {
            Text         = "Введите точное название action (например: ActionOpenDoors).",
            Foreground   = (System.Windows.Media.Brush)Application.Current.Resources["MutedTextBrush"],
            FontSize     = 11,
            Margin       = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
    }

    // ─── Установить/получить значение TYPE_OBJECT ────────────────────────────

    private void SetTypeObjectValue(TypeObjectKind kind, string value)
    {
        if (kind == TypeObjectKind.Coordinates)
        {
            // Парсим «X Y Z»
            var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (_coordXBox != null) _coordXBox.Text = parts.Length > 0 ? parts[0] : "";
            if (_coordYBox != null) _coordYBox.Text = parts.Length > 1 ? parts[1] : "";
            if (_coordZBox != null) _coordZBox.Text = parts.Length > 2 ? parts[2] : "";
        }
        else if (_classnameBox != null)
        {
            _classnameBox.Text = value;
            // Попробовать выбрать из списка
            if (_classnameBox.ItemsSource is IEnumerable<ItemDatabaseEntry> list)
            {
                var match = list.FirstOrDefault(x =>
                    x.ClassName.Equals(value, StringComparison.OrdinalIgnoreCase));
                if (match != null) _classnameBox.SelectedItem = match;
            }
            else if (_classnameBox.ItemsSource is string[] strArr)
            {
                if (strArr.Contains(value)) _classnameBox.SelectedItem = value;
            }
        }
    }

    private string GetTypeObjectValue()
    {
        if (_coordXBox != null)
        {
            var x = _coordXBox.Text.Trim();
            var y = _coordYBox?.Text.Trim() ?? "0";
            var z = _coordZBox?.Text.Trim() ?? "0";
            return $"{x} {y} {z}";
        }

        if (_classnameBox != null)
        {
            if (_classnameBox.SelectedItem is ItemDatabaseEntry entry)
                return entry.ClassName;
            return _classnameBox.Text?.Trim() ?? "";
        }

        return "";
    }

    // ─── Кнопки ──────────────────────────────────────────────────────────────

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (TypeCombo.SelectedItem is not QuestTypeInfo info)
        {
            MessageBox.Show("Выберите тип цели.", "Сохранение");
            return;
        }

        Result.TYPE_QUEST        = info.Id;
        Result.TYPE_OBJECT       = GetTypeObjectValue();
        Result.TYPE_NAME_OVERRIDE= NameOverrideBox.Text.Trim();
        Result.IMAGE_OVERRIDE    = ImageOverrideBox.Text.Trim();
        Result.SETTINGS          = SettingsBox.Text.Trim();

        if (int.TryParse(CountBox.Text, out var cnt))       Result.COUNT       = cnt;
        if (int.TryParse(NeedLiquidBox.Text, out var liq))  Result.NEED_LIQUID = liq;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private TextBlock MakeLabel(string text) => new()
    {
        Text       = text,
        Foreground = (System.Windows.Media.Brush)Application.Current.Resources["MutedTextBrush"],
        FontSize   = 12,
        Margin     = new Thickness(0, 0, 0, 2)
    };
}

// ─── Placeholder для ItemDatabaseEntry если нет using ───────────────────────
// (модель уже есть в Models/ItemDatabaseEntry.cs, этот using не нужен)
