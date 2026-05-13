// ═══════════════════════════════════════════════════════════════════════════
// ИНСТРУКЦИЯ: внесите следующие изменения в MainWindow.ModsLogic.cs
// ═══════════════════════════════════════════════════════════════════════════
//
// 1. В классе FlatConfigRow добавьте свойство DisplayName:
//
// public class FlatConfigRow
// {
//     public string Key         { get; set; } = "";
//     public string Value       { get; set; } = "";
//     public string Kind        { get; set; } = "";
//     public bool   IsSection   => Kind == "section";
//
//     // ДОБАВИТЬ:
//     public string DisplayName { get; set; } = "";
//     public string ShownKey    => string.IsNullOrEmpty(DisplayName) ? Key : DisplayName;
// }
//
// ───────────────────────────────────────────────────────────────────────────
//
// 2. В методе LoadFlatConfig замените строку:
//
//     rows.Add(new FlatConfigRow { Key = prop.Name, Value = val, Kind = kind });
//
// На:
//
//     rows.Add(new FlatConfigRow
//     {
//         Key         = prop.Name,
//         Value       = val,
//         Kind        = kind,
//         DisplayName = KeyDisplayNameService.GetDisplayName(prop.Name,
//                           LanguageManager.IsRussian)
//     });
//
// ───────────────────────────────────────────────────────────────────────────
//
// 3. В XAML для Medicine и Liquid гридов замените:
//
//     <DataGridTextColumn Header="Ключ" Binding="{Binding Key}" .../>
//
// На:
//
//     <DataGridTextColumn Header="{DynamicResource S.Col.Key}"
//                         Binding="{Binding ShownKey}" .../>
//
// (колонки Value и Kind оставить как есть, только Header перевести)
//
// ═══════════════════════════════════════════════════════════════════════════
// Файл ниже — только для справки, показывает итоговый вид класса:
// ═══════════════════════════════════════════════════════════════════════════

using GMCraftTableEditor.Services;

public class FlatConfigRow
{
    public string Key         { get; set; } = "";
    public string Value       { get; set; } = "";
    /// <summary>section / string / int / float / list</summary>
    public string Kind        { get; set; } = "";
    public bool   IsSection   => Kind == "section";

    /// <summary>Читаемое название (из словаря). Если пусто — показываем Key.</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Что показывать в колонке Ключ.</summary>
    public string ShownKey    => string.IsNullOrEmpty(DisplayName) ? Key : DisplayName;
}
