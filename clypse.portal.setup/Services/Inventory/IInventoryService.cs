using clypse.portal.setup.Enums;

namespace clypse.portal.setup.Services.Inventory;

public interface IInventoryService
{
    public void RecordResource(InventoryItem item);

    public IEnumerable<InventoryItem> GetResourcesByType(ResourceType resourceType);

    public void Save(string path);

    public void Load(string path);
}
