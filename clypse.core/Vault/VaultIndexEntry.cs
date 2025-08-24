namespace clypse.core.Vault;

public class VaultIndexEntry
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }

    public VaultIndexEntry(
        string id,
        string name,
        string? description,
        string? tags)
    {
        Id = id;
        Name = name;
        Description = description;
        Tags = tags;
    }
}
