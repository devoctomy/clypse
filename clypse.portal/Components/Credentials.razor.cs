using clypse.core.Enums;
using clypse.core.Secrets;
using clypse.core.Secrets.Import;
using clypse.core.Vault;
using clypse.portal.Models;
using Microsoft.AspNetCore.Components;
using System.Linq;

namespace clypse.portal.Components;

public partial class Credentials : ComponentBase
{
    [Parameter] public VaultMetadata? CurrentVault { get; set; }
    [Parameter] public string? CurrentVaultKey { get; set; }
    [Parameter] public IVault? LoadedVault { get; set; }
    [Parameter] public EventCallback OnLockVault { get; set; }
    [Parameter] public EventCallback OnVaultUpdated { get; set; }
    [Parameter] public EventCallback OnCreateCredential { get; set; }
    [Parameter] public IVaultManager? VaultManager { get; set; }
    
    private bool showImportDialog = false;
    private bool isLoadingSecret = false;
    private bool isSavingSecret = false;
    private bool showDeleteConfirmation = false;
    private bool isDeletingSecret = false;
    private string secretIdToDelete = string.Empty;
    private string deleteConfirmationMessage = string.Empty;

    // SecretDialog properties
    private bool showSecretDialog = false;
    private Secret? currentSecret = null;
    private SecretDialog.SecretDialogMode secretDialogMode = SecretDialog.SecretDialogMode.Create;
    private string searchTerm = string.Empty;
    private List<VaultIndexEntry> filteredEntries = [];

    private async Task HandleLockVault()
    {
        await OnLockVault.InvokeAsync();
    }
    
    private async Task ViewSecret(string secretId)
    {
        if (VaultManager == null || LoadedVault == null || string.IsNullOrEmpty(CurrentVaultKey))
        {
            return;
        }

        try
        {
            isLoadingSecret = true;
            StateHasChanged();
            
            var secret = await VaultManager.GetSecretAsync(LoadedVault, secretId, CurrentVaultKey, CancellationToken.None);
            
            if (secret != null)
            {
                currentSecret = secret; // Reuse the currentSecret for viewing
                secretDialogMode = SecretDialog.SecretDialogMode.View;
                showSecretDialog = true;
            }
        }
        catch
        {
            // Error handling is done via the loading state
        }
        finally
        {
            isLoadingSecret = false;
            StateHasChanged();
        }
    }
    
    private async Task EditSecret(string secretId)
    {
        if (VaultManager == null || LoadedVault == null || string.IsNullOrEmpty(CurrentVaultKey))
        {
            return;
        }

        try
        {
            isLoadingSecret = true;
            StateHasChanged();
            
            var secret = await VaultManager.GetSecretAsync(LoadedVault, secretId, CurrentVaultKey, CancellationToken.None);
            
            if (secret != null)
            {
                currentSecret = secret;
                secretDialogMode = SecretDialog.SecretDialogMode.Edit;
                showSecretDialog = true;
            }
        }
        catch
        {
            // Error handling is done via the loading state
        }
        finally
        {
            isLoadingSecret = false;
            StateHasChanged();
        }
    }
    
    public void ShowCreateDialog()
    {
        currentSecret = new WebSecret();
        secretDialogMode = SecretDialog.SecretDialogMode.Create;
        showSecretDialog = true;
        StateHasChanged();
    }
    
    private void CloseCreateDialog()
    {
        showSecretDialog = false;
        currentSecret = null;
        StateHasChanged();
    }

    public void ShowImportDialog()
    {
        showImportDialog = true;
        StateHasChanged();
    }
    
    private void CloseImportDialog()
    {
        showImportDialog = false;
        StateHasChanged();
    }

    private async Task HandleImportSecrets(ImportSecretsDialog.ImportResult result)
    {
        if (VaultManager == null || LoadedVault == null || string.IsNullOrEmpty(CurrentVaultKey))
        {
            return;
        }

        try
        {
            // Add the mapped secrets to the vault
            var addResult = LoadedVault.AddRawSecrets(result.MappedSecrets, SecretType.Web);
            
            if (addResult)
            {
                // Save the vault with the changes
                var results = await VaultManager.SaveAsync(LoadedVault, CurrentVaultKey, null, CancellationToken.None);
                
                // Notify parent that vault was updated so it can refresh the index
                await OnVaultUpdated.InvokeAsync();
            }
            
            // Close the import dialog
            CloseImportDialog();
        }
        catch (Exception ex)
        {
            // Error handling - could show a toast notification here
            Console.WriteLine($"Error importing secrets: {ex.Message}");
        }
    }
    
