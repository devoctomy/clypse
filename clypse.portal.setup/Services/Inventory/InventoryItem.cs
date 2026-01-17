using clypse.portal.setup.Enums;

namespace clypse.portal.setup.Services.Inventory;

public class InventoryItem
{
    public string Description { get; set; } = string.Empty;
    public ResourceType ResourceType { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
