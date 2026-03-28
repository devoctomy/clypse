using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using Microsoft.AspNetCore.Components.Forms;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Import;

namespace clypse.portal.Components;

/// <summary>
/// Code-behind for the import secrets dialog. Business logic is in <see cref="ImportSecretsDialogViewModel"/>.
/// </summary>
public partial class ImportSecretsDialog : MvvmComponentBase<ImportSecretsDialogViewModel>
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<ImportResult> OnImport { get; set; }

    protected override void OnParametersSet()
    {
        ViewModel.OnCancelCallback = () => OnCancel.InvokeAsync();
        ViewModel.OnImportCallback = result => OnImport.InvokeAsync(result);

        if (!Show)
        {
            ViewModel.Reset();
        }
    }

    private Task HandleFileSelected(InputFileChangeEventArgs e) => ViewModel.HandleFileSelectedAsync(e);
}
