using System.Collections.Generic;

namespace GMCraftTableEditor.Models;

// ─── GM_QuestSystemCFG.json ──────────────────────────────────────────────────

public class GMQuestSystemConfig
{
    public string TITLE { get; set; } = "";
    public string AUTHOR { get; set; } = "";
    public string DISCORD { get; set; } = "";
    public string SETTINGS { get; set; } = "";

    public int MAXIMUM_QUEST { get; set; }
    public int HIDE_QUEST { get; set; }
    public int CANCEL_QUEST { get; set; }

    public List<QuestNpc> NPC { get; set; } = new();
}

public class QuestNpc
{
    public int NPC_ID { get; set; }
    public string NPC_TYPE { get; set; } = "";
    public string NPC_NAME { get; set; } = "";
    public string NPC_NAME_FOR_ACTION { get; set; } = "";
    public string NPC_ROLE { get; set; } = "";
    public string NPC_DESCRIPTION { get; set; } = "";
    public string NPC_AVATAR_PATH { get; set; } = "";
    /// <summary>0 — координаты из POSITION, 1 — из расписания TIME_POSITION_NPC</summary>
    public int ENABLE_TRANSFER { get; set; }
    public string POSITION { get; set; } = "";
    public string ORIENTATION { get; set; } = "";
    public List<string> STATIC_EMOTE_NAME { get; set; } = new();
    public List<string> DYNAMIC_EMOTE_GOOD { get; set; } = new();
    public List<string> DYNAMIC_EMOTE_NORMAL { get; set; } = new();
    public List<string> DYNAMIC_EMOTE_BAD { get; set; } = new();
    public List<string> ATTACHMENTS { get; set; } = new();
    /// <summary>ID квестов через «|», например «1|2|3»</summary>
    public string QUEST_START { get; set; } = "";
    /// <summary>ID квестов через «|», например «1|2|3»</summary>
    public string QUEST_FINISH { get; set; } = "";
    public List<NpcDialog> DIALOG_SETTINGS { get; set; } = new();
    public NpcReputationSettings REPUTATION_SETTINGS { get; set; } = new();
}

public class NpcReputationSettings
{
    public double LOW_REPUTATION { get; set; }
    public double HIGH_REPUTATION { get; set; }
}

public class NpcDialog
{
    /// <summary>Зарезервированные: «first», «quest», «final», «quit»</summary>
    public string ID { get; set; } = "";
    public string NPC_TEXT_LOW_REPUTATION { get; set; } = "";
    public string NPC_TEXT_NORMAL { get; set; } = "";
    public string NPC_TEXT_HIGH_REPUTATION { get; set; } = "";
    public List<string> NPC_VOICE_LOW_REPUTATION { get; set; } = new();
    public List<string> NPC_VOICE_NORMAL { get; set; } = new();
    public List<string> NPC_VOICE_HIGH_REPUTATION { get; set; } = new();
    public List<NpcAnswer> ANSWERS { get; set; } = new();
}

public class NpcAnswer
{
    public string ANSWER_TEXT_LOW_REPUTATION { get; set; } = "";
    public string ANSWER_TEXT_NORMAL { get; set; } = "";
    public string ANSWER_TEXT_HIGH_REPUTATION { get; set; } = "";
    public List<string> NPC_ANS_VOICE_LOW_REPUTATION { get; set; } = new();
    public List<string> NPC_ANS_VOICE_NORMAL { get; set; } = new();
    public List<string> NPC_ANS_VOICE_HIGH_REPUTATION { get; set; } = new();
    public string NEXT_ID_DIALOG_LOW_REPUTATION { get; set; } = "";
    public string NEXT_ID_DIALOG_NORMAL_REPUTATION { get; set; } = "";
    public string NEXT_ID_DIALOG_HIGH_REPUTATION { get; set; } = "";
}

// ─── id_*.json ───────────────────────────────────────────────────────────────

public class QuestFileItem
{
    public int ID { get; set; }
    public string QUEST_NAME { get; set; } = "";
    public string DESCRIPTION { get; set; } = "";
    public string DESCRIPTION_SHORT { get; set; } = "";
    /// <summary>ID типа квеста основной цели (1-14). Влияет на аватарку.</summary>
    public int GLOBAL_TYPE { get; set; }
    /// <summary>-1 = одноразовый; N = секунд до перезарядки</summary>
    public int RELOAD_QUEST { get; set; } = -1;
    /// <summary>-1 = без лимита; N = секунд на выполнение</summary>
    public int TIME_LIMIT_FOR_QUEST { get; set; } = -1;
    /// <summary>ID квестов через «|» — должны быть выполнены для доступа</summary>
    public string QUEST_REQUIRED { get; set; } = "";
    /// <summary>ID квестов через «|» — станут недоступны после выполнения этого</summary>
    public string QUEST_UNAVAILABLE { get; set; } = "";
    public List<QuestTarget> TARGETS { get; set; } = new();
    public List<QuestReward> REWARDS { get; set; } = new();
    public List<QuestCostItem> COST_QUEST { get; set; } = new();
    public double REPUTATION_REWARD { get; set; }
    public double REPUTATION_PENALTY { get; set; }
    public double NEED_REPUTATION { get; set; }
    /// <summary>0 = берёт CANCEL_QUEST; -1 = запрет; 1 = разрешено</summary>
    public int CAN_CANCEL_QUEST { get; set; }
    /// <summary>0 = выкл; 1 = доступен только по расписанию</summary>
    public int UNIQUE { get; set; }
    public List<string> DAY_QUEST { get; set; } = new();
    public int HOUR_START { get; set; }
    public int MINUTE_START { get; set; }
    public int HOUR_FINISH { get; set; } = 23;
    public int MINUTE_FINISH { get; set; } = 59;
    /// <summary>1 = выдаётся всем при подключении (не работает для типов 2, 12, 13)</summary>
    public int IS_SPECIAL_QUEST { get; set; }
    public List<SpecialQuestSettings> IS_SPECIAL_SETTINGS { get; set; } = new();
    // Опционально — ранговая система
    public int? NEED_RANK { get; set; }
    public double? RANK_POINTS { get; set; }
}

