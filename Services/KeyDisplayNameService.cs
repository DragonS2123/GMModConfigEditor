using System.Text.RegularExpressions;

namespace GMCraftTableEditor.Services;

/// <summary>
/// Переводит технические ключи JSON в читаемые названия для отображения.
/// JSON файлы не затрагиваются — только отображение в UI.
/// </summary>
public static class KeyDisplayNameService
{
    // ── Точные совпадения (Medicine + общие) ─────────────────────────────────
    private static readonly Dictionary<string, (string Ru, string En)> ExactKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        // Служебные
        { "CONFIG_VERSION",                  ("Версия конфига",              "Config Version") },
        { "TITLE",                           ("Название",                    "Title") },
        { "AUTHOR",                          ("Автор",                       "Author") },
        { "DISCORD",                         ("Discord",                     "Discord") },

        // Medicine — секции
        { "ABILITY_SETTINGS",                ("⚙ Настройки способностей",    "⚙ Ability Settings") },
        { "GENERAL_SETTINGS",                ("⚙ Общие настройки",           "⚙ General Settings") },
        { "ALCOHOL_SETTINGS",                ("⚙ Алкоголь",                  "⚙ Alcohol") },
        { "BIOHAZARD_SETTINGS",              ("⚙ Биохимия",                  "⚙ Biohazard") },
        { "BROKENARMS_SETTINGS",             ("⚙ Сломанные руки",            "⚙ Broken Arms") },
        { "BULLET_SETTINGS",                 ("⚙ Пули",                      "⚙ Bullets") },
        { "BURNS_SETTINGS",                  ("⚙ Ожоги",                     "⚙ Burns") },
        { "CONCUSSION_SETTINGS",             ("⚙ Контузия",                  "⚙ Concussion") },
        { "GANGRENE_SETTINGS",               ("⚙ Гангрена",                  "⚙ Gangrene") },
        { "HEMATOMA_SETTINGS",               ("⚙ Гематома",                  "⚙ Hematoma") },
        { "OPENWOUND_SETTINGS",              ("⚙ Открытая рана",             "⚙ Open Wound") },
        { "MENTAL_SETTINGS",                 ("⚙ Психическое расстройство",  "⚙ Mental Disorder") },
        { "RABIES_SETTINGS",                 ("⚙ Бешенство",                 "⚙ Rabies") },
        { "VIRUS_SETTINGS",                  ("⚙ Вирус",                     "⚙ Virus") },

        // Medicine — иконки (пустые — не переводим, оставляем технический ключ)
        { "ALCOHOL_ICONS",                   ("", "") },
        { "BIOHAZARD_ICONS",                 ("", "") },
        { "BROKENARMS_ICONS",                ("", "") },
        { "BULLET_ICONS",                    ("", "") },
        { "BURNS_ICONS",                     ("", "") },
        { "CONCUSSION_ICONS",                ("", "") },
        { "GANGRENE_ICONS",                  ("", "") },
        { "HEMATOMA_ICONS",                  ("", "") },
        { "OPENWOUND_ICONS",                 ("", "") },
        { "MENTAL_ICONS",                    ("", "") },
        { "RABIES_ICONS",                    ("", "") },
        { "VIRUS_ICONS",                     ("", "") },

        // Medicine — настройки способностей
        { "PointsLossLimit",                 ("Лимит потери очков",          "Points Loss Limit") },
        { "ModifiactorLossPointsForDeath",   ("Модификатор потери при смерти","Loss Modifier on Death") },
        { "ENABLE_DEBUGLOGS",                ("Включить дебаг-логи",         "Enable Debug Logs") },

        // Medicine — общие шансы
        { "CHANCE_BLEEDING_BY_ZOMBIE",       ("Шанс кровотечения от зомби",  "Bleed Chance (Zombie)") },
        { "CHANCE_BLEEDING_BY_ANIMALS",      ("Шанс кровотечения от животных","Bleed Chance (Animals)") },
        { "CHANCE_INFECT_WOUND_BY_ZOMBIE",   ("Шанс заражения раны зомби",   "Wound Infect (Zombie)") },
        { "CHANCE_INFECT_WOUND_BY_ANIMALS",  ("Шанс заражения раны животных","Wound Infect (Animals)") },
        { "INSERT_WOUND_AGENTS_BY_ZOMBIE",   ("Агентов раны от зомби",       "Wound Agents (Zombie)") },
        { "INSERT_WOUND_AGENTS_BY_ANIMALS",  ("Агентов раны от животных",    "Wound Agents (Animals)") },
        { "Enable_ExtendedSettings",         ("Расширенные настройки",       "Extended Settings") },

