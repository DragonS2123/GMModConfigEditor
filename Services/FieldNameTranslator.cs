namespace GMCraftTableEditor.Services;

public static class FieldNameTranslator
{
    private static readonly Dictionary<string, string> Ru = new()
    {
        ["RECIPE_NAME"] = "Название рецепта",
        ["DESCRIPTION"] = "Описание",
        ["CATEGORY"] = "Категория",
        ["RESULT"] = "Результат",
        ["RESULT_COUNT"] = "Количество результата",
        ["RESULT_HEALTH"] = "Состояние результата",
        ["TIME_TO_CREATE"] = "Время создания",
        ["GIVE_POINTS"] = "Очки опыта",
        ["PLAN"] = "Чертёж",
        ["DAMAGE_TO_PLAN"] = "Урон чертежу",
        ["ABILITY"] = "Навык",
        ["REQUIRED_LEVEL"] = "Требуемый уровень",

        ["WEAPON_REPAIR"] = "Ремонт оружия",
        ["DISMANTLE"] = "Разбор",
        ["NEED_ELECTRICITY"] = "Нужно электричество",
        ["OTHER_REPAIR"] = "Другой ремонт",
        ["REMOTE_CRAFT"] = "Удалённый крафт",
        ["NEED_SPECIAL_ITEMS"] = "Нужны спец. предметы",

        ["NEED_COUNT_KIT"] = "Количество наборов",
        ["LIQUID_TYPE"] = "Тип жидкости",
        ["MAX_CONSUMPTION"] = "Макс. расход",
        ["RESULT_LIQUID"] = "Жидкость результата",
        ["MIN_CONSUMPTION"] = "Мин. расход",

        ["TOOLS"] = "Инструменты",
        ["NEEDS_KIT_ITEMS"] = "Наборы",
        ["MIN_TOOLS_DMG"] = "Мин. урон инструмента",
        ["MAX_TOOLS_DMG"] = "Макс. урон инструмента",
        ["MIN_ATTACH_DMG"] = "Мин. урон предмета",
        ["MAX_ATTACH_DMG"] = "Макс. урон предмета",
    };

    public static string T(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return key;

        return Ru.TryGetValue(key, out var value) ? value : key;
    }
}