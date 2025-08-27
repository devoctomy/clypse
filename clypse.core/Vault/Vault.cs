using clypse.core.Secrets;

namespace clypse.core.Vault;

public class Vault : IVault
{
    private readonly List<Secret> pendingSecrets;
    private readonly List<string> secretsToDelete;
    private bool isDirty = true;

    public VaultInfo Info { get; set; }

    public VaultIndex Index { get; set; }

    public IReadOnlyList<Secret> PendingSecrets => pendingSecrets;

    public IReadOnlyList<string> SecretsToDelete => secretsToDelete;

    public bool IsDirty => isDirty;

    public Vault(
        VaultInfo info,
        VaultIndex index)
    {
        Info = info;
        Index = index;
        pendingSecrets = [];
        secretsToDelete = [];
    }

    public bool AddSecret(Secret secret)
    {
        var indexEntry = Index.Entries.SingleOrDefault(x => x.Id == secret.Id);
        if (indexEntry == null)
        {
            pendingSecrets.Add(secret);
            isDirty = true;
            return true;
        }

        return false;
    }

    public bool DeleteSecret(string secretId)
    {
        var indexEntry = Index.Entries.SingleOrDefault(x => x.Id == secretId);
        if (indexEntry != null)
        {
            secretsToDelete.Add(secretId);
            isDirty = true;
            return true;
        }

        return false;
    }

    public bool UpdateSecret(Secret secret)
    {
        var existing = Index.Entries.SingleOrDefault(x => x.Id == secret.Id);
        if (existing != null)
        {
            pendingSecrets.Add(secret);
            isDirty = true;
            return true;
        }

        return false;
    }

    public void MakeClean()
    {
        pendingSecrets.Clear();
        secretsToDelete.Clear();
        isDirty = false;
    }
}