        // Medicine — очки способностей
        { "AbilityPointsForGiveSalineSelf",      ("Очки: капельница (себе)",     "Points: Saline (Self)") },
        { "AbilityPointsForGiveSalineTarget",    ("Очки: капельница (другому)",  "Points: Saline (Target)") },
        { "AbilityPointsForSplintSelf",          ("Очки: шина (себе)",           "Points: Splint (Self)") },
        { "AbilityPointsForSplintTarget",        ("Очки: шина (другому)",        "Points: Splint (Target)") },
        { "AbilityPointsForSewSelf",             ("Очки: зашить (себе)",         "Points: Sew (Self)") },
        { "AbilityPointsForSewTarget",           ("Очки: зашить (другому)",      "Points: Sew (Target)") },
        { "AbilityPointsForGiveBloodSelf",       ("Очки: перелить кровь (себе)", "Points: Blood (Self)") },
        { "AbilityPointsForGiveBloodTarget",     ("Очки: перелить кровь (другому)","Points: Blood (Target)") },
        { "AbilityPointsForCollectBloodSelf",    ("Очки: взять кровь (себе)",    "Points: Collect Blood (Self)") },
        { "AbilityPointsForCollectBloodTarget",  ("Очки: взять кровь (другому)", "Points: Collect Blood (Target)") },
        { "AbilityPointsForDisinfectSelf",       ("Очки: дезинфекция (себе)",    "Points: Disinfect (Self)") },
        { "AbilityPointsForDisinfectTarget",     ("Очки: дезинфекция (другому)", "Points: Disinfect (Target)") },
        { "AbilityPointsForDressingSelf",        ("Очки: перевязка (себе)",      "Points: Dressing (Self)") },
        { "AbilityPointsForDressingTarget",      ("Очки: перевязка (другому)",   "Points: Dressing (Target)") },
        { "AbilityPointsForBloodTestSelf",       ("Очки: анализ крови (себе)",   "Points: Blood Test (Self)") },
        { "AbilityPointsForBloodTestTarget",     ("Очки: анализ крови (другому)","Points: Blood Test (Target)") },
        { "AbilityPointsForBurnedWound",         ("Очки: ожог",                  "Points: Burn Wound") },
        { "AbilityPointsForEatTablets",          ("Очки: принять таблетки",      "Points: Eat Tablets") },
        { "AbilityPointsForInjectSyringe",       ("Очки: укол шприцем",          "Points: Inject Syringe") },
        { "ChanceForSkinningBrain",              ("Шанс мозга при разделке",     "Brain Skinning Chance") },

        // Medicine — алкоголь
        { "BEER_ITEMS",                      ("Список пива",                 "Beer Items") },
        { "INSERT_ALCOHOL_BY_VODKA",         ("Алкоголь от водки",           "Alcohol (Vodka)") },
        { "INSERT_ALCOHOL_BY_BEER",          ("Алкоголь от пива",            "Alcohol (Beer)") },
        { "INSERT_WATER_BY_VODKA",           ("Вода от водки",               "Water (Vodka)") },
        { "INSERT_ENERGY_BY_VODKA",          ("Энергия от водки",            "Energy (Vodka)") },
        { "INSERT_WATER_BY_BEER",            ("Вода от пива",                "Water (Beer)") },
        { "INSERT_ENERGY_BY_BEER",           ("Энергия от пива",             "Energy (Beer)") },

        // Medicine — сломанные руки
        { "CHANCE_BROKEN_ARMS",             ("Шанс сломать руки",           "Broken Arms Chance") },
        { "TAKE_ITEMS_FOR_BROKEM_ARMS",     ("Предметы для лечения рук",    "Items for Broken Arms") },

        // Medicine — пули
        { "CHANCE_BULLET_FOR_TORSO",        ("Шанс пули в туловище",        "Bullet Chance (Torso)") },
        { "CHANCE_BULLET_FOR_OTHER",        ("Шанс пули в конечности",      "Bullet Chance (Other)") },

