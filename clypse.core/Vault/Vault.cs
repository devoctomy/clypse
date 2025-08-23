using clypse.core.Secrets;

namespace clypse.core.Vault;

public class Vault : IVault
{
    public VaultInfo Info { get; set; }
    public VaultIndex Index { get; set; }
    public IReadOnlyList<Secret> PendingSecrets => _pendingSecrets;

    private readonly List<Secret> _pendingSecrets;

    public Vault(
        VaultInfo info,
        VaultIndex index)
    {
        Info = info;
        Index = index;
        _pendingSecrets = [];
    }
}
