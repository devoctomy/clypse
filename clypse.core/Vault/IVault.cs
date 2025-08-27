using clypse.core.Secrets;

namespace clypse.core.Vault;

public interface IVault
{
    public VaultInfo Info { get; set; }

    public VaultIndex Index { get; set; }

    public IReadOnlyList<Secret> PendingSecrets { get; }

    public IReadOnlyList<string> SecretsToDelete { get; }

    public bool IsDirty { get; }

    public bool AddSecret(Secret secret);

    public bool DeleteSecret(string secretId);

    public bool UpdateSecret(Secret secret);

    public void MakeClean();
}
