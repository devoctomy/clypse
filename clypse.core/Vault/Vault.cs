using clypse.core.Enums;
using clypse.core.Secrets;

namespace clypse.core.Vault;

/// <summary>
/// Represents a vault for storing and managing secrets with support for adding, updating, and deleting secrets.
/// </summary>
public class Vault : IVault
{
    private readonly List<Secret> pendingSecrets;
    private readonly List<string> secretsToDelete;
    private bool isDirty = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="Vault"/> class with the specified vault information and index.
    /// </summary>
    /// <param name="manifest">The vault manifest.</param>
    /// <param name="info">The vault information.</param>
    /// <param name="index">The vault index containing metadata about stored secrets.</param>
    public Vault(
        VaultManifest manifest,
        VaultInfo info,
        VaultIndex index)
    {
        this.Manifest = manifest;
        this.Info = info;
        this.Index = index;
        this.pendingSecrets = [];
        this.secretsToDelete = [];
    }

    /// <summary>
    /// Gets or sets the vault manifest.
    /// </summary>
    public VaultManifest Manifest { get; set; }

    /// <summary>
    /// Gets or sets the vault information.
    /// </summary>
    public VaultInfo Info { get; set; }

    /// <summary>
    /// Gets or sets the vault index containing metadata about stored secrets.
    /// </summary>
    public VaultIndex Index { get; set; }

    /// <summary>
    /// Gets the list of secrets that are pending to be saved to storage.
    /// </summary>
    public IReadOnlyList<Secret> PendingSecrets => this.pendingSecrets;

    /// <summary>
    /// Gets the list of secret IDs that are pending deletion from storage.
    /// </summary>
    public IReadOnlyList<string> SecretsToDelete => this.secretsToDelete;

    /// <summary>
    /// Gets a value indicating whether the vault has unsaved changes.
    /// </summary>
    public bool IsDirty => this.isDirty;

    /// <summary>
    /// Adds a new secret to the vault if it doesn't already exist.
    /// </summary>
    /// <param name="secret">The secret to add.</param>
    /// <returns>True if the secret was added; false if it already exists.</returns>
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

    /// <summary>
    /// Adds multiple raw secrets to the vault.
    /// </summary>
    /// <param name="rawSecrets">A list of dictionaries representing raw secrets to add.</param>
    /// <param name="defaultSecretType">The default secret type to assign if not specified in the raw data.</param>
    /// <returns>True if all secrets were successfully added; false if any failed.</returns>
    public bool AddRawSecrets(
        IList<Dictionary<string, string>> rawSecrets,
        SecretType defaultSecretType)
    {
        var addedSecrets = new List<Secret>();
        var allAdded = true;
        var isDirtyNew = this.isDirty;

        foreach (var rawSecret in rawSecrets)
        {
            try
            {
                var secret = Secret.FromDictionary(rawSecret);
                if (secret.SecretType == SecretType.None)
                {
                    secret.SecretType = defaultSecretType;
                }

                var added = this.AddSecret(secret);
                if (!added)
                {
                    allAdded = false;
                    break;
                }

                addedSecrets.Add(secret);
            }
            catch
            {
                allAdded = false;
            }
        }

        if (!allAdded)
        {
            foreach (var secret in addedSecrets)
            {
                this.pendingSecrets.Remove(secret);
            }

            this.isDirty = isDirtyNew;
        }

        return allAdded;
    }

    /// <summary>
    /// Marks a secret for deletion from the vault.
    /// </summary>
    /// <param name="secretId">The ID of the secret to delete.</param>
    /// <returns>True if the secret was marked for deletion; false if it doesn't exist.</returns>
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

    /// <summary>
    /// Updates an existing secret in the vault.
    /// </summary>
    /// <param name="secret">The secret with updated information.</param>
    /// <returns>True if the secret was marked for update; false if it doesn't exist.</returns>
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

    /// <summary>
    /// Clears all pending changes and marks the vault as clean (no unsaved changes).
    /// </summary>
    public void MakeClean()
    {
        this.pendingSecrets.Clear();
        this.secretsToDelete.Clear();
        this.isDirty = false;
    }
}
