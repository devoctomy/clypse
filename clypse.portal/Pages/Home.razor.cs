using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Security;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Vault;
using clypse.portal.Models;
using clypse.portal.Services;

namespace clypse.portal.Pages;

public partial class Home : ComponentBase
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public IVaultManagerFactoryService VaultManagerFactory { get; set; } = default!;
    [Inject] public IVaultStorageService VaultStorage { get; set; } = default!;
    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public AppSettings AppSettings { get; set; } = default!;
    [Inject] public AwsS3Config AwsS3Config { get; set; } = default!;
    [Inject] public IKeyDerivationService KeyDerivationService { get; set; } = default!;

    private bool isLoggedIn = false;
    private string currentPage = "vaults"; // Default to vaults page
    private IVaultManager? vaultManager = null;
    private Components.Vaults? vaultsComponent;
    private Components.Credentials? credentialsComponent;
    private VaultMetadata? currentVault = null;
    private string? currentVaultKey = null;
    private IVault? loadedVault = null;
    private bool showVerifyDialog = false;
    private VaultVerifyResults? verifyResults = null;
    private bool showDeleteVaultDialog = false;
    private VaultMetadata? vaultToDelete = null;
    private bool isDeletingVault = false;
    private string? deleteVaultErrorMessage = null;

    [CascadingParameter] public Layout.HomeLayout? Layout { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await AuthService.Initialize();
            
            isLoggedIn = await AuthService.CheckAuthentication();
            if (!isLoggedIn)
            {
                Navigation.NavigateTo("/login");
                return;
            }

            // Set up communication with layout
            Layout?.SetHomePageReference(this);
            await UpdateNavigation();
        }
    }

    public async Task HandleNavigationAction(string action)
    {
        switch (action)
        {
            case "create-vault":
                currentPage = "create-vault";
                break;
            case "show-vaults":
                currentPage = "vaults";
                break;
            case "delete-vault":
                if (currentVault != null)
                {
                    ShowDeleteVaultDialog();
                }
                return; // Don't call StateHasChanged or UpdateNavigation as this is just showing a dialog
            case "create-credential":
                credentialsComponent?.ShowCreateDialog();
                return; // Don't call StateHasChanged or UpdateNavigation as this is just showing a dialog
            case "import":
                credentialsComponent?.ShowImportDialog();
                return; // Don't call StateHasChanged or UpdateNavigation as this is just showing a dialog
            case "lock-vault":
                await HandleLockVault();
                return; // HandleLockVault already calls StateHasChanged and UpdateNavigation
            case "refresh":
                await HandleRefresh();
                break;
            case "verify":
                await HandleVerify();
                break;
        }

        StateHasChanged();
        await UpdateNavigation();
    }

    private async Task UpdateNavigation()
    {
        var navigationItems = GetNavigationItems();
        Layout?.SetNavigationItems(navigationItems);
        await Task.CompletedTask;
    }

    private List<NavigationItem> GetNavigationItems()
    {
        return currentPage switch
        {
            "vaults" => new List<NavigationItem>
            {
                new() { Text = "Create Vault", Action = "create-vault", Icon = "bi bi-plus-circle" },
                new() { Text = "Refresh", Action = "refresh", Icon = "bi bi-arrow-clockwise" }
            },
            "credentials" => new List<NavigationItem>
            {
                new() { Text = "Create Credential", Action = "create-credential", Icon = "bi bi-plus-circle" },
                new() { Text = "Import", Action = "import", Icon = "bi bi-upload" },
                new() { Text = "Verify Vault", Action = "verify", Icon = "bi bi-shield-check", ButtonClass = "btn-success" },
                new() { Text = "Lock Vault", Action = "lock-vault", Icon = "bi bi-lock", ButtonClass = "btn-primary" },
                new() { Text = "Delete Vault", Action = "delete-vault", Icon = "bi bi-trash3", ButtonClass = "btn-danger" }
            },
            "create-vault" => new List<NavigationItem>
            {
                new() { Text = "Back to Vaults", Action = "show-vaults", Icon = "bi bi-arrow-left" }
            },
            _ => new List<NavigationItem>()
        };
    }

    private async Task HandleRefresh()
    {
        try
        {
            Console.WriteLine("Refreshing vaults...");

            // Refresh the vaults component if we're on the vaults page
            if (currentPage == "vaults" && vaultsComponent != null)
            {
                await vaultsComponent.RefreshVaults();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing vaults: {ex.Message}");
        }
    }

    private async Task<IVaultManager?> CreateVaultManager()
    {
        try
        {
            var credentials = await AuthService.GetStoredCredentials();
            if (credentials?.AwsCredentials == null)
            {
                Console.WriteLine("No valid credentials found");
                return null;
            }

            // Create JavaScript S3 invoker
            var jsInvoker = new JavaScriptS3Invoker(JSRuntime);

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

    private async Task HandleCreateVault(VaultCreationRequest request)
    {
        try
        {
            Console.WriteLine($"Creating vault: {request.Name} - {request.Description}");

            // For vault creation, we need to create a temporary vault manager instance
            // using the VaultManagerFactory, not the bootstrapper (which is for existing vaults)
            var credentials = await AuthService.GetStoredCredentials();
            if (credentials?.AwsCredentials == null)
            {
                Console.WriteLine("No valid credentials found - cannot create vault");
                return;
            }

            // Create JavaScript S3 invoker
            var jsInvoker = new JavaScriptS3Invoker(JSRuntime);

            // Create temporary vault manager for vault creation
            var tempVaultManager = VaultManagerFactory.CreateForBlazor(
                jsInvoker,
                credentials.AwsCredentials.AccessKeyId,
                credentials.AwsCredentials.SecretAccessKey,
                credentials.AwsCredentials.SessionToken,
                AwsS3Config.Region,
                AwsS3Config.BucketName,  
                credentials.AwsCredentials.IdentityId);

            // Create the vault
            var vault = tempVaultManager.Create(request.Name, request.Description);
            Console.WriteLine($"Vault created with ID: {vault.Info.Id}");

            // Derive encryption key from passphrase
            var keyBytes = await KeyDerivationService.DeriveKeyFromPassphraseAsync(
                request.Passphrase,
                vault.Info.Base64Salt);
            var base64Key = Convert.ToBase64String(keyBytes);

            // Save the vault
            Console.WriteLine("Saving vault...");
            var saveResults = await tempVaultManager.SaveAsync(
                vault,
                base64Key,
                null,
                CancellationToken.None);

            Console.WriteLine($"Vault saved successfully. Results: {saveResults}");
            
            // Dispose of the temporary vault manager as it's no longer needed
            // A new one will be created when the vault is unlocked
            tempVaultManager.Dispose();
            
            // Persist vault metadata immediately since we know the name and description
            var existingVaults = await VaultStorage.GetVaultsAsync();
            var existingVault = existingVaults.FirstOrDefault(v => v.Id == vault.Info.Id);
            
            if (existingVault != null)
            {
                // Update existing vault with the metadata
                existingVault.Name = vault.Info.Name;
                existingVault.Description = vault.Info.Description;
            }
            else
            {
                // Add new vault with full metadata
                existingVaults.Add(new VaultMetadata 
                { 
                    Id = vault.Info.Id,
                    Name = vault.Info.Name,
                    Description = vault.Info.Description
                });
            }
            
            // Save the updated vault list
            await VaultStorage.SaveVaultsAsync(existingVaults);
            Console.WriteLine("Vault metadata persisted to localStorage");
            
            // Navigate back to vaults page
            currentPage = "vaults";
            StateHasChanged();
            await UpdateNavigation();
            
            // Refresh the vault list to show the newly created vault
            if (vaultsComponent != null)
            {
                await vaultsComponent.RefreshVaults();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating vault: {ex.Message}");
            // Don't navigate away on error so user can see the error and try again
        }
    }

    private async Task HandleCancelCreateVault()
    {
        currentPage = "vaults";
        StateHasChanged();
        await UpdateNavigation();
    }

    private async Task HandleVaultUnlocked((VaultMetadata vault, string key, IVaultManager manager) vaultData)
    {
        currentVault = vaultData.vault;
        currentVaultKey = vaultData.key;
        
        // Use the vault-specific manager provided by the bootstrapper
        vaultManager = vaultData.manager;

        // Load and store the vault instance using the vault-specific manager
        loadedVault = await vaultManager.LoadAsync(currentVault.Id, currentVaultKey, CancellationToken.None);
        
        currentPage = "credentials";
        StateHasChanged();
        await UpdateNavigation();
    }

    public async Task HandleLockVault()
    {
        vaultManager?.Dispose();
        vaultManager = null;
        currentVault = null;
        currentVaultKey = null;
        loadedVault = null;
        currentPage = "vaults";
        StateHasChanged();
        await UpdateNavigation();
    }

    private async Task HandleVaultUpdated()
    {
        if (vaultManager == null || currentVault == null || currentVaultKey == null)
        {
            return;
        }

        try
        {
            // Reload the vault to get the updated index
            loadedVault = await vaultManager.LoadAsync(currentVault.Id, currentVaultKey, CancellationToken.None);
            
            // Update the current vault metadata with the fresh index entries
            currentVault.IndexEntries = loadedVault.Index.Entries.ToList();
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing vault: {ex.Message}");
        }
    }

    private async Task HandleVerify()
    {
        if (loadedVault == null || currentVaultKey == null || vaultManager == null)
        {
            return;
        }

        try
        {
            showVerifyDialog = true;
            verifyResults = null;
            StateHasChanged();

            verifyResults = await vaultManager.VerifyAsync(loadedVault, currentVaultKey, CancellationToken.None);
            StateHasChanged();
        }
        catch (Exception)
        {
            // Close dialog on error
            showVerifyDialog = false;
            verifyResults = null;
            StateHasChanged();
        }
    }

    private void ShowDeleteVaultDialog()
    {
        if (currentVault == null) return;
        
        vaultToDelete = currentVault;
        deleteVaultErrorMessage = null;
        showDeleteVaultDialog = true;
        StateHasChanged();
    }
    
    private void CancelDeleteVault()
    {
        showDeleteVaultDialog = false;
        vaultToDelete = null;
        deleteVaultErrorMessage = null;
        isDeletingVault = false;
        StateHasChanged();
    }
    
    private async Task HandleDeleteVaultConfirm()
    {
        if (vaultToDelete == null || vaultManager == null || currentVaultKey == null || loadedVault == null)
        {
            CancelDeleteVault();
            return;
        }

        try
        {
            isDeletingVault = true;
            deleteVaultErrorMessage = null;
            StateHasChanged();
            
            // Delete the vault using the vault manager
            await vaultManager.DeleteAsync(loadedVault, currentVaultKey, CancellationToken.None);
            
            // Remove the vault from local storage
            await VaultStorage.RemoveVaultAsync(vaultToDelete.Id);
            
            // Close the dialog
            CancelDeleteVault();
            
            // Clear current vault state since we just deleted it
            currentVault = null;
            currentVaultKey = null;
            loadedVault = null;
            
            // Navigate back to vaults page and refresh
            currentPage = "vaults";
            StateHasChanged();
            await UpdateNavigation();
            
            // Refresh the vaults list
            await HandleRefresh();
        }
        catch (Exception ex)
        {
            deleteVaultErrorMessage = $"Failed to delete vault: {ex.Message}";
            Console.WriteLine($"Error deleting vault: {ex.Message}");
        }
        finally
        {
            isDeletingVault = false;
            StateHasChanged();
        }
    }
}
