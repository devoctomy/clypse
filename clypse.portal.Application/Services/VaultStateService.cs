using clypse.core.Vault;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.Vault;

namespace clypse.portal.Application.Services;

/// <inheritdoc/>
public class VaultStateService : IVaultStateService
{
    /// <inheritdoc/>
    public event EventHandler? VaultStateChanged;

    /// <inheritdoc/>
    public VaultMetadata? CurrentVault { get; private set; }

    /// <inheritdoc/>
    public string? CurrentVaultKey { get; private set; }

    /// <inheritdoc/>
    public IVault? LoadedVault { get; private set; }

    /// <inheritdoc/>
    public IVaultManager? VaultManager { get; private set; }

    /// <inheritdoc/>
    public void SetVaultState(VaultMetadata vault, string key, IVault loadedVault, IVaultManager manager)
    {
        CurrentVault = vault;
        CurrentVaultKey = key;
        LoadedVault = loadedVault;
        VaultManager = manager;
        VaultStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void ClearVaultState()
    {
        VaultManager?.Dispose();
        CurrentVault = null;
        CurrentVaultKey = null;
        LoadedVault = null;
        VaultManager = null;
        VaultStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void UpdateLoadedVault(IVault loadedVault)
    {
        LoadedVault = loadedVault;
        if (CurrentVault != null)
        {
            CurrentVault.IndexEntries = loadedVault.Index.Entries.ToList();
        }

        VaultStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
