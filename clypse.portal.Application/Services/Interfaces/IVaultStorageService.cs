using clypse.portal.Models.Vault;

namespace clypse.portal.Application.Services.Interfaces;

/// <summary>
/// Provides storage and retrieval functionality for vault metadata.
/// </summary>
public interface IVaultStorageService
{
    /// <summary>
    /// Retrieves all stored vault metadata.
    /// </summary>
    /// <returns>A list of all vault metadata objects.</returns>
    Task<List<VaultMetadata>> GetVaultsAsync();

    /// <summary>
    /// Saves a collection of vault metadata to storage.
    /// </summary>
    /// <param name="vaults">The list of vault metadata to save.</param>
    /// <returns>Nothing.</returns>
    Task SaveVaultsAsync(List<VaultMetadata> vaults);

    /// <summary>
    /// Updates an existing vault metadata or adds it if it doesn't exist.
    /// </summary>
    /// <param name="vault">The vault metadata to update or add.</param>
    /// <returns>Nothing.</returns>
    Task UpdateVaultAsync(VaultMetadata vault);

    /// <summary>
    /// Removes a vault metadata from storage by its ID.
    /// </summary>
    /// <param name="vaultId">The unique identifier of the vault to remove.</param>
    /// <returns>Nothing.</returns>
    Task RemoveVaultAsync(string vaultId);

    /// <summary>
    /// Clears all vault metadata from storage.
    /// </summary>
    /// <returns>Nothing.</returns>
    Task ClearVaultsAsync();
}
