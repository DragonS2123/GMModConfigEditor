using System.Collections.ObjectModel;

namespace GMCraftTableEditor.Models;

public class StashConfig
{
    public int DebugStash { get; set; }
    public int IgnoreChance { get; set; } = 1;
    public double BleedChance { get; set; } = 50.0;
    public double GlovesDamage { get; set; } = 10.0;
    public ObservableCollection<string> AdminList { get; set; } = new();
    public ObservableCollection<string> LocationsName { get; set; } = new();
    public ObservableCollection<SpawnTier> SpawnTierList { get; set; } = new();
    public ObservableCollection<StashSpawnPreset> StashSpawnPresetList { get; set; } = new();
}

public class SpawnTier
{
    public string SpawnItemStash { get; set; } = "";
    public string SpawnLootItemStash { get; set; } = "";
    public string SpawnChest { get; set; } = "";
    public double DeleteChestTimer { get; set; } = 30.0;
    public int RespawnLoot { get; set; } = 1;
    public double RespawnLootTimerMix { get; set; } = 60.0;
    public double RespawnLootTimerMax { get; set; } = 70.0;
    public double MinSpawnChance { get; set; } = 30.0;
    public double MaxSpawnChance { get; set; } = 70.0;
    public double ExplosionChance { get; set; }
    public double ExpljsionRadius { get; set; } = 5.0;
    public double ExplosionDamage { get; set; } = 50.0;
    public ObservableCollection<string> PresetList { get; set; } = new();
    public ObservableCollection<string> PresetListGaranted { get; set; } = new();
}

public class StashSpawnPreset
{
    public string PresetName { get; set; } = "Новый пресет";
    public int MinLootCount { get; set; }
    public int MaxLootCount { get; set; }
    public ObservableCollection<StashPresetCfg> StashPresetCfgList { get; set; } = new();
}

public class StashPresetCfg
{
    public ObservableCollection<string> ItemName { get; set; } = new();
    public double SpawnChance { get; set; } = 100.0;
    public double ItemQuantity { get; set; } = -1.0;
    public double ItemHealth { get; set; } = -1.0;
    public ObservableCollection<StashAttachItem> StashAttachesItemList { get; set; } = new();
}

public class StashAttachItem
{
    public ObservableCollection<string> ItemList { get; set; } = new();
    public double SpawnChance { get; set; } = 100.0;
    public double ItemQuantity { get; set; } = -1.0;
    public double ItemHealth { get; set; } = -1.0;
}
