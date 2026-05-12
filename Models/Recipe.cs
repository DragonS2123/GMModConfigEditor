using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace GMCraftTableEditor.Models;

public class Recipe
{
    public string RECIPE_NAME { get; set; } = "Новый рецепт";
    public string DESCRIPTION { get; set; } = "";
    public string CATEGORY { get; set; } = "Крафт";
    public string RESULT { get; set; } = "";
    public int WEAPON_REPAIR { get; set; }
    public int OTHER_REPAIR { get; set; }
    public int DISMANTLE { get; set; }
    public ObservableCollection<string> NEEDS_KIT_ITEMS { get; set; } = new();
    public int NEED_COUNT_KIT { get; set; }
    public string PLAN { get; set; } = "";
    public double DAMAGE_TO_PLAN { get; set; } = 2.0;
    public int RESULT_COUNT { get; set; } = 1;
    public double RESULT_HEALTH { get; set; } = 1.0;
    public int RESULT_LIQUID { get; set; }
    public int LIQUID_TYPE { get; set; }
    public double TIME_TO_CREATE { get; set; } = 30.0;
    public int REMOTE_CRAFT { get; set; } = 1;
    public int NEED_ELECTRICITY { get; set; }
    public ObservableCollection<string> TOOLS { get; set; } = new();
    public double MIN_TOOLS_DAMAGE { get; set; } = 5.0;
    public double MAX_TOOLS_DAMAGE { get; set; } = 10.0;
    public double MIN_ATTACHMENT_DAMAGE { get; set; } = 10.0;
    public double MAX_ATTACHMENT_DAMAGE { get; set; } = 50.0;
    public double MIN_CONSUMPTION { get; set; } = 20.0;
    public double MAX_CONSUMPTION { get; set; } = 25.0;
    public string ABILITY { get; set; } = "";
    public int REQUIRED_LEVEL { get; set; }
    public double GIVE_POINTS { get; set; }
    public int NEED_SPECIAL_ITEMS { get; set; }
    public ObservableCollection<string> NEEDS_SPECIAL_ITEMS { get; set; } = new();
    public ObservableCollection<Ingredient> INGRIDIENTS { get; set; } = new();

    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(RECIPE_NAME) ? RESULT : RECIPE_NAME;
}

public class Ingredient
{
    public string CLASSNAME { get; set; } = "";
    public int ITEM_AMOUNT { get; set; } = 1;
    public int USE_ENERGY { get; set; }
    public int NEED_LIQUID { get; set; }
    public int LIQUID_TYPE { get; set; }
    public int DESTROY_ITEM { get; set; } = 1;
    public int CHANGE_HEALTH_ITEM_BY_CRAFT { get; set; }
}
