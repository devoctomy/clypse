using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Security;
using clypse.portal.Models;
using clypse.portal.Services;
using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Vault;

namespace clypse.portal.Components;

public partial class Vaults : ComponentBase
{
    [Inject] public IVaultStorageService VaultStorage { get; set; } = default!;
    [Inject] public IVaultManagerFactoryService VaultManagerFactory { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public AwsS3Config AwsS3Config { get; set; } = default!;
    [Inject] public IKeyDerivationService KeyDerivationService { get; set; } = default!;

    private List<VaultMetadata> vaults = new();
    private bool isLoading = true;
    private bool showPassphrasePanel = false;
    private bool isUnlocking = false;
    private VaultMetadata? selectedVault = null;
    private string passphrase = string.Empty;
    private string? errorMessage = null;
    private IVaultManager? vaultManager = null;
    private ElementReference passphraseInput;

    [Parameter] public EventCallback<(VaultMetadata vault, string key)> OnVaultUnlocked { get; set; }

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

            vaults = await VaultStorage.GetVaultsAsync();

            isLoading = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading vaults: {ex.Message}");
            isLoading = false;
            StateHasChanged();
        }
    }

    // This method can be called by parent components to refresh the vault list
    public async Task RefreshVaults()
    {
        await LoadVaults();
    }

    private async Task ShowPassphrasePanel(VaultMetadata vault)
    {
        selectedVault = vault;
        passphrase = string.Empty;
        errorMessage = null;
        showPassphrasePanel = true;
        StateHasChanged();
        
        // Focus the password input after a small delay to ensure the modal is rendered
        await Task.Delay(100);
        await passphraseInput.FocusAsync();
    }

    private void HidePassphrasePanel()
    {
        showPassphrasePanel = false;
        selectedVault = null;
        passphrase = string.Empty;
        errorMessage = null;
        StateHasChanged();
    }

    private async Task OnPassphraseKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !isUnlocking)
        {
            try
            {
                await UnlockVault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during Enter key unlock: {ex.Message}");
                errorMessage = $"Failed to unlock vault: {ex.Message}";
                StateHasChanged();
            }
        }
    }

    private async Task UnlockVault()
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

            if (vaultManager == null)
            {
                vaultManager = await CreateVaultManager();
                if (vaultManager == null)
                {
                    errorMessage = "Failed to create vault manager";
                    isUnlocking = false;
                    StateHasChanged();
                    return;
                }
            }

            var saltBytes = CryptoHelpers.Sha256HashString(selectedVault.Id);
            var base64Salt = Convert.ToBase64String(saltBytes);
            var securePassphrase = new SecureString();
            foreach (char c in passphrase)
            {
                securePassphrase.AppendChar(c);
            }

            var keyBytes = await KeyDerivationService.DeriveKeyFromPassphraseAsync(
                core.Enums.KeyDerivationAlgorithm.Argon2,
                securePassphrase,
                base64Salt);

            var base64Key = Convert.ToBase64String(keyBytes);
            var vault = await vaultManager.LoadAsync(selectedVault.Id, base64Key, CancellationToken.None);
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
            
            // Notify parent that vault was unlocked
            await OnVaultUnlocked.InvokeAsync((unlockedVault, base64Key));
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

    private async Task<IVaultManager?> CreateVaultManager()
    {
        try
        {
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
                Console.WriteLine("Invalid stored credentials");
                return null;
            }

            // Create JavaScript S3 invoker
            var jsInvoker = new clypse.core.Cloud.Aws.S3.JavaScriptS3Invoker(JSRuntime);

            // Create vault manager using the factory
            var manager = VaultManagerFactory.CreateForBlazor(
                jsInvoker,
                credentials.AwsCredentials.AccessKeyId,
                credentials.AwsCredentials.SecretAccessKey,
                credentials.AwsCredentials.SessionToken,
                AwsS3Config.Region,
                AwsS3Config.BucketName,  
                credentials.AwsCredentials.IdentityId);

            Console.WriteLine("Vault manager created successfully");
            return manager;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating vault manager: {ex.Message}");
            return null;
        }
    }
}
