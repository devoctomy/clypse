using clypse.core.Secrets;

namespace clypse.core.Vault;

public class Vault : IVault
{
    public VaultInfo Info { get; set; }
    public VaultIndex Index { get; set; }
    public IReadOnlyList<Secret> PendingSecrets => _pendingSecrets;
    public bool IsDirty => _isDirty;

    private readonly List<Secret> _pendingSecrets;
    private bool _isDirty = true;

    public Vault(
        VaultInfo info,
        VaultIndex index)
    {
        Info = info;
        Index = index;
        _pendingSecrets = [];
    }

    public void AddSecret(Secret secret)
    {
        _pendingSecrets.Add(secret);
        _isDirty = true;
    }

    public bool UpdateSecret(Secret secret)
    {
        var existing = Index.Entries.SingleOrDefault(x => x.Id == secret.Id);
        if (existing != null)
        {
            _pendingSecrets.Add(secret);
            _isDirty = true;
            return true;
        }

        return false;
    }

    public void MakeClean()
    {
        _pendingSecrets.Clear();
        _isDirty = false;
    }
}