        // Medicine — ожоги
        { "BEPANTEN_COST_HEALING",          ("Расход бепантена",            "Bepanten Cost") },
        { "TEMPERATURE_FOR_BURNS",          ("Температура ожогов",          "Burns Temperature") },
        { "TAKE_ITEMS_FOR_BURNS",           ("Предметы для ожогов",         "Items for Burns") },

        // Medicine — контузия
        { "CHANCE_FOR_CONCUSSION",          ("Шанс контузии",               "Concussion Chance") },

        // Medicine — гангрена
        { "CHANCE_FOR_GANGRENE",            ("Шанс гангрены",               "Gangrene Chance") },
        { "MINIMUM_WOUND_INFECTION_FOR_INFECT_GANGRENE", ("Мин. заражение раны для гангрены", "Min Wound Infect for Gangrene") },
        { "INSERT_GANGRENE_AGENTS",         ("Агентов гангрены",            "Gangrene Agents") },

        // Medicine — гематома
        { "CHANCE_FOR_HEMATOMA",            ("Шанс гематомы",               "Hematoma Chance") },
        { "TROXEVASIN_COST_HEALING",        ("Расход троксевазина",         "Troxevasin Cost") },

        // Medicine — открытая рана
        { "CHANCE_FOR_OPENWOUND",           ("Шанс открытой раны",          "Open Wound Chance") },

        // Medicine — психическое
        { "A_DEADLY_DISEASE",               ("Смертельная болезнь (0/1)",   "Deadly Disease (0/1)") },
        { "CHANCE_MENTAL_FOR_HIT_ZOMBIE",   ("Шанс безумия от укуса зомби", "Mental Chance (Zombie Hit)") },
        { "INSERT_AGENTS_FOR_HIT_ZOMBIE",   ("Агентов безумия от зомби",    "Mental Agents (Zombie)") },
        { "CHANCE_MENTAL_FOR_KILL_PLAYER",  ("Шанс безумия за убийство",    "Mental Chance (Kill)") },
        { "INSERT_AGENTS_FOR_KILL_PLAYER",  ("Агентов безумия за убийство", "Mental Agents (Kill)") },
        { "HEALING_AGENTS_BY_SMOKING",      ("Лечение курением",            "Healing by Smoking") },
        { "CHANCE_INFECT_MENTAL_FOR_SKINNING_ZOMBIE",  ("Шанс безумия от разделки зомби", "Mental Chance (Skinning Zombie)") },
        { "CHANCE_INFECT_MENTAL_FOR_SKINNING_PLAYER",  ("Шанс безумия от разделки игрока","Mental Chance (Skinning Player)") },
        { "INSERT_MENTAL_FOR_SKINNING_ZOMBIE",  ("Агентов безумия от зомби",  "Mental Agents (Zombie Skinning)") },
        { "INSERT_MENTAL_FOR_SKINNING_PLAYER",  ("Агентов безумия от игрока", "Mental Agents (Player Skinning)") },

        // Medicine — бешенство
        { "RABIES_ANIMALS",                 ("Животные-переносчики",        "Rabies Animals") },
        { "CHANCE_INFECT_RABIES_BY_ANIMALS",("Шанс заражения бешенством",  "Rabies Infect Chance") },
        { "INSERT_VIRUS_RABIES_BY_ANIMALS", ("Вирусов бешенства от животных","Rabies Virus Amount") },

        // Medicine — вирус
        { "VIRUS_ANIMALS",                  ("Животные-вирусоносители",     "Virus Animals") },
        { "CHANCE_INFECT_VIRUS_BY_ZOMBIE",  ("Шанс вируса от зомби",        "Virus Chance (Zombie)") },
        { "INSERT_VIRUS_AGENTS_BY_ZOMBIE",  ("Вирусных агентов от зомби",   "Virus Agents (Zombie)") },
        { "CHANCE_INFECT_VIRUS_BY_ANIMALS", ("Шанс вируса от животных",     "Virus Chance (Animals)") },
        { "INSERT_VIRUS_AGENTS_BY_ANIMALS", ("Вирусных агентов от животных","Virus Agents (Animals)") },
        { "HEAL_VANILLA_DISEASES",          ("Лечить стандартные болезни",  "Heal Vanilla Diseases") },

