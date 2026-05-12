using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GMCraftTableEditor.Models;

namespace GMCraftTableEditor;

public partial class GlobalSearchWindow : Window
{
    // ─── Результат поиска ────────────────────────────────────────────────────

    public record SearchResult(
        string Section,   // Рецепты / NPC / Квесты / ...
        string Field,     // Имя поля
        string Value,     // Значение
        string FileName,  // Имя файла
        string NavKey     // Ключ для навигации в MainWindow
    );

    // ─── Данные из MainWindow ────────────────────────────────────────────────

    private readonly MainWindow _main;

    public GlobalSearchWindow(MainWindow main)
    {
        InitializeComponent();
        _main = main;
        Loaded += (_, _) => SearchBox.Focus();
    }

    // ─── Поиск ──────────────────────────────────────────────────────────────

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Живой поиск если строка >= 3 символов
        if (SearchBox.Text.Length >= 3)
            DoSearch();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) DoSearch();
        if (e.Key == Key.Escape) Close();
    }

    private void DoSearch_Click(object sender, RoutedEventArgs e) => DoSearch();
    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        ResultsGrid.ItemsSource = null;
        StatusText.Text = "Введите запрос и нажмите Enter или кнопку Найти";
        SearchBox.Focus();
    }

    private void DoSearch()
    {
        var q = SearchBox.Text?.Trim() ?? "";
        if (q.Length < 2)
        {
            StatusText.Text = "Введите минимум 2 символа";
            return;
        }

        var results = new List<SearchResult>();

        if (ChkRecipes.IsChecked == true)  results.AddRange(SearchRecipes(q));
        if (ChkItems.IsChecked   == true)  results.AddRange(SearchItems(q));
        if (ChkQuests.IsChecked  == true)  results.AddRange(SearchQuests(q));
        if (ChkNPC.IsChecked     == true)  results.AddRange(SearchNPC(q));
        if (ChkLoot.IsChecked    == true)  results.AddRange(SearchLoot(q));
        if (ChkMods.IsChecked    == true)  results.AddRange(SearchMods(q));

        ResultsGrid.ItemsSource = results;
        StatusText.Text = results.Count == 0
            ? $"По запросу «{q}» ничего не найдено"
            : $"Найдено: {results.Count} результатов";
    }

    // ─── Поиск по разделам ──────────────────────────────────────────────────

    private IEnumerable<SearchResult> SearchRecipes(string q)
    {
        var recipes = _main.GetRecipes();
        var file    = _main.GetRecipesFileName();

        foreach (var r in recipes)
        {
            if (Match(r.RECIPE_NAME, q)) yield return new("Рецепты", "RECIPE_NAME", r.RECIPE_NAME, file, "Recipes");
            if (Match(r.RESULT, q))      yield return new("Рецепты", "RESULT",      r.RESULT,      file, "Recipes");
            if (Match(r.CATEGORY, q))    yield return new("Рецепты", "CATEGORY",    r.CATEGORY,    file, "Recipes");
            if (Match(r.PLAN, q))        yield return new("Рецепты", "PLAN",        r.PLAN,        file, "Recipes");

            foreach (var t in r.TOOLS)
                if (Match(t, q)) yield return new("Рецепты", $"TOOL [{r.RECIPE_NAME}]", t, file, "Recipes");

            foreach (var ing in r.INGRIDIENTS)
                if (Match(ing.CLASSNAME, q))
                    yield return new("Рецепты", $"INGREDIENT [{r.RECIPE_NAME}]", ing.CLASSNAME, file, "Recipes");
        }
    }

    private IEnumerable<SearchResult> SearchItems(string q)
    {
        var items = _main.GetItemDatabase();
        foreach (var item in items)
        {
            if (Match(item.ClassName, q))   yield return new("Справочник", "ClassName",   item.ClassName,  "items_database.json", "Items");
            if (Match(item.DisplayName, q)) yield return new("Справочник", "DisplayName", item.DisplayName,"items_database.json", "Items");
            if (Match(item.Category, q))    yield return new("Справочник", "Category",    item.Category,   "items_database.json", "Items");
            if (Match(item.SourceMod, q))   yield return new("Справочник", "SourceMod",   item.SourceMod,  "items_database.json", "Items");
        }
    }

    private IEnumerable<SearchResult> SearchQuests(string q)
    {
        var (quests, file) = _main.GetQuests();
        foreach (var quest in quests)
        {
            if (Match(quest.QUEST_NAME, q))    yield return new("Квесты", "QUEST_NAME",    quest.QUEST_NAME,    file, "Quests");
            if (Match(quest.DESCRIPTION, q))   yield return new("Квесты", "DESCRIPTION",   quest.DESCRIPTION,   file, "Quests");
            if (Match(quest.QUEST_REQUIRED, q))yield return new("Квесты", "QUEST_REQUIRED",quest.QUEST_REQUIRED, file, "Quests");

            foreach (var t in quest.TARGETS)
            {
                if (Match(t.TYPE_OBJECT, q))
                    yield return new("Квесты", $"TARGET.TYPE_OBJECT [{quest.QUEST_NAME}]", t.TYPE_OBJECT, file, "Quests");
            }
            foreach (var r in quest.REWARDS)
            {
                if (Match(r.TYPE_OBJECT, q))
                    yield return new("Квесты", $"REWARD.TYPE_OBJECT [{quest.QUEST_NAME}]", r.TYPE_OBJECT, file, "Quests");
            }
        }
    }

    private IEnumerable<SearchResult> SearchNPC(string q)
    {
        var (npcs, file) = _main.GetNPCs();
        foreach (var npc in npcs)
        {
            if (Match(npc.NPC_NAME, q))  yield return new("NPC", "NPC_NAME",  npc.NPC_NAME,  file, "NPC");
            if (Match(npc.NPC_TYPE, q))  yield return new("NPC", "NPC_TYPE",  npc.NPC_TYPE,  file, "NPC");
            if (Match(npc.NPC_ROLE, q))  yield return new("NPC", "NPC_ROLE",  npc.NPC_ROLE,  file, "NPC");
            if (Match(npc.POSITION, q))  yield return new("NPC", "POSITION",  npc.POSITION,  file, "NPC");

            foreach (var att in npc.ATTACHMENTS)
                if (Match(att, q)) yield return new("NPC", $"ATTACHMENT [{npc.NPC_NAME}]", att, file, "NPC");
        }
    }

    private IEnumerable<SearchResult> SearchLoot(string q)
    {
        var (presets, file) = _main.GetLootPresets();
        foreach (var preset in presets)
        {
            foreach (var item in preset.ITEMS_LIST)
            {
                if (Match(item.CLASSNAME, q))
                    yield return new("Пресеты лута", $"CLASSNAME [Пресет {preset.LOOT_PRESET}]", item.CLASSNAME, file, "Loot");
            }
        }
    }

    private IEnumerable<SearchResult> SearchMods(string q)
    {
        // Medicine flat config
        foreach (var row in _main.GetMedicineRows())
            if (Match(row.Key, q) || Match(row.Value, q))
                yield return new("Medicine", row.Key, row.Value, "GM_MEDICINE_CONFIG.json", "Medicine");

        // Liquid flat config
        foreach (var row in _main.GetLiquidRows())
            if (Match(row.Key, q) || Match(row.Value, q))
                yield return new("Liquid", row.Key, row.Value, "GM_LiquidConfig.json", "Liquid");

        // Food
        var food = _main.GetFoodConfig();
        if (food != null)
        {
            foreach (var item in food.Allowed_Items_to_FryingPan)
                if (Match(item, q)) yield return new("Еда", "FryingPan", item, "GM_FoodConfig.json", "Food");

            foreach (var r in food.RECIPES)
            {
                if (Match(r.RESULT, q)) yield return new("Еда", $"RESULT [{r.RESULT}]", r.RESULT, "GM_FoodConfig.json", "Food");
                foreach (var ing in r.INGREDIENT_ITEMS)
                    if (Match(ing.NAME, q)) yield return new("Еда", $"INGREDIENT [{r.RESULT}]", ing.NAME, "GM_FoodConfig.json", "Food");
            }
        }

        // MineRock
        var mine = _main.GetMineConfig();
        if (mine != null)
        {
            foreach (var item in mine.EXTRACTION_SETTINGS.EXTRACTION_ARRAY)
                if (Match(item, q)) yield return new("Майнинг", "EXTRACTION_ARRAY", item, "GM_MineRockConfig.json", "MineRock");

            foreach (var r in mine.RECIPES)
            {
                if (Match(r.RESULT, q)) yield return new("Майнинг", $"RESULT [{r.RECIPE_NAME}]", r.RESULT, "GM_MineRockConfig.json", "MineRock");
                foreach (var ing in r.INGREDIENTS)
                    if (Match(ing, q)) yield return new("Майнинг", $"INGREDIENT [{r.RECIPE_NAME}]", ing, "GM_MineRockConfig.json", "MineRock");
            }
        }

        // Rock Points
        var rocks = _main.GetRockObjects();
        foreach (var rock in rocks)
            if (Match(rock.CLASSNAME, q))
                yield return new("Rock Points", "CLASSNAME", rock.CLASSNAME, "ROCK_POINTS.json", "RockPoints");
    }

    // ─── Helper ─────────────────────────────────────────────────────────────

    private void ResultsGrid_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ResultsGrid.SelectedItem is not SearchResult result) return;
        if (string.IsNullOrWhiteSpace(result.Value)) return;

        var menu = new ContextMenu();
        var copy = new MenuItem { Header = $"📋  Копировать: {result.Value}" };
        copy.Click += (_, _) =>
        {
            System.Windows.Clipboard.SetText(result.Value);
            StatusText.Text = $"Скопировано: {result.Value}";
        };
        menu.Items.Add(copy);
        ResultsGrid.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private static bool Match(string? value, string query) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    // ─── Навигация ───────────────────────────────────────────────────────────

    private void ResultsGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        => Navigate();

    private void Navigate_Click(object sender, RoutedEventArgs e)
        => Navigate();

    private void Navigate()
    {
        if (ResultsGrid.SelectedItem is not SearchResult result) return;
        var idx = Array.IndexOf(_main.GetPageIds(), result.NavKey);
        if (idx >= 0) _main.NavigateToPublic(idx);
        Close();
    }
}