public class QuestTarget
{
    /// <summary>
    /// 1-убийство, 2-поиск, 3-поиск+доставка, 4-доставка, 5-крафт,
    /// 6-рыбалка, 7-охота, 8-садоводство, 9-исследование, 10-схрон,
    /// 11-действие, 12-зачистка зоны, 13-зачистка объекта, 14-слесарный стол
    /// </summary>
    public int TYPE_QUEST { get; set; }
    public string TYPE_NAME_OVERRIDE { get; set; } = "";
    public string IMAGE_OVERRIDE { get; set; } = "";
    /// <summary>
    /// Тип 1,2,3,4,5,6,7,12 — classname.
    /// Тип 8 — classname грядки.
    /// Тип 9 — координаты «X Y Z».
    /// Тип 10 — classname схрона.
    /// Тип 11 — название действия.
    /// </summary>
    public string TYPE_OBJECT { get; set; } = "";
    public string DESCRIPTION { get; set; } = "";
    /// <summary>ID жидкости; -1 = не важно</summary>
    public int NEED_LIQUID { get; set; } = -1;
    public int COUNT { get; set; } = 1;
    /// <summary>
    /// Тип 1: «steamId|weapon|dist»  Тип 2,3,4: «health|quantity»
    /// Тип 7: «weapon|dist»  Тип 9: «radius|groundLevel|showOnMap»
    /// Тип 10: «presetId|-|buriedSpawn»  Тип 12: «triggerId|showOnMap»
    /// Тип 13: «triggerId|mappingGroupId|showOnMap»  Тип 14: «recipeName|recipeType»
    /// </summary>
    public string SETTINGS { get; set; } = "";
    public int SOUND_TRIGGER_PRESET { get; set; }
}

public class QuestReward
{
    public string TYPE_OBJECT { get; set; } = "";
    public string TYPE_NAME_OVERRIDE { get; set; } = "";
    public double COUNT { get; set; } = 1;
    /// <summary>Пусто или -1 = полное заполнение</summary>
    public string QUANTITY { get; set; } = "";
    public string DESCRIPTION { get; set; } = "";
    /// <summary>ID жидкости; -1 = нет</summary>
    public int SET_LIQUID { get; set; } = -1;
    /// <summary>0=нетронуто, 1=поношено, …</summary>
    public int HEALTH_LEVEL { get; set; }
    /// <summary>ID жидкости в таре как строка («2048») или «-1»</summary>
    public string SETTINGS { get; set; } = "-1";
}

public class QuestCostItem
{
    public string TYPE_OBJECT { get; set; } = "";
    public int COUNT { get; set; } = 1;
    /// <summary>ID жидкости; -1 = нет</summary>
    public int NEED_LIQUID { get; set; } = -1;
    public string NAME_OVERRIDE { get; set; } = "";
    /// <summary>Точное количество или проценты («50%»)</summary>
    public string SETTINGS { get; set; } = "";
}

public class SpecialQuestSettings
{
    public int IS_IMAGE_ENABLE { get; set; }
    public string IMAGE_PATH { get; set; } = "";
    public int IS_SOUND_ENABLE { get; set; }
    public string SOUND_SET { get; set; } = "";
}

// ─── GM_PRESET_CONFIG.json ───────────────────────────────────────────────────

public class GMPresetConfig
{
    public string TITLE { get; set; } = "";
    public string AUTHOR { get; set; } = "";
    public string DISCORD { get; set; } = "";
    public string PRESET_MOD_SETTINGS { get; set; } = "";
    public List<string> ADMIN_STEAM_ID { get; set; } = new();
    /// <summary>0/1 — включить озвучку диалогов</summary>
    public int ENABLE_NPC_VOICE { get; set; }
    public List<NpcTimePosition> TIME_POSITION_NPC { get; set; } = new();
    public List<LootPreset> PRESET_SETTINGS { get; set; } = new();
    public List<TriggerPreset> TRIGGER_SETTINGS { get; set; } = new();
    public List<MappingGroup> MAPPING_SETTINGS { get; set; } = new();
    public List<SoundTrigger> SOUND_TRIGGER { get; set; } = new();
}

