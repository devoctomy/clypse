using Microsoft.AspNetCore.Components;
using clypse.portal.Models;

namespace clypse.portal.Components;

public partial class CreateVault : ComponentBase
{
    private string vaultName = string.Empty;
    private string vaultDescription = string.Empty;
    private string vaultPassphrase = string.Empty;
    private string vaultPassphraseConfirm = string.Empty;
    private string errorMessage = string.Empty;
    private bool isCreating = false;

    [Parameter] public EventCallback<VaultCreationRequest> OnCreateVault { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task DoCreateVault()
    {
        errorMessage = string.Empty;

        // Validation
        if (string.IsNullOrWhiteSpace(vaultName))
        {
            errorMessage = "Vault name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(vaultDescription))
        {
            errorMessage = "Vault description is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(vaultPassphrase))
        {
            errorMessage = "Passphrase is required.";
            return;
        }

        if (vaultPassphrase != vaultPassphraseConfirm)
        {
            errorMessage = "Passphrases do not match.";
            return;
        }

        if (vaultPassphrase.Length < 8)
        {
            errorMessage = "Passphrase must be at least 8 characters long.";
            return;
        }

        isCreating = true;
        StateHasChanged();

        try
        {
            var request = new VaultCreationRequest
            {
                Name = vaultName.Trim(),
                Description = vaultDescription.Trim(),
                Passphrase = vaultPassphrase
            };

            await OnCreateVault.InvokeAsync(request);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error creating vault: {ex.Message}";
        }
        finally
        {
            isCreating = false;
            StateHasChanged();
        }
    }

    private async Task Cancel()
    {
        // Clear form
        vaultName = string.Empty;
        vaultDescription = string.Empty;
        vaultPassphrase = string.Empty;
        vaultPassphraseConfirm = string.Empty;
        errorMessage = string.Empty;

        await OnCancel.InvokeAsync();
    }
}
