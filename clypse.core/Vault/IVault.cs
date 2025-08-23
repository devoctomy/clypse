using clypse.core.Secrets;

namespace clypse.core.Vault;

public interface IVault
{
    public VaultInfo Info { get; set; }
    public VaultIndex Index { get; set; }
    public IReadOnlyList<Secret> PendingSecrets { get; }
}
