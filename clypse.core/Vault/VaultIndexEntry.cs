namespace clypse.core.Vault
{
    public class VaultIndexEntry
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string? Tags { get; set; }

        public VaultIndexEntry(
            string name,
            string description)
        {
            Name = name;
            Description = description;
        }
    }
}
