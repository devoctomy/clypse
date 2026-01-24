using System.Text.Json;
using clypse.portal.setup.Enums;
using clypse.portal.setup.Services.IO;

namespace clypse.portal.setup.Services.Inventory;

/// <inheritdoc cref="IInventoryService" />
public class InventoryService : IInventoryService
{
    private readonly IIoService _ioService;
    private readonly List<InventoryItem> _inventory = new();

    public InventoryService(IIoService ioService)
    {
        _ioService = ioService;
    }

    /// <inheritdoc />
    public IEnumerable<InventoryItem> GetResourcesByType(ResourceType resourceType)
    {
        return _inventory.Where(item => item.ResourceType == resourceType);
    }

    /// <inheritdoc />
    public void RecordResource(InventoryItem item)
    {
        _inventory.Add(item);
    }

    /// <inheritdoc />
    public void Save(string path)
    {
        var directory = _ioService.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !_ioService.DirectoryExists(directory))
        {
            _ioService.CreateDirectory(directory);
        }

        var serialized = JsonSerializer.Serialize(_inventory);
        _ioService.WriteAllText(path, serialized);
    }

    /// <inheritdoc />
    public void Load(string path)
    {
        _inventory.Clear();

        if (string.IsNullOrWhiteSpace(path) || !_ioService.FileExists(path))
        {
            return;
        }

        var serializedInventory = _ioService.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(serializedInventory))
        {
            return;
        }

        var items = JsonSerializer.Deserialize<List<InventoryItem>>(serializedInventory) ?? new List<InventoryItem>();
        _inventory.AddRange(items);
    }
}
