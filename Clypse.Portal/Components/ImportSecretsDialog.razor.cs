using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Clypse.Portal.Components;

/// <summary>
/// Code-behind for the import dialog. Logic lives in <see cref="ImportSecretsViewModel"/>; this only reads the file the
/// user picks, and says when the dialog is done.
/// </summary>
public partial class ImportSecretsDialog : MvvmComponentBase<ImportSecretsViewModel>
{
    /// <summary>
    /// Gets or sets what happens when the dialog is closed.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    private async Task LoadFileAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;
        await using var stream = file.OpenReadStream(ImportSecretsViewModel.MaximumFileSize);
        using var reader = new StreamReader(stream);
        ViewModel.LoadFile(file.Name, await reader.ReadToEndAsync());
    }

    private Task CloseAsync() => ViewModel.IsWorking ? Task.CompletedTask : OnClose.InvokeAsync();
}
