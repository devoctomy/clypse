using clypse.core.Vault;
using clypse.portal.Models.Vault;

namespace clypse.portal.Application.Services.Interfaces;

/// <summary>
/// Manages the currently active vault state shared between the home page and the credentials component.
/// </summary>
public interface IVaultStateService
{
    /// <summary>
    /// Gets the current vault metadata.
    /// </summary>
    VaultMetadata? CurrentVault { get; }

    /// <summary>
    /// Gets the current vault encryption key.
    /// </summary>
    string? CurrentVaultKey { get; }

    /// <summary>
    /// Gets the currently loaded vault instance.
    /// </summary>
    IVault? LoadedVault { get; }

    /// <summary>
    /// Gets the vault manager for the current vault.
    /// </summary>
    IVaultManager? VaultManager { get; }

    /// <summary>
    /// Occurs when the vault state changes.
    /// </summary>
    event EventHandler? VaultStateChanged;

    /// <summary>
    /// Sets the active vault state after a vault has been unlocked.
    /// </summary>
    /// <param name="vault">The vault metadata.</param>
    /// <param name="key">The vault encryption key.</param>
    /// <param name="loadedVault">The loaded vault instance.</param>
    /// <param name="manager">The vault manager.</param>
    void SetVaultState(VaultMetadata vault, string key, IVault loadedVault, IVaultManager manager);

    /// <summary>
    /// Clears the active vault state (e.g., after locking).
    /// </summary>
    void ClearVaultState();

    /// <summary>
    /// Updates the loaded vault instance (e.g., after a vault operation).
    /// </summary>
    /// <param name="loadedVault">The updated vault instance.</param>
    void UpdateLoadedVault(IVault loadedVault);
}
