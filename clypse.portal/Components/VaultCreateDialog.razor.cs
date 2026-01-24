using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using clypse.portal.Models;

namespace clypse.portal.Components;

public partial class VaultCreateDialog : ComponentBase
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public bool Show { get; set; }
    [Parameter] public bool IsCreating { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<VaultCreationRequest> OnCreateVault { get; set; }

    private string vaultName = string.Empty;
    private string vaultDescription = string.Empty;
    private string vaultPassphrase = string.Empty;
    private string vaultPassphraseConfirm = string.Empty;
    private ElementReference nameInput;
    private bool previousShowState;

    protected override async Task OnParametersSetAsync()
    {
        // Only clear form and focus when dialog is being opened (transitioning from hidden to shown)
        if (Show && !previousShowState)
        {
            ClearForm();
            await Task.Delay(100); // Small delay to ensure modal is rendered
            try
            {
                await nameInput.FocusAsync();
            }
            catch (Exception ex)
            {
                // Focus might fail if component is not yet fully rendered
                Console.WriteLine($"Failed to focus name input: {ex.Message}");
            }
        }
        
        // Update previous state for next comparison
        previousShowState = Show;
    }

    private void ClearForm()
    {
        vaultName = string.Empty;
        vaultDescription = string.Empty;
        vaultPassphrase = string.Empty;
        vaultPassphraseConfirm = string.Empty;
    }

    private bool IsFormValid()
    {
        return !string.IsNullOrWhiteSpace(vaultName) &&
               !string.IsNullOrWhiteSpace(vaultDescription) &&
               !string.IsNullOrWhiteSpace(vaultPassphrase) &&
               !string.IsNullOrWhiteSpace(vaultPassphraseConfirm) &&
               vaultPassphrase == vaultPassphraseConfirm &&
               vaultPassphrase.Length >= 8;
    }

    private async Task HandleCreateVault()
    {
        // Validation
        if (string.IsNullOrWhiteSpace(vaultName))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(vaultDescription))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(vaultPassphrase))
        {
            return;
        }

        if (vaultPassphrase != vaultPassphraseConfirm)
        {
            return;
        }

        if (vaultPassphrase.Length < 8)
        {
            return;
        }

        var request = new VaultCreationRequest
        {
            Name = vaultName.Trim(),
            Description = vaultDescription.Trim(),
            Passphrase = vaultPassphrase
        };

        await OnCreateVault.InvokeAsync(request);
    }

    private async Task HandleCancel()
    {
        ClearForm();
        await OnCancel.InvokeAsync();
    }
}