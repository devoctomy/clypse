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
        this.Id = id;
        this.Name = name;
        this.Description = description;
        this.Tags = tags;
    }
}
