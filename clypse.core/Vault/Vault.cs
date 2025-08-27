using clypse.core.Secrets;

namespace clypse.core.Vault;

public class Vault : IVault
{
    private readonly List<Secret> pendingSecrets;
    private readonly List<string> secretsToDelete;
    private bool isDirty = true;

    public VaultInfo Info { get; set; }

    public VaultIndex Index { get; set; }

    public IReadOnlyList<Secret> PendingSecrets => this.pendingSecrets;

    public IReadOnlyList<string> SecretsToDelete => this.secretsToDelete;

    public bool IsDirty => this.isDirty;

    public Vault(
        VaultInfo info,
        VaultIndex index)
    {
        this.Info = info;
        this.Index = index;
        this.pendingSecrets = [];
        this.secretsToDelete = [];
    }

    public bool AddSecret(Secret secret)
    {
        var indexEntry = this.Index.Entries.SingleOrDefault(x => x.Id == secret.Id);
        if (indexEntry == null)
        {
            this.pendingSecrets.Add(secret);
            this.isDirty = true;
            return true;
        }

        return false;
    }

    public bool DeleteSecret(string secretId)
    {
        var indexEntry = this.Index.Entries.SingleOrDefault(x => x.Id == secretId);
        if (indexEntry != null)
        {
            this.secretsToDelete.Add(secretId);
            this.isDirty = true;
            return true;
        }

        return false;
    }

    public bool UpdateSecret(Secret secret)
    {
        var existing = this.Index.Entries.SingleOrDefault(x => x.Id == secret.Id);
        if (existing != null)
        {
            this.pendingSecrets.Add(secret);
            this.isDirty = true;
            return true;
        }

        return false;
    }

    public void MakeClean()
    {
        this.pendingSecrets.Clear();
        this.secretsToDelete.Clear();
        this.isDirty = false;
    }
}
