namespace GMCraftTableEditor.Models;

public class ItemDatabaseEntry
{
    public string ClassName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = "";
    public string SourceMod { get; set; } = "";
    public string Comment { get; set; } = "";
    public bool Favorite { get; set; }
}