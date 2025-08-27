namespace clypse.core.Vault;

public class VaultIndex
{
    public List<VaultIndexEntry> Entries { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public VaultIndex()
    {
        this.Entries = [];
        this.CreatedAt = DateTime.UtcNow;
        this.LastUpdatedAt = DateTime.UtcNow;
    }
}
