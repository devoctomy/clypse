using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using clypse.portal.Models.Vault;

namespace clypse.portal.Components;

public partial class UnlockVaultDialog : ComponentBase
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public VaultMetadata? Vault { get; set; }
    [Parameter] public bool IsUnlocking { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<string> OnUnlock { get; set; }

    private string passphrase = string.Empty;
    private ElementReference passphraseInput;

    protected override async Task OnParametersSetAsync()
    {
        // Clear passphrase when dialog is shown
        if (IsVisible && Vault != null)
        {
            passphrase = string.Empty;
            await Task.Delay(100); // Small delay to ensure modal is rendered
            try
            {
                await passphraseInput.FocusAsync();
            }
            catch (Exception ex)
            {
                // Focus might fail if component is not yet fully rendered
                Console.WriteLine($"Failed to focus passphrase input: {ex.Message}");
            }
        }
    }

    private async Task OnPassphraseKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !IsUnlocking && !string.IsNullOrEmpty(passphrase))
        {
            await OnUnlockClick();
        }
    }

    private async Task OnUnlockClick()
    {
        if (!string.IsNullOrEmpty(passphrase))
        {
            await OnUnlock.InvokeAsync(passphrase);
        }
    }

    private async Task OnCancelClick()
    {
        passphrase = string.Empty;
        await OnCancel.InvokeAsync();
    }
}