    private async Task HandleSaveSecret(Secret editedSecret)
    {
        if (VaultManager == null || LoadedVault == null || string.IsNullOrEmpty(CurrentVaultKey))
        {
            return;
        }

        try
        {
            isSavingSecret = true;
            StateHasChanged();
            
            // Update the secret in the vault
            LoadedVault.UpdateSecret(editedSecret);
            
            // Save the vault with the changes (this automatically updates the index)
            await VaultManager.SaveAsync(LoadedVault, CurrentVaultKey, null, CancellationToken.None);
            
            // Close the dialog
            CloseSecretDialog();
            
            // Notify parent that vault was updated so it can refresh the index
            await OnVaultUpdated.InvokeAsync();
        }
        catch
        {
            // Error handling - could show a toast notification here
        }
        finally
        {
            isSavingSecret = false;
            StateHasChanged();
        }
    }
    
    private async Task HandleCreateSecret(Secret newSecret)
    {
        if (VaultManager == null || LoadedVault == null || string.IsNullOrEmpty(CurrentVaultKey))
        {
            return;
        }

        try
        {
            isSavingSecret = true;
            StateHasChanged();
            
            // Add the new secret to the vault
            LoadedVault.AddSecret(newSecret);
            
            // Save the vault with the changes (this automatically updates the index)
            await VaultManager.SaveAsync(LoadedVault, CurrentVaultKey, null, CancellationToken.None);
            
            // Close the dialog
            CloseCreateDialog();
            
            // Notify parent that vault was updated so it can refresh the index
            await OnVaultUpdated.InvokeAsync();
        }
        catch
        {
            // Error handling - could show a toast notification here
        }
        finally
        {
            isSavingSecret = false;
            StateHasChanged();
        }
    }

    protected override void OnParametersSet()
    {
#if DEBUG
        if (CurrentVault?.IndexEntries != null)
        {
            for (var i = 0; i < 50; i++)
            {
                var testSecretName = $"Test Secret {i + 1}";
                if (CurrentVault.IndexEntries.Any(e => e.Name == testSecretName))
                {
                    continue;
                }
                
                CurrentVault.IndexEntries.Add(new VaultIndexEntry(
                    Guid.NewGuid().ToString(),
                    testSecretName,
                    "This secret cannot be opened.",
                    "foo,bar"));
            }
        }
#endif

        // Initialize filtered entries with sorted list
        if (CurrentVault?.IndexEntries != null)
        {
            filteredEntries = CurrentVault.IndexEntries
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            filteredEntries = [];
        }
    }

    private void HandleSearch()
    {
        if (CurrentVault?.IndexEntries == null)
        {
            filteredEntries = [];
            return;
        }

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredEntries = CurrentVault.IndexEntries
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

          return;
        }

        var term = searchTerm.Trim().ToLower();
        filteredEntries = CurrentVault.IndexEntries
            .Where(entry => 
                (entry.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (entry.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (entry.Tags?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ClearSearch()
    {
        searchTerm = string.Empty;
        HandleSearch();
    }
    
    private void ShowDeleteConfirmation(string secretId, string secretName)
    {
        secretIdToDelete = secretId;
        deleteConfirmationMessage = $"Are you sure you want to delete the secret '{secretName}'?";
        showDeleteConfirmation = true;
        StateHasChanged();
    }
    
    private void CancelDeleteConfirmation()
    {
        showDeleteConfirmation = false;
        secretIdToDelete = string.Empty;
        deleteConfirmationMessage = string.Empty;
        StateHasChanged();
    }
    
    private async Task HandleDeleteSecret()
    {
        if (VaultManager == null || LoadedVault == null || string.IsNullOrEmpty(CurrentVaultKey) || string.IsNullOrEmpty(secretIdToDelete))
        {
            CancelDeleteConfirmation();
            return;
        }

        try
        {
            isDeletingSecret = true;
            StateHasChanged();
            
            // Mark the secret for deletion in the vault
            var deleted = LoadedVault.DeleteSecret(secretIdToDelete);
            
            if (deleted)
            {
                // Save the vault with the changes (this automatically handles the deletion)
                await VaultManager.SaveAsync(LoadedVault, CurrentVaultKey, null, CancellationToken.None);
                
                // Notify parent that vault was updated so it can refresh the index
                await OnVaultUpdated.InvokeAsync();
            }
            
            // Close the confirmation dialog
            CancelDeleteConfirmation();
        }
        catch
        {
            // Error handling - could show a toast notification here
        }
        finally
        {
            isDeletingSecret = false;
            StateHasChanged();
        }
    }

    private async Task HandleSecretDialogSave(Secret secret)
    {
        switch (secretDialogMode)
        {
            case SecretDialog.SecretDialogMode.Create:
                await HandleCreateSecret(secret);
                break;
            case SecretDialog.SecretDialogMode.Edit:
                await HandleSaveSecret(secret);
                break;
            case SecretDialog.SecretDialogMode.View:
                // View mode doesn't save
                CloseSecretDialog();
                break;
        }
    }

    private void CloseSecretDialog()
    {
        showSecretDialog = false;
        currentSecret = null;
        StateHasChanged();
    }
}