        // Liquid — общие настройки
        { "CanCutRotten",                        ("Резать гнилое",                "Can Cut Rotten") },
        { "MinimumQuantityItemOnCooking",         ("Мин. кол-во при варке",        "Min Quantity on Cooking") },
        { "CanBeCookByDriedItem",                 ("Варить сушёными предметами",   "Cook by Dried Item") },
        { "TemperatureFofLiquidBaffActivation",   ("Температура активации баффа",  "Buff Activation Temp") },
        { "SETTINGS_FOR_LIQUID",                  ("⚙ Настройки жидкостей",        "⚙ Liquid Settings") },
    };

    // ── Паттерны для Liquid Config (630 ключей по шаблону) ───────────────────

    // Названия болезней/баффов по префиксу
    private static readonly Dictionary<string, (string Ru, string En)> DiseaseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "CHOLERA",          ("Холера",              "Cholera") },
        { "INFLUENZA",        ("Грипп",               "Influenza") },
        { "SALMONELLA",       ("Сальмонелла",         "Salmonella") },
        { "BRAIN",            ("Мозг",                "Brain") },
        { "FOOD_POISON",      ("Пищевое отравление",  "Food Poison") },
        { "CHEMICAL_POISON",  ("Хим. отравление",     "Chemical Poison") },
        { "HEAVYMETAL",       ("Тяжёлые металлы",     "Heavy Metals") },
        { "WOUND_AGENT",      ("Заражение раны",      "Wound Agent") },
        { "BLOOD",            ("Кровь",               "Blood") },
        { "HEALTH",           ("Здоровье",            "Health") },
        { "ALCOHOL",          ("Алкоголь",            "Alcohol") },
        { "GANGRENE",         ("Гангрена",            "Gangrene") },
        { "MENTAL",           ("Психическое",         "Mental") },
        { "RABIES",           ("Бешенство",           "Rabies") },
        { "VIRUS",            ("Вирус",               "Virus") },
    };

    // Названия предметов/жидкостей по суффиксу
    private static readonly Dictionary<string, (string Ru, string En)> ItemNames = new(StringComparer.OrdinalIgnoreCase)
    {
        { "OTVAR",        ("Отвар",           "Decoction") },
        { "UHAMORE",      ("Уха (море)",      "Fish Soup (Sea)") },
        { "UHAREKA",      ("Уха (река)",      "Fish Soup (River)") },
        { "DOSHIRAK",     ("Доширак",         "Doshirak") },
        { "GRIBSOUP",     ("Грибной суп",     "Mushroom Soup") },
        { "CHICKENSOUP",  ("Куриный суп",     "Chicken Soup") },
        { "MEATSOUP",     ("Мясной суп",      "Meat Soup") },
        { "RABBITSOUP",   ("Суп из кролика",  "Rabbit Soup") },
        { "OVOSHSUP",     ("Овощной суп",     "Vegetable Soup") },
        { "MOLOKO",       ("Молоко",          "Milk") },
        { "KOMPOT",       ("Компот",          "Kompot") },
        { "TEA",          ("Чай",             "Tea") },
        { "COFFEE",       ("Кофе",            "Coffee") },
        { "SHURPA",       ("Шурпа",           "Shurpa") },
        { "BORSCH",       ("Борщ",            "Borscht") },
        { "CEREAL",       ("Каша",            "Cereal") },
        { "MARMELADE",    ("Мармелад",        "Marmalade") },
        { "PERLOVKASHA",  ("Перловая каша",   "Barley Porridge") },
        { "PLOV",         ("Плов",            "Plov") },
    };

    // Medicine — уровни (LEVEL_0..3 и craft_chance_level_N_M)
    private static readonly Regex LevelSectionRe = new(@"^LEVEL_(\d+)$", RegexOptions.IgnoreCase);
    private static readonly Regex CraftChanceRe  = new(@"^craft_chance_level_(\d+)_(\d+)$", RegexOptions.IgnoreCase);
    private static readonly Regex ChanceTestRe   = new(@"^ChanceFor(Ordinary|Electronic)Test_(\d+)$", RegexOptions.IgnoreCase);

    // Liquid — DISEASE_BAFF_LIQUID_ITEM или SETTINGS_FOR_LIQUID_ITEM
    private static readonly Regex LiquidBuffRe    = new(@"^([A-Z_]+)_BAFF_LIQUID_(.+)$", RegexOptions.IgnoreCase);
    private static readonly Regex LiquidSettingsRe = new(@"^SETTINGS_FOR_LIQUID(?:_LIQUID)?_(.+)$", RegexOptions.IgnoreCase);

    // ── Публичный метод ──────────────────────────────────────────────────────

    public static string GetDisplayName(string key, bool russian)
    {
        if (string.IsNullOrEmpty(key)) return key;

        // 1. Точное совпадение
        if (ExactKeys.TryGetValue(key, out var exact))
        {
            var name = russian ? exact.Ru : exact.En;
            return string.IsNullOrEmpty(name) ? key : name;
        }

        // 2. LEVEL_N (Medicine секции)
        var m = LevelSectionRe.Match(key);
        if (m.Success)
        {
            var lvl = m.Groups[1].Value;
            return russian ? $"── Уровень {lvl} ──" : $"── Level {lvl} ──";
        }

        // 3. craft_chance_level_N_M
        m = CraftChanceRe.Match(key);
        if (m.Success)
        {
            var lvl  = m.Groups[1].Value;
            var tier = m.Groups[2].Value;
            return russian
                ? $"Шанс крафта ур.{lvl} тир.{tier}"
                : $"Craft Chance Lvl.{lvl} Tier.{tier}";
        }

        // 4. ChanceForOrdinaryTest_N / ChanceForElectronicTest_N
        m = ChanceTestRe.Match(key);
        if (m.Success)
        {
            var kind = m.Groups[1].Value == "Ordinary"
                ? (russian ? "обычного теста" : "ordinary test")
                : (russian ? "электронного теста" : "electronic test");
            var lvl = m.Groups[2].Value;
            return russian
                ? $"Шанс {kind} ур.{lvl}"
                : $"Chance {kind} lvl.{lvl}";
        }

        // 5. DISEASE_BAFF_LIQUID_ITEM
        m = LiquidBuffRe.Match(key);
        if (m.Success)
        {
            var diseaseKey = m.Groups[1].Value;
            var itemKey    = m.Groups[2].Value;

            // Убираем LIQUID_ префикс если есть в itemKey
            if (itemKey.StartsWith("LIQUID_", StringComparison.OrdinalIgnoreCase))
                itemKey = itemKey[7..];

            var disease = DiseaseNames.TryGetValue(diseaseKey, out var dn)
                ? (russian ? dn.Ru : dn.En)
                : diseaseKey;

            // Попробуем найти числовой суффикс для NEW_N
            var newMatch = Regex.Match(itemKey, @"^NEW_(\d+)$", RegexOptions.IgnoreCase);
            string item;
            if (newMatch.Success)
                item = russian ? $"Новый предмет {newMatch.Groups[1].Value}" : $"New Item {newMatch.Groups[1].Value}";
            else
                item = ItemNames.TryGetValue(itemKey, out var itn)
                    ? (russian ? itn.Ru : itn.En)
                    : itemKey;

            return russian
                ? $"{disease} ({item})"
                : $"{disease} ({item})";
        }

        // 6. SETTINGS_FOR_LIQUID_ITEM
        m = LiquidSettingsRe.Match(key);
        if (m.Success)
        {
            var itemKey = m.Groups[1].Value;

            var newMatch = Regex.Match(itemKey, @"^NEW_(\d+)$", RegexOptions.IgnoreCase);
            string item;
            if (newMatch.Success)
                item = russian ? $"Новый предмет {newMatch.Groups[1].Value}" : $"New Item {newMatch.Groups[1].Value}";
            else
                item = ItemNames.TryGetValue(itemKey, out var itn)
                    ? (russian ? itn.Ru : itn.En)
                    : itemKey;

            return russian ? $"⚙ Настройки: {item}" : $"⚙ Settings: {item}";
        }

        // Не нашли — возвращаем оригинальный ключ
        return key;
    }

    /// <summary>Возвращает true если для ключа есть перевод.</summary>
    public static bool HasTranslation(string key)
    {
        if (ExactKeys.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v.Ru))
            return true;
        return LevelSectionRe.IsMatch(key)
            || CraftChanceRe.IsMatch(key)
            || ChanceTestRe.IsMatch(key)
            || LiquidBuffRe.IsMatch(key)
            || LiquidSettingsRe.IsMatch(key);
    }
}
