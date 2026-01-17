using clypse.portal.setup.Enums;

namespace clypse.portal.setup.Services.Inventory;

/// <summary>
/// Tracks AWS resources created during setup.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Adds a resource record to the inventory.
    /// </summary>
    /// <param name="item">The resource to record.</param>
    public void RecordResource(InventoryItem item);

    /// <summary>
    /// Retrieves recorded resources filtered by type.
    /// </summary>
    /// <param name="resourceType">The resource type to filter by.</param>
    /// <returns>An enumerable of matching resources.</returns>
    public IEnumerable<InventoryItem> GetResourcesByType(ResourceType resourceType);

    /// <summary>
    /// Persists the inventory to the specified path.
    /// </summary>
    /// <param name="path">File path to save to.</param>
    public void Save(string path);

    /// <summary>
    /// Loads inventory data from the specified path.
    /// </summary>
    /// <param name="path">File path to read from.</param>
    public void Load(string path);
}
