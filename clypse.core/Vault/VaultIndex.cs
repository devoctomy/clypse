namespace clypse.core.Vault
{
    public class VaultIndex
    {
        List<VaultIndexEntry> Entries { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        public VaultIndex()
        {
            Entries = [];
            CreatedAt = DateTime.UtcNow;
            LastUpdatedAt = DateTime.UtcNow;
        }
    }
}