// ── Расписание NPC ─────────────────────────────────────────────────────────

public class NpcTimePosition
{
    public int NPC_ID { get; set; }
    /// <summary>Радиоволна; «-1» = не требуется</summary>
    public string NEED_FREQUENCY { get; set; } = "-1";
    public string NOTIFY_BY_RADIO { get; set; } = "";
    public List<string> RADIO_CLASSNAME { get; set; } = new();
    public List<NpcTimeSchedule> TIME_SCHEDULES { get; set; } = new();
}

public class NpcTimeSchedule
{
    public string DAY { get; set; } = "Monday";
    public int ENABLE_SCHEDULE { get; set; } = 1;
    /// <summary>Задержка перемещения в секундах после рестарта</summary>
    public double TRANSFER_BETWEEN_RESTART { get; set; } = 1.0;
    public int HOUR_START { get; set; }
    public int MINUTE_START { get; set; }
    public int HOUR_FINISH { get; set; } = 23;
    public int MINUTE_FINISH { get; set; } = 59;
    public List<NpcPosSettings> POS_SETTINGS { get; set; } = new();
}

public class NpcPosSettings
{
    public string LOCATION_NAME { get; set; } = "";
    public string POSITION { get; set; } = "0 0 0";
    public string ORIENTATION { get; set; } = "0 0 0";
}

// ── Пресеты лута (схроны) ──────────────────────────────────────────────────

public class LootPreset
{
    public int LOOT_PRESET { get; set; }
    public List<string> STASH_POSITIONS { get; set; } = new();
    public List<LootPresetItem> ITEMS_LIST { get; set; } = new();
}

public class LootPresetItem
{
    public string CLASSNAME { get; set; } = "";
    /// <summary>0.0 – 1.0</summary>
    public double CHANCE_TO_CREATE { get; set; } = 1.0;
    public int COUNT_ITEM { get; set; } = 1;
    public double HEALTH_ITEM { get; set; } = 1.0;
    /// <summary>0 = без quantity; 1 = задать QUANTITY_ITEM</summary>
    public int HAS_QUANTITY { get; set; }
    public double QUANTITY_ITEM { get; set; }
    public List<LootPresetAttachment> ATTACHMENTS { get; set; } = new();
}

public class LootPresetAttachment
{
    public string CLASSNAME { get; set; } = "";
    public double CHANCE_TO_SPAWN { get; set; } = 1.0;
    public double HEALTH { get; set; } = 1.0;
    public int HAS_QUANTITY { get; set; }
    public double QUANTITY { get; set; }
}

// ── Триггеры спавна зомби ─────────────────────────────────────────────────

public class TriggerPreset
{
    public int TRIGGER_ID { get; set; }
    public string POSITION { get; set; } = "0 0 0";
    public double RADIUS { get; set; } = 15.0;
    public string NOTIFICATION_TEXT { get; set; } = "";
    public int TIME_SHOW_NOTIFICATION { get; set; } = 5;
    public int DELAY_SPAWN { get; set; }
    public List<string> AI_SPAWN_POSITION { get; set; } = new();
    public double AI_SPAWN_RADIUS { get; set; } = 8.0;
    public AiSettings AI_SETTINGS { get; set; } = new();
}

public class AiSettings
{
    public int COUNT_AI { get; set; }
    public List<string> CLASSNAME { get; set; } = new();
    public List<string> CREATE_ITEMS_IN_INVENTORY { get; set; } = new();
}

// ── Маппинг ───────────────────────────────────────────────────────────────

public class MappingGroup
{
    public int GROUP_ID { get; set; }
    public string GROUP_NAME { get; set; } = "";
    public List<MappingItem> MAPPING_ITEMS { get; set; } = new();
}

public class MappingItem
{
    public string OBJECT { get; set; } = "";
    public string POSITION { get; set; } = "0 0 0";
    public string ORIENTATION { get; set; } = "0 0 0";
}

// ── Звуковые триггеры ────────────────────────────────────────────────────

public class SoundTrigger
{
    public int TRIGGER_ID { get; set; }
    public List<SoundTriggerSettings> TRIGGER_SETTINGS { get; set; } = new();
}

public class SoundTriggerSettings
{
    public string TRIGGER_POSITION { get; set; } = "0 0 0";
    public List<string> SOUND_SET { get; set; } = new();
}

// ─── Файл прогресса игрока (SteamID.json) ───────────────────────────────────

public class PlayerQuestData
{
    public List<QuestInProgress> QUEST_IN_PROGRESS { get; set; } = new();
    public List<QuestFinished> QUEST_IS_FINISH { get; set; } = new();
    public List<QuestFinished> QUEST_FAILED { get; set; } = new();
    public double REPUTATION { get; set; }
}

public class QuestInProgress
{
    public int ID { get; set; }
    public long SYSTEM_ID { get; set; }
    public int IS_FINISH { get; set; }
    public List<double> PROGRESS_TARGETS { get; set; } = new();
}

public class QuestFinished
{
    public int ID { get; set; }
    public long SYSTEM_ID { get; set; }
}
