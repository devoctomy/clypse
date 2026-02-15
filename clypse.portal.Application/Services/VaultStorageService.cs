using System.Text.Json;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.Vault;
using Microsoft.JSInterop;

namespace clypse.portal.Application.Services;

/// <inheritdoc/>
public class VaultStorageService(IJSRuntime jsRuntime)
    : IVaultStorageService
{
    private const string VaultsLocalStorageKey = "clypse_vaults";
    private static readonly JsonSerializerOptions JsonSerializerOptions = new ()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IJSRuntime jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    /// <inheritdoc/>
    public async Task<List<VaultMetadata>> GetVaultsAsync()
    {
        try
        {
            var vaultsJson = await this.jsRuntime.InvokeAsync<string>("localStorage.getItem", VaultsLocalStorageKey);

            if (string.IsNullOrEmpty(vaultsJson))
            {
                return [];
            }

            var vaultStorage = JsonSerializer.Deserialize<VaultStorage>(vaultsJson);
            return vaultStorage?.Vaults ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading vaults from storage: {ex.Message}");
            return new List<VaultMetadata>();
        }
    }

    /// <inheritdoc/>
    public async Task SaveVaultsAsync(List<VaultMetadata> vaults)
    {
        try
        {
            var vaultStorage = new VaultStorage { Vaults = vaults };
            var vaultsJson = JsonSerializer.Serialize(
                vaultStorage,
                JsonSerializerOptions);

            await this.jsRuntime.InvokeVoidAsync("localStorage.setItem", VaultsLocalStorageKey, vaultsJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving vaults to storage: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task UpdateVaultAsync(VaultMetadata vault)
    {
        try
        {
            var vaults = await this.GetVaultsAsync();
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

            await this.SaveVaultsAsync(vaults);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating vault in storage: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task RemoveVaultAsync(string vaultId)
    {
        try
        {
            var vaults = await this.GetVaultsAsync();
            var vaultToRemove = vaults.FirstOrDefault(v => v.Id == vaultId);

            if (vaultToRemove != null)
            {
                vaults.Remove(vaultToRemove);
                await this.SaveVaultsAsync(vaults);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing vault from storage: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task ClearVaultsAsync()
    {
        try
        {
            await this.jsRuntime.InvokeVoidAsync("localStorage.removeItem", VaultsLocalStorageKey);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing vaults from storage: {ex.Message}");
        }
    }
}
