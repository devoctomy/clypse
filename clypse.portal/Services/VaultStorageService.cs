using Microsoft.JSInterop;
using System.Text.Json;
using clypse.portal.Models;

namespace clypse.portal.Services;

public interface IVaultStorageService
{
    Task<List<VaultMetadata>> GetVaultsAsync();
    Task SaveVaultsAsync(List<VaultMetadata> vaults);
    Task UpdateVaultAsync(VaultMetadata vault);
    Task ClearVaultsAsync();
}

public class VaultStorageService : IVaultStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "clypse_vaults";

    public VaultStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<List<VaultMetadata>> GetVaultsAsync()
    {
        try
        {
            var vaultsJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
            
            if (string.IsNullOrEmpty(vaultsJson))
            {
                return new List<VaultMetadata>();
            }

            var vaultStorage = JsonSerializer.Deserialize<VaultStorage>(vaultsJson);
            return vaultStorage?.Vaults ?? new List<VaultMetadata>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading vaults from storage: {ex.Message}");
            return new List<VaultMetadata>();
        }
    }

    public async Task SaveVaultsAsync(List<VaultMetadata> vaults)
    {
        try
        {
            var vaultStorage = new VaultStorage { Vaults = vaults };
            var vaultsJson = JsonSerializer.Serialize(vaultStorage, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, vaultsJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving vaults to storage: {ex.Message}");
        }
    }

    public async Task UpdateVaultAsync(VaultMetadata vault)
    {
        try
        {
            var vaults = await GetVaultsAsync();
            var existingVault = vaults.FirstOrDefault(v => v.Id == vault.Id);
            
            if (existingVault != null)
            {
                existingVault.Name = vault.Name;
                existingVault.Description = vault.Description;
            }
            else
            {
                vaults.Add(vault);
            }
            
            await SaveVaultsAsync(vaults);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating vault in storage: {ex.Message}");
        }
    }

    public async Task ClearVaultsAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing vaults from storage: {ex.Message}");
        }
    }
}
