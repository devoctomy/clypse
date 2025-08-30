using Amazon.S3.Model;
using clypse.core.Secrets;

namespace clypse.core.Vault;

/// <summary>
/// Defines the contract for vault management operations including creation, storage, and retrieval of vaults and secrets.
/// </summary>
public interface IVaultManager
{
    /// <summary>
    /// Creates a new vault with the specified name and description.
    /// </summary>
    /// <param name="name">The name of the vault.</param>
    /// <param name="description">The description of the vault.</param>
    /// <returns>A new vault instance.</returns>
    public IVault Create(
        string name,
        string description);

    /// <summary>
    /// Fetches a list of all vault Ids available in storage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of vault ids found in storage.</returns>
    public Task<List<string>> ListVaultIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves the vault and all pending changes to encrypted cloud storage.
    /// </summary>
    /// <param name="vault">The vault to save.</param>
    /// <param name="base64Key">The base64-encoded encryption key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results of the save operation.</returns>
    public Task<VaultSaveResults> SaveAsync(
        IVault vault,
        string base64Key,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the entire vault and all its secrets from encrypted cloud storage.
    /// </summary>
    /// <param name="vault">The vault to delete.</param>
    /// <param name="base64Key">The base64-encoded encryption key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteAsync(
        IVault vault,
        string base64Key,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads a vault from encrypted cloud storage using the specified ID and encryption key.
    /// </summary>
    /// <param name="id">The unique identifier of the vault to load.</param>
    /// <param name="base64Key">The base64-encoded encryption key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded vault instance.</returns>
    public Task<Vault> LoadAsync(
        string id,
        string base64Key,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a specific secret from the vault by its ID.
    /// </summary>
    /// <param name="vault">The vault containing the secret.</param>
    /// <param name="secretId">The unique identifier of the secret to retrieve.</param>
    /// <param name="base64Key">The base64-encoded encryption key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret if found; otherwise, null.</returns>
    public Task<Secret?> GetSecretAsync(
            IVault vault,
            string secretId,
            string base64Key,
            CancellationToken cancellationToken);

    /// <summary>
    /// Verifies the integrity of the vault by checking consistency between the index and stored secrets.
    /// </summary>
    /// <param name="vault">The vault to verify.</param>
    /// <param name="base64Key">The base64-encoded encryption key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results of the verification operation.</returns>
    public Task<VaultVerifyResults> VerifyAsync(
        IVault vault,
        string base64Key,
        CancellationToken cancellationToken);
}
