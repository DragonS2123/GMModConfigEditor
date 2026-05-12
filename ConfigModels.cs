using System.Collections.Generic;

namespace GMCraftTableEditor.Models;

// ─── GM_ACCESS_CONFIG ───────────────────────────────────────────────────────

public class GMAccessConfig
{
    public string TITLE { get; set; } = "";
    public string AUTHOR { get; set; } = "";
    public string DISCORD { get; set; } = "";
    public string CONFIG_VERSION { get; set; } = "";
    public string GENERAL_SETTINGS { get; set; } = "";
    public List<string> ADMIN_IDS { get; set; } = new();
    public List<CraftTableAccess> CRAFT_TABLES { get; set; } = new();
}

public class CraftTableAccess
{
    public int TABLE_ID { get; set; }
    public int USE_WHITELIST { get; set; }
    public List<int> ARRAY_CATEGORY { get; set; } = new();
    public List<string> ALLOWED_PLAYERS { get; set; } = new();
    public int STATUS { get; set; }
}

// ─── GM_CRAFTTABLE_CONFIG ────────────────────────────────────────────────────

public class GMCraftTableConfig
{
    public string TITLE { get; set; } = "";
    public string AUTHOR { get; set; } = "";
    public string DISCORD { get; set; } = "";
    public string CONFIG_VERSION { get; set; } = "";
    public string GENERAL_SETTINGS { get; set; } = "";
    public int CAN_BE_REPAIR_TO_PRISTINE { get; set; }
    public List<CategoryItem> CATEGORY_LIST { get; set; } = new();
    public List<object> RECIPE_LIST { get; set; } = new();
}

public class CategoryItem
{
    public int TYPE { get; set; }
    public string NAME { get; set; } = "";
    public string RECIPE_FILENAME { get; set; } = "";
    public string ICON_PATH { get; set; } = "";
    public int PRIVATE { get; set; }
}
