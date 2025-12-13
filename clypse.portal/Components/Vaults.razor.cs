using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Security;
using clypse.portal.Models;
using clypse.portal.Services;
using clypse.core.Cryptography;
using clypse.core.Cryptography.Interfaces;
using clypse.core.Vault;
using clypse.core.Cloud.Aws.S3;

namespace clypse.portal.Components;

public partial class Vaults : ComponentBase
{
    [Inject] public IVaultStorageService VaultStorage { get; set; } = default!;
    [Inject] public IVaultManagerFactoryService VaultManagerFactory { get; set; } = default!;
    [Inject] public IVaultManagerBootstrapperFactoryService VaultManagerBootstrapperFactory { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public AwsS3Config AwsS3Config { get; set; } = default!;
    [Inject] public IKeyDerivationService KeyDerivationService { get; set; } = default!;

    private List<VaultMetadata> vaults = new();
    private List<VaultListing> vaultListings = new(); // Store the original listings with manifests
    private bool isLoading = true;
    private bool showPassphrasePanel = false;
    private bool showVaultDetailsPanel = false; // New property for vault details modal
    private bool isUnlocking = false;
    private VaultMetadata? selectedVault = null;
    private VaultListing? selectedVaultListing = null; // Store selected vault listing for details
    private string? errorMessage = null;
    private IVaultManagerBootstrapperService? bootstrapperService = null;

    [Parameter] public EventCallback<(VaultMetadata vault, string key, IVaultManager manager)> OnVaultUnlocked { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadVaults();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadVaults();
        }
    }

    private async Task LoadVaults()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            // Create bootstrapper service if not already created
            if (bootstrapperService == null)
            {
                bootstrapperService = await CreateBootstrapperService();
                if (bootstrapperService == null)
                {
                    Console.WriteLine("Failed to create bootstrapper service");
                    vaults = new List<VaultMetadata>();
                    isLoading = false;
                    StateHasChanged();
                    return;
                }
            }

            // Use bootstrapper to list vaults
            var vaultListingsResult = await bootstrapperService.ListVaultsAsync(CancellationToken.None);
            vaultListings = vaultListingsResult.ToList(); // Store the original listings
            
            // Get stored vault metadata from localStorage
            var storedVaults = await VaultStorage.GetVaultsAsync();
            
            // Convert VaultListing objects to VaultMetadata objects, combining with stored metadata
            vaults = vaultListings.Select(listing => 
            {
                var storedVault = storedVaults.FirstOrDefault(v => v.Id == listing.Id);
                return new VaultMetadata
                {
                    Id = listing.Id ?? string.Empty,
                    Name = storedVault?.Name, // Use stored name if available
                    Description = storedVault?.Description // Use stored description if available
                };
            }).ToList();

#if DEBUG
            // For testing purposes, create some dummy vaults so we can test scrolling
            for (var i = 0; i < 20; i++)
            {
                vaults.Add(new VaultMetadata
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Test Vault {i + 1}",
                    Description = $"This vault cannot be opened."
                });
            }
