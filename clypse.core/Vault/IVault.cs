using clypse.core.Secrets;

namespace clypse.core.Vault;

public interface IVault
{
    public VaultInfo Info { get; set; }
    public VaultIndex Index { get; set; }
    public IReadOnlyList<Secret> PendingSecrets { get; }
    public bool IsDirty { get; }

    public void AddSecret(Secret secret);
    public bool UpdateSecret(Secret secret);
    public void MakeClean();
}
