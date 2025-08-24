namespace clypse.core.Vault;

public class VaultSaveResults
{
    public bool Success { get; set; }
    public int SecretsCreated { get; set; }
    public int SecretsUpdated { get; set; }
    public int SecretsDeleted { get; set; }
}
