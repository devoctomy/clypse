using clypse.portal.Models.Vault;

namespace clypse.portal.Application.Services.Interfaces;

public interface IVaultStorageService
{
    Task<List<VaultMetadata>> GetVaultsAsync();
    Task SaveVaultsAsync(List<VaultMetadata> vaults);
    Task UpdateVaultAsync(VaultMetadata vault);
    Task RemoveVaultAsync(string vaultId);
    Task ClearVaultsAsync();
}
