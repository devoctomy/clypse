using clypse.portal.Models;

namespace clypse.portal.Services;

public interface IVaultStorageService
{
    Task<List<VaultMetadata>> GetVaultsAsync();
    Task SaveVaultsAsync(List<VaultMetadata> vaults);
    Task UpdateVaultAsync(VaultMetadata vault);
    Task RemoveVaultAsync(string vaultId);
    Task ClearVaultsAsync();
}
