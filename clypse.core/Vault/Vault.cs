using clypse.core.Secrets;

namespace clypse.core.Vault;

public class Vault : IVault
{
    public VaultInfo Info { get; set; }
    public VaultIndex Index { get; set; }
    public IReadOnlyList<Secret> PendingSecrets => _pendingSecrets;
    public IReadOnlyList<string> SecretsToDelete => _secretsToDelete;
    public bool IsDirty => _isDirty;

    private readonly List<Secret> _pendingSecrets;
    private readonly List<string> _secretsToDelete;
    private bool _isDirty = true;

    public Vault(
        VaultInfo info,
        VaultIndex index)
    {
        Info = info;
        Index = index;
        _pendingSecrets = [];
        _secretsToDelete = [];
    }

    public bool AddSecret(Secret secret)
    {
        var indexEntry = Index.Entries.SingleOrDefault(x => x.Id == secret.Id);
        if (indexEntry == null)
        {
            _pendingSecrets.Add(secret);
            _isDirty = true;
            return true;
        }

        return false;
    }

    public bool DeleteSecret(string secretId)
    {
        var indexEntry = Index.Entries.SingleOrDefault(x => x.Id == secretId);
        if(indexEntry != null)
        {
            _secretsToDelete.Add(secretId);
            _isDirty = true;
            return true;
        }

        return false;
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
        _secretsToDelete.Clear();
        _isDirty = false;
    }
}
