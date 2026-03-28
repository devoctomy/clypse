using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using Microsoft.AspNetCore.Components.Web;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Vault;

namespace clypse.portal.Components;

/// <summary>
/// Code-behind for the unlock vault dialog. Business logic is in <see cref="UnlockVaultDialogViewModel"/>.
/// </summary>
public partial class UnlockVaultDialog : MvvmComponentBase<UnlockVaultDialogViewModel>
{
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public VaultMetadata? Vault { get; set; }
    [Parameter] public bool IsUnlocking { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<string> OnUnlock { get; set; }

    private ElementReference passphraseInput;

    protected override async Task OnParametersSetAsync()
    {
        ViewModel.Vault = Vault;
        ViewModel.IsUnlocking = IsUnlocking;
        ViewModel.ErrorMessage = ErrorMessage;
        ViewModel.OnUnlockCallback = passphrase => OnUnlock.InvokeAsync(passphrase);
        ViewModel.OnCancelCallback = () => OnCancel.InvokeAsync();

        if (IsVisible && Vault != null)
        {
            ViewModel.ResetPassphrase();
            await Task.Delay(100);
            try
            {
                await passphraseInput.FocusAsync();
            }
            catch
            {
                // Focus may fail if not yet rendered
            }
        }
    }

    private async Task OnPassphraseKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !IsUnlocking && !string.IsNullOrEmpty(ViewModel.Passphrase))
        {
            await ViewModel.UnlockCommand.ExecuteAsync(null);
        }
    }
}
