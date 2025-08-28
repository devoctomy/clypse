using clypse.core.Secrets;

namespace clypse.core.Vault;

/// <summary>
/// Defines the contract for secret vaults that store and manage secrets with support for tracking changes and pending operations.
/// </summary>
public interface IVault
{
    /// <summary>
    /// Gets or sets the vault information containing metadata about the vault.
    /// </summary>
    public VaultInfo Info { get; set; }

    /// <summary>
    /// Gets or sets the vault index containing metadata about all secrets stored in the vault.
    /// </summary>
    public VaultIndex Index { get; set; }

    /// <summary>
    /// Gets the read-only list of secrets that are pending to be saved to storage.
    /// </summary>
    public IReadOnlyList<Secret> PendingSecrets { get; }

    /// <summary>
    /// Gets the read-only list of secret IDs that are pending deletion from storage.
    /// </summary>
    public IReadOnlyList<string> SecretsToDelete { get; }

    /// <summary>
    /// Gets a value indicating whether the vault has unsaved changes.
    /// </summary>
    public bool IsDirty { get; }

    /// <summary>
    /// Adds a new secret to the vault if it doesn't already exist.
    /// </summary>
    /// <param name="secret">The secret to add to the vault.</param>
    /// <returns>True if the secret was successfully added; false if it already exists.</returns>
    public bool AddSecret(Secret secret);

    /// <summary>
    /// Marks a secret for deletion from the vault.
    /// </summary>
    /// <param name="secretId">The unique identifier of the secret to delete.</param>
    /// <returns>True if the secret was marked for deletion; false if it doesn't exist.</returns>
    public bool DeleteSecret(string secretId);

    /// <summary>
    /// Updates an existing secret in the vault.
    /// </summary>
    /// <param name="secret">The secret with updated information.</param>
    /// <returns>True if the secret was marked for update; false if it doesn't exist.</returns>
    public bool UpdateSecret(Secret secret);

    /// <summary>
    /// Clears all pending changes and marks the vault as clean (no unsaved changes).
    /// </summary>
    public void MakeClean();
}
