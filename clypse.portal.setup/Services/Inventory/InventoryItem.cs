using clypse.portal.setup.Enums;

namespace clypse.portal.setup.Services.Inventory;

/// <summary>
/// Represents a provisioned AWS resource tracked during setup.
/// </summary>
public class InventoryItem
{
    /// <summary>
    /// Description of the resource.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type of the resource.
    /// </summary>
    public ResourceType ResourceType { get; set; }

    /// <summary>
    /// Identifier of the resource (ARN, name, etc.).
    /// </summary>
    public string ResourceId { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata associated with the resource.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