#endif

            isLoading = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading vaults: {ex.Message}");
            vaults = new List<VaultMetadata>();
            isLoading = false;
            StateHasChanged();
        }
    }

    // This method can be called by parent components to refresh the vault list
    public async Task RefreshVaults()
    {
        await LoadVaults();
    }

    private void ShowPassphrasePanel(VaultMetadata vault)
    {
        selectedVault = vault;
        errorMessage = null;
        showPassphrasePanel = true;
        StateHasChanged();
    }

    private void HidePassphrasePanel()
    {
        showPassphrasePanel = false;
        selectedVault = null;
        errorMessage = null;
        StateHasChanged();
    }

    private void ShowVaultDetailsPanel(VaultMetadata vault)
    {
        selectedVault = vault;
        selectedVaultListing = vaultListings.FirstOrDefault(vl => vl.Id == vault.Id);
        showVaultDetailsPanel = true;
        StateHasChanged();
    }

    private void HideVaultDetailsPanel()
    {
        showVaultDetailsPanel = false;
        selectedVault = null;
        selectedVaultListing = null;
        StateHasChanged();
    }

    private async Task HandleUnlockVault(string passphrase)
    {
        if (selectedVault == null || string.IsNullOrEmpty(passphrase))
        {
            errorMessage = "Please enter a passphrase";
            StateHasChanged();
            return;
        }

        try
        {
            isUnlocking = true;
            errorMessage = null;
            StateHasChanged();

            // Bootstrapper should already exist from LoadVaults()
            if (bootstrapperService == null)
            {
                errorMessage = "Bootstrapper service not available";
                isUnlocking = false;
                StateHasChanged();
                return;
            }

            // Use bootstrapper to create a vault-specific manager for this vault
            var vaultSpecificManager = await bootstrapperService.CreateVaultManagerForVaultAsync(selectedVault.Id, CancellationToken.None);
            if (vaultSpecificManager == null)
            {
                errorMessage = "Failed to create vault-specific manager";
                isUnlocking = false;
                StateHasChanged();
                return;
            }

            var keyBytes = await vaultSpecificManager.DeriveKeyFromPassphraseAsync(
                selectedVault.Id,
                passphrase);

            var base64Key = Convert.ToBase64String(keyBytes);
            var vault = await vaultSpecificManager.LoadAsync(selectedVault.Id, base64Key, CancellationToken.None);
            selectedVault.Name = vault.Info.Name;
            selectedVault.Description = vault.Info.Description;
            await VaultStorage.UpdateVaultAsync(selectedVault);

            // Create a copy of the vault metadata with index entries before clearing selectedVault
            var unlockedVault = new VaultMetadata
            {
                Id = selectedVault.Id,
                Name = selectedVault.Name,
                Description = selectedVault.Description,
                IndexEntries = vault.Index.Entries.ToList()
            };

            HidePassphrasePanel();
            await RefreshVaults();
            
            // Notify parent that vault was unlocked with the vault-specific manager
            await OnVaultUnlocked.InvokeAsync((unlockedVault, base64Key, vaultSpecificManager));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error unlocking vault: {ex.Message}");
            errorMessage = $"Failed to unlock vault: {ex.Message}";
        }
        finally
        {
            isUnlocking = false;
            StateHasChanged();
        }
    }

    private async Task<IVaultManagerBootstrapperService?> CreateBootstrapperService()
    {
        try
        {
            Console.WriteLine("Creating bootstrapper service...");


            // Get stored credentials from localStorage
            var credentialsJson = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "clypse_credentials");
            
            if (string.IsNullOrEmpty(credentialsJson))
            {
                Console.WriteLine("No stored credentials found");
                return null;
            }

            var credentials = System.Text.Json.JsonSerializer.Deserialize<StoredCredentials>(credentialsJson);
            if (credentials?.AwsCredentials == null)
            {
                Console.WriteLine("Invalid stored credentials - AwsCredentials is null");
                return null;
            }
            
            if (string.IsNullOrEmpty(credentials.AwsCredentials.IdentityId))
            {
                Console.WriteLine("Identity ID is null or empty - cannot create bootstrapper");
                return null;
            }

            // Create JavaScript S3 invoker
            var jsInvoker = new JavaScriptS3Invoker(JSRuntime);

            Console.WriteLine($"About to create bootstrapper with Identity ID: '{credentials.AwsCredentials.IdentityId}'");

            // Create bootstrapper service using the factory
            var bootstrapper = VaultManagerBootstrapperFactory.CreateForBlazor(
                jsInvoker,
                credentials.AwsCredentials.AccessKeyId,
                credentials.AwsCredentials.SecretAccessKey,
                credentials.AwsCredentials.SessionToken,
                AwsS3Config.Region,
                AwsS3Config.BucketName,  
                credentials.AwsCredentials.IdentityId);

            Console.WriteLine("Bootstrapper service created successfully");
            return bootstrapper;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating bootstrapper service: {ex.Message}");
            return null;
        }
    }
}
