using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Vault;

namespace clypse.portal.Components;

/// <summary>
/// Code-behind for the create vault dialog. Business logic is in <see cref="VaultCreateDialogViewModel"/>.
/// </summary>
public partial class VaultCreateDialog : MvvmComponentBase<VaultCreateDialogViewModel>
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public bool IsCreating { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<VaultCreationRequest> OnCreateVault { get; set; }

    private ElementReference nameInput;
    private bool previousShowState;

    protected override void OnParametersSet()
    {
        ViewModel.IsCreating = IsCreating;
        ViewModel.ErrorMessage = ErrorMessage;
        ViewModel.OnCreateVaultCallback = request => OnCreateVault.InvokeAsync(request);
        ViewModel.OnCancelCallback = () => OnCancel.InvokeAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Show && !previousShowState)
        {
            ViewModel.ClearForm();
            await Task.Delay(100);
            try
            {
                await nameInput.FocusAsync();
            }
            catch
            {
                // Focus may fail if not yet rendered
            }
        }

        previousShowState = Show;
    }
}
