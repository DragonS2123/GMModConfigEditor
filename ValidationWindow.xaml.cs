using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GMCraftTableEditor.Models;
using Microsoft.Win32;

namespace GMCraftTableEditor;

public partial class ValidationWindow : Window
{
    // ─── Модель ──────────────────────────────────────────────────────────────

    public enum Sev { Error, Warning, Info }

    public class ValidationIssue
    {
        public Sev    Severity     { get; init; }
        public string SeverityIcon => Severity switch { Sev.Error => "❌", Sev.Warning => "⚠", _ => "ℹ" };
        public string Section      { get; init; } = "";
        public string ObjectName   { get; init; } = "";
        public string Field        { get; init; } = "";
        public string Message      { get; init; } = "";
        public string NavKey       { get; init; } = "";
    }

    // ─── State ───────────────────────────────────────────────────────────────

    private readonly MainWindow _main;
    private List<ValidationIssue> _allIssues = new();
    private bool _initialized;

    public ValidationWindow(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        Loaded += (_, _) => { _initialized = true; RunValidation(); };
    }

    // ─── Запуск ──────────────────────────────────────────────────────────────

    private void ReCheck_Click(object sender, RoutedEventArgs e) => RunValidation();

    private void RunValidation()
    {
        _allIssues = Validate().ToList();

        // Заполняем фильтр разделов
        var sections = new[] { "Все" }.Concat(_allIssues.Select(i => i.Section).Distinct().OrderBy(s => s)).ToList();
        SectionFilter.ItemsSource  = sections;
        SectionFilter.SelectedIndex = 0;

        ApplyFilter();
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        int errors   = _allIssues.Count(i => i.Severity == Sev.Error);
        int warnings = _allIssues.Count(i => i.Severity == Sev.Warning);
        int infos    = _allIssues.Count(i => i.Severity == Sev.Info);

        SummaryText.Text = errors == 0 && warnings == 0
            ? "✅ Ошибок не найдено"
            : $"❌ Ошибок: {errors}   ⚠ Предупреждений: {warnings}   ℹ Инфо: {infos}";

        SubText.Text = $"Проверено разделов: {_allIssues.Select(i => i.Section).Distinct().Count()}  •  " +
                       $"Всего записей: {_allIssues.Count}";
    }

    // ─── Фильтрация ──────────────────────────────────────────────────────────

    private void Filter_Changed(object sender, RoutedEventArgs e) => ApplyFilter();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        if (!_initialized) return;
        var showErr  = ShowErrors.IsChecked   == true;
        var showWarn = ShowWarnings.IsChecked == true;
        var showInfo = ShowInfo.IsChecked     == true;
        var section  = SectionFilter.SelectedItem?.ToString() ?? "Все";

        var filtered = _allIssues.Where(i =>
            (i.Severity == Sev.Error   && showErr)  ||
            (i.Severity == Sev.Warning && showWarn) ||
            (i.Severity == Sev.Info    && showInfo)
        );

        if (section != "Все")
            filtered = filtered.Where(i => i.Section == section);

