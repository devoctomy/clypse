using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Clypse.Portal.Components;

/// <summary>
/// Code-behind for a file's details. Logic lives in <see cref="FileDetailsViewModel"/>; this only passes the file in,
/// and says when the dialog is done.
/// </summary>
public partial class FileDetailsDialog : MvvmComponentBase<FileDetailsViewModel>
{
    private bool loaded;
    private string? loadedId;

    /// <summary>
    /// Gets or sets the id of the file.
    /// </summary>
    [Parameter]
    public string? FileId { get; set; }

    /// <summary>
    /// Gets or sets what happens when the dialog is closed, or the details saved.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!loaded || loadedId != FileId)
        {
            loaded = true;
            loadedId = FileId;
            ViewModel.Load(FileId);
        }
    }

    private async Task SaveAsync()
    {
        if (await ViewModel.SaveAsync())
        {
            await OnClose.InvokeAsync();
        }
    }

    private Task CloseAsync() => ViewModel.IsWorking ? Task.CompletedTask : OnClose.InvokeAsync();
}
