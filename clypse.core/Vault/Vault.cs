namespace clypse.core.Vault;

public class Vault : IVault
{
    public VaultInfo Info { get; set; }
    public VaultIndex Index { get; set; }

    public Vault(
        VaultInfo info,
        VaultIndex index)
    {
        Info = info;
        Index = index;
    }
}