        var list = filtered.ToList();
        ResultsGrid.ItemsSource = list;
        CountText.Text = $"Показано: {list.Count} из {_allIssues.Count}";
    }

    // ─── Навигация ───────────────────────────────────────────────────────────

    private void ResultsGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => Navigate();

    private void Navigate_Click(object sender, RoutedEventArgs e) => Navigate();

    private void ResultsGrid_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ResultsGrid.SelectedItem is not ValidationIssue issue) return;

        var menu = new ContextMenu();

        // Копировать проблемное значение
        if (!string.IsNullOrWhiteSpace(issue.ObjectName) && issue.ObjectName != "—")
        {
            var copyObj = new MenuItem { Header = $"📋  Копировать: {issue.ObjectName}" };
            copyObj.Click += (_, _) => System.Windows.Clipboard.SetText(issue.ObjectName);
            menu.Items.Add(copyObj);
        }

        // Копировать текст ошибки
        var copyMsg = new MenuItem { Header = $"📋  Копировать ошибку" };
        copyMsg.Click += (_, _) =>
            System.Windows.Clipboard.SetText($"[{issue.Section}] {issue.ObjectName} | {issue.Field}: {issue.Message}");
        menu.Items.Add(copyMsg);

        ResultsGrid.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private void Navigate()
    {
        if (ResultsGrid.SelectedItem is not ValidationIssue issue) return;
        var idx = Array.IndexOf(_main.GetPageIds(), issue.NavKey);
        if (idx >= 0)
        {
            _main.NavigateToPublic(idx);
            _main.SelectItemByName(issue.NavKey, issue.ObjectName);
            _main.Focus();
        }
    }

    // ─── Экспорт ─────────────────────────────────────────────────────────────

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt", FileName = "validation_report.txt" };
        if (dlg.ShowDialog() != true) return;

        var lines = new List<string>
        {
            $"GM Mod Config Editor — Отчёт о проверке",
            $"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}",
            $"Ошибок: {_allIssues.Count(i => i.Severity == Sev.Error)}  " +
            $"Предупреждений: {_allIssues.Count(i => i.Severity == Sev.Warning)}",
            new string('─', 80)
        };

        foreach (var g in _allIssues.GroupBy(i => i.Section))
        {
            lines.Add($"\n[{g.Key}]");
            foreach (var issue in g)
                lines.Add($"  {issue.SeverityIcon} {issue.ObjectName} | {issue.Field}: {issue.Message}");
        }

        File.WriteAllLines(dlg.FileName, lines);
        System.Diagnostics.Process.Start("notepad.exe", dlg.FileName);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ВАЛИДАЦИЯ
    // ════════════════════════════════════════════════════════════════════════

    private IEnumerable<ValidationIssue> Validate()
    {
        var all = new List<ValidationIssue>();
        all.AddRange(SafeValidate("Рецепты",      ValidateRecipes));
        all.AddRange(SafeValidate("Квесты",       ValidateQuests));
        all.AddRange(SafeValidate("NPC",          ValidateNPC));
        all.AddRange(SafeValidate("Пресеты лута", ValidateLoot));
        all.AddRange(SafeValidate("Майнинг",      ValidateMineRock));
        all.AddRange(SafeValidate("Еда",          ValidateFood));
        all.AddRange(SafeValidate("Rock Points",  ValidateRockPoints));
        return all;
    }

    private List<ValidationIssue> SafeValidate(string section, Func<IEnumerable<ValidationIssue>> validator)
    {
        try
        {
            return validator().ToList();
        }
        catch (Exception ex)
        {
            return new List<ValidationIssue>
            {
                new() { Severity = Sev.Error, Section = section,
                        ObjectName = "ОШИБКА ПРОВЕРКИ", Field = "—",
                        Message = ex.Message, NavKey = "" }
            };
        }
    }

    // ─── Рецепты ─────────────────────────────────────────────────────────────

    private IEnumerable<ValidationIssue> ValidateRecipes()
    {
        var recipes = _main.GetRecipes();
        if (recipes == null || !recipes.Any())
        {
            yield return Info("Рецепты", "—", "—", "Рецепты не загружены", "Recipes");
            yield break;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in recipes)
        {
            var name = string.IsNullOrWhiteSpace(r.RECIPE_NAME) ? "(без имени)" : r.RECIPE_NAME;

            // Дубликаты имён
            if (!names.Add(r.RECIPE_NAME ?? ""))
                yield return Warn("Рецепты", name, "RECIPE_NAME", $"Дублирующееся имя рецепта", "Recipes");

            // RESULT
            if (string.IsNullOrWhiteSpace(r.RESULT))
                yield return Err("Рецепты", name, "RESULT", "Пустой RESULT", "Recipes");
            else if (!ClassExists(r.RESULT))
                yield return Err("Рецепты", name, "RESULT", $"Не найден в справочнике: {r.RESULT}", "Recipes");

            // PLAN
            if (!string.IsNullOrWhiteSpace(r.PLAN) && !ClassExists(r.PLAN))
                yield return Warn("Рецепты", name, "PLAN", $"Не найден в справочнике: {r.PLAN}", "Recipes");

            // Инструменты
            foreach (var tool in r.TOOLS)
            {
                if (string.IsNullOrWhiteSpace(tool))
                    yield return Warn("Рецепты", name, "TOOLS", "Пустой инструмент", "Recipes");
                else if (!ClassExists(tool))
                    yield return Err("Рецепты", name, "TOOLS", $"Не найден: {tool}", "Recipes");
            }

            // Ингредиенты
            if (!r.INGRIDIENTS.Any())
                yield return Warn("Рецепты", name, "INGRIDIENTS", "Нет ингредиентов", "Recipes");

            foreach (var ing in r.INGRIDIENTS)
            {
                if (string.IsNullOrWhiteSpace(ing.CLASSNAME))
                    yield return Err("Рецепты", name, "INGRIDIENTS", "Пустой CLASSNAME ингредиента", "Recipes");
                else if (!ClassExists(ing.CLASSNAME))
                    yield return Err("Рецепты", name, "INGRIDIENTS", $"Не найден: {ing.CLASSNAME}", "Recipes");

                if (ing.ITEM_AMOUNT <= 0)
                    yield return Warn("Рецепты", name, "INGRIDIENTS", $"ITEM_AMOUNT <= 0 у {ing.CLASSNAME}", "Recipes");
            }

            // Логические
            if (r.TIME_TO_CREATE <= 0)
                yield return Warn("Рецепты", name, "TIME_TO_CREATE", "Время крафта <= 0", "Recipes");
            if (r.RESULT_COUNT <= 0)
                yield return Warn("Рецепты", name, "RESULT_COUNT", "Количество результата <= 0", "Recipes");
        }
    }

    // ─── Квесты ──────────────────────────────────────────────────────────────

    private IEnumerable<ValidationIssue> ValidateQuests()
    {
        var (quests, _) = _main.GetQuests();
        if (quests == null || !quests.Any())
        {
            yield return Info("Квесты", "—", "—", "Квесты не загружены", "Quests");
            yield break;
        }

        var ids = new HashSet<int>();

        foreach (var q in quests)
        {
            var name = string.IsNullOrWhiteSpace(q.QUEST_NAME) ? $"ID={q.ID}" : q.QUEST_NAME;

            // Дублирующиеся ID
            if (!ids.Add(q.ID))
                yield return Err("Квесты", name, "ID", $"Дублирующийся ID={q.ID}", "Quests");

            // Имя
            if (string.IsNullOrWhiteSpace(q.QUEST_NAME))
                yield return Warn("Квесты", name, "QUEST_NAME", "Пустое имя квеста", "Quests");

            // GLOBAL_TYPE
            if (q.GLOBAL_TYPE < 1 || q.GLOBAL_TYPE > 14)
                yield return Warn("Квесты", name, "GLOBAL_TYPE", $"Значение {q.GLOBAL_TYPE} вне диапазона 1-14", "Quests");

            // Цели
            if (q.TARGETS == null || !q.TARGETS.Any())
                yield return Warn("Квесты", name, "TARGETS", "Нет целей", "Quests");

            foreach (var t in q.TARGETS ?? new List<QuestTarget>())
            {
                if (t.TYPE_QUEST < 1 || t.TYPE_QUEST > 14)
                    yield return Err("Квесты", name, "TARGET.TYPE_QUEST", $"Тип {t.TYPE_QUEST} вне диапазона 1-14", "Quests");

                if (string.IsNullOrWhiteSpace(t.TYPE_OBJECT))
                    yield return Warn("Квесты", name, "TARGET.TYPE_OBJECT", "Пустой TYPE_OBJECT у цели", "Quests");
                else if (t.TYPE_QUEST != 9 && t.TYPE_QUEST != 11 && !ClassExists(t.TYPE_OBJECT))
                    yield return Err("Квесты", name, "TARGET.TYPE_OBJECT", $"Не найден: {t.TYPE_OBJECT}", "Quests");

                if (t.COUNT <= 0)
                    yield return Warn("Квесты", name, "TARGET.COUNT", $"COUNT <= 0 у цели {t.TYPE_OBJECT}", "Quests");
            }

            // Награды
            foreach (var r in q.REWARDS ?? new List<QuestReward>())
            {
                if (string.IsNullOrWhiteSpace(r.TYPE_OBJECT))
                    yield return Warn("Квесты", name, "REWARD.TYPE_OBJECT", "Пустой TYPE_OBJECT у награды", "Quests");
                else if (!ClassExists(r.TYPE_OBJECT))
                    yield return Err("Квесты", name, "REWARD.TYPE_OBJECT", $"Не найден: {r.TYPE_OBJECT}", "Quests");
            }

            // Стоимость
            foreach (var cq in q.COST_QUEST ?? new List<QuestCostItem>())
            {
                if (!string.IsNullOrWhiteSpace(cq.TYPE_OBJECT) && !ClassExists(cq.TYPE_OBJECT))
                    yield return Err("Квесты", name, "COST.TYPE_OBJECT", $"Не найден: {cq.TYPE_OBJECT}", "Quests");
            }
        }
    }

    // ─── NPC ─────────────────────────────────────────────────────────────────

    private IEnumerable<ValidationIssue> ValidateNPC()
    {
        var (npcs, _) = _main.GetNPCs();
        if (npcs == null || !npcs.Any())
        {
            yield return Info("NPC", "—", "—", "NPC не загружены", "NPC");
            yield break;
        }

        var (quests, _) = _main.GetQuests();
        var questIds = quests.Select(q => q.ID.ToString()).ToHashSet();

        foreach (var npc in npcs)
        {
            var name = $"{npc.NPC_NAME} (ID={npc.NPC_ID})";

            if (string.IsNullOrWhiteSpace(npc.NPC_TYPE))
                yield return Err("NPC", name, "NPC_TYPE", "Пустой NPC_TYPE", "NPC");

            if (string.IsNullOrWhiteSpace(npc.NPC_NAME))
                yield return Warn("NPC", name, "NPC_NAME", "Пустое имя NPC", "NPC");

            if (npc.POSITION == "0 0 0" || string.IsNullOrWhiteSpace(npc.POSITION))
                yield return Warn("NPC", name, "POSITION", "Позиция не задана (0 0 0)", "NPC");

            // Проверяем что ID квестов в QUEST_START/FINISH существуют
            foreach (var idStr in (npc.QUEST_START ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(idStr) && !questIds.Contains(idStr.Trim()))
                    yield return Warn("NPC", name, "QUEST_START", $"Квест ID={idStr} не найден в загруженных квестах", "NPC");
            }
            foreach (var idStr in (npc.QUEST_FINISH ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(idStr) && !questIds.Contains(idStr.Trim()))
                    yield return Warn("NPC", name, "QUEST_FINISH", $"Квест ID={idStr} не найден", "NPC");
            }

            // Аттачменты
            foreach (var att in npc.ATTACHMENTS ?? new())
            {
                if (!string.IsNullOrWhiteSpace(att) && !ClassExists(att))
                    yield return Err("NPC", name, "ATTACHMENTS", $"Не найден: {att}", "NPC");
            }
        }
    }

    // ─── Пресеты лута ────────────────────────────────────────────────────────

    private IEnumerable<ValidationIssue> ValidateLoot()
    {
        var (presets, _) = _main.GetLootPresets();
        if (presets == null || !presets.Any())
        {
            yield return Info("Пресеты лута", "—", "—", "Пресеты не загружены", "Loot");
            yield break;
        }

        foreach (var preset in presets)
        {
            var name = $"Пресет {preset.LOOT_PRESET}";

            if (preset.ITEMS_LIST == null || !preset.ITEMS_LIST.Any())
                yield return Warn("Пресеты лута", name, "ITEMS_LIST", "Пресет пустой", "Loot");

            if (preset.STASH_POSITIONS == null || !preset.STASH_POSITIONS.Any())
                yield return Warn("Пресеты лута", name, "STASH_POSITIONS", "Нет позиций спавна", "Loot");

            foreach (var item in preset.ITEMS_LIST ?? new())
            {
                if (string.IsNullOrWhiteSpace(item.CLASSNAME))
                    yield return Err("Пресеты лута", name, "CLASSNAME", "Пустой CLASSNAME", "Loot");
                else if (!ClassExists(item.CLASSNAME))
                    yield return Err("Пресеты лута", name, "CLASSNAME", $"Не найден: {item.CLASSNAME}", "Loot");

                if (item.CHANCE_TO_CREATE < 0 || item.CHANCE_TO_CREATE > 1)
                    yield return Warn("Пресеты лута", name, "CHANCE_TO_CREATE",
                        $"Шанс {item.CHANCE_TO_CREATE} вне [0,1] у {item.CLASSNAME}", "Loot");

                if (item.COUNT_ITEM <= 0)
                    yield return Warn("Пресеты лута", name, "COUNT_ITEM",
                        $"COUNT <= 0 у {item.CLASSNAME}", "Loot");
            }
        }
    }

    // ─── MineRock ────────────────────────────────────────────────────────────

    private IEnumerable<ValidationIssue> ValidateMineRock()
    {
        var mine = _main.GetMineConfig();
        if (mine == null)
        {
            yield return Info("Майнинг", "—", "—", "GM_MineRockConfig не загружен", "MineRock");
            yield break;
        }

        // EXTRACTION_ARRAY
        foreach (var item in mine.EXTRACTION_SETTINGS?.EXTRACTION_ARRAY ?? new())
            if (!ClassExists(item))
                yield return Err("Майнинг", "EXTRACTION_ARRAY", "CLASSNAME", $"Не найден: {item}", "MineRock");

        // WASH_ARRAY
        foreach (var item in mine.WASH_SETTINGS?.WASH_ARRAY ?? new())
            if (!ClassExists(item))
                yield return Err("Майнинг", "WASH_ARRAY", "CLASSNAME", $"Не найден: {item}", "MineRock");

        // Рецепты плавки
        foreach (var r in mine.RECIPES ?? new())
        {
            var name = string.IsNullOrWhiteSpace(r.RECIPE_NAME) ? "(без имени)" : r.RECIPE_NAME;

            if (string.IsNullOrWhiteSpace(r.RESULT))
                yield return Err("Майнинг", name, "RESULT", "Пустой RESULT", "MineRock");
            else if (!ClassExists(r.RESULT))
                yield return Err("Майнинг", name, "RESULT", $"Не найден: {r.RESULT}", "MineRock");

            if (!string.IsNullOrWhiteSpace(r.SPECIAL_INGREDIENTS) && !ClassExists(r.SPECIAL_INGREDIENTS))
                yield return Warn("Майнинг", name, "SPECIAL_INGREDIENTS",
                    $"Не найден: {r.SPECIAL_INGREDIENTS}", "MineRock");

            foreach (var ing in r.INGREDIENTS)
                if (!string.IsNullOrWhiteSpace(ing) && !ClassExists(ing))
                    yield return Err("Майнинг", name, "INGREDIENTS", $"Не найден: {ing}", "MineRock");

            if (r.RESULT_QUANTITY <= 0)
                yield return Warn("Майнинг", name, "RESULT_QUANTITY", "Количество <= 0", "MineRock");
        }
    }

    // ─── Food ────────────────────────────────────────────────────────────────

    private IEnumerable<ValidationIssue> ValidateFood()
    {
        var food = _main.GetFoodConfig();
        if (food == null)
        {
            yield return Info("Еда", "—", "—", "GM_FoodConfig не загружен", "Food");
            yield break;
        }

        // FryingPan
        foreach (var item in food.Allowed_Items_to_FryingPan ?? new())
            if (!ClassExists(item))
                yield return Warn("Еда", "FryingPan", "CLASSNAME", $"Не найден: {item}", "Food");

        // IRP
        foreach (var item in food.IRP_Items ?? new())
            if (!ClassExists(item))
                yield return Warn("Еда", "IRP_Items", "CLASSNAME", $"Не найден: {item}", "Food");

        // Рецепты
        foreach (var r in food.RECIPES ?? new())
        {
            var name = string.IsNullOrWhiteSpace(r.RESULT) ? "(без результата)" : r.RESULT;

            if (string.IsNullOrWhiteSpace(r.RESULT))
                yield return Err("Еда", name, "RESULT", "Пустой RESULT", "Food");
            else if (!ClassExists(r.RESULT))
                yield return Warn("Еда", name, "RESULT", $"Не найден в справочнике: {r.RESULT}", "Food");

            if (!string.IsNullOrWhiteSpace(r.SPECIAL_INGREDIENTS) && !ClassExists(r.SPECIAL_INGREDIENTS))
                yield return Warn("Еда", name, "SPECIAL_INGREDIENTS",
                    $"Не найден: {r.SPECIAL_INGREDIENTS}", "Food");

            foreach (var ing in r.INGREDIENT_ITEMS)
                if (!string.IsNullOrWhiteSpace(ing.NAME) && !ClassExists(ing.NAME))
                    yield return Warn("Еда", name, "INGREDIENT", $"Не найден: {ing.NAME}", "Food");
        }
    }

    // ─── Rock Points ─────────────────────────────────────────────────────────

    private IEnumerable<ValidationIssue> ValidateRockPoints()
    {
        var rocks = _main.GetRockObjects();
        if (!rocks.Any())
        {
            yield return Info("Rock Points", "—", "—", "ROCK_POINTS не загружен", "RockPoints");
            yield break;
        }

        foreach (var rock in rocks)
        {
            if (string.IsNullOrWhiteSpace(rock.CLASSNAME))
                yield return Err("Rock Points", "(без имени)", "CLASSNAME", "Пустой CLASSNAME", "RockPoints");
            else if (!ClassExists(rock.CLASSNAME))
                yield return Warn("Rock Points", rock.CLASSNAME, "CLASSNAME",
                    $"Не найден в справочнике: {rock.CLASSNAME}", "RockPoints");

            if (rock.POSITION == null || !rock.POSITION.Any())
                yield return Warn("Rock Points", rock.CLASSNAME, "POSITION", "Нет позиций спавна", "RockPoints");

            if (rock.MAX_OBJECTS_ON_MAP <= 0)
                yield return Warn("Rock Points", rock.CLASSNAME, "MAX_OBJECTS_ON_MAP",
                    "MAX <= 0", "RockPoints");

            if (rock.SPAWN_CHANCE <= 0 || rock.SPAWN_CHANCE > 100)
                yield return Warn("Rock Points", rock.CLASSNAME, "SPAWN_CHANCE",
                    $"Шанс {rock.SPAWN_CHANCE}% вне диапазона 1-100", "RockPoints");
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private bool ClassExists(string className) =>
        _main.GetItemDatabase().Any(x =>
            x.ClassName.Equals(className.Trim(), StringComparison.OrdinalIgnoreCase));

    private static ValidationIssue Err (string sec, string obj, string field, string msg, string nav) =>
        new() { Severity = Sev.Error,   Section = sec, ObjectName = obj, Field = field, Message = msg, NavKey = nav };
    private static ValidationIssue Warn(string sec, string obj, string field, string msg, string nav) =>
        new() { Severity = Sev.Warning, Section = sec, ObjectName = obj, Field = field, Message = msg, NavKey = nav };
    private static ValidationIssue Info(string sec, string obj, string field, string msg, string nav) =>
        new() { Severity = Sev.Info,    Section = sec, ObjectName = obj, Field = field, Message = msg, NavKey = nav };
}
