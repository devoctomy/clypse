using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Clypse.Portal.Components;

/// <summary>
/// Code-behind for the file viewer. Logic lives in <see cref="FileViewerViewModel"/>; this only passes the file in, says
/// when the viewer is done, and has what was decrypted dropped when the viewer goes.
/// </summary>
public partial class FileViewerDialog : MvvmComponentBase<FileViewerViewModel>
{
    private bool loaded;
    private string? loadedId;

    /// <summary>
    /// Gets or sets the id of the file to show.
    /// </summary>
    [Parameter]
    public string? FileId { get; set; }

    /// <summary>
    /// Gets or sets what happens when the viewer is closed.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Gets or sets what happens when the file's details are asked for, given the file's id.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnDetails { get; set; }

    /// <summary>
    /// Gets or sets what happens when deleting the file is asked for, given the file's id.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnDelete { get; set; }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (!loaded || loadedId != FileId)
        {
            loaded = true;
            loadedId = FileId;
            await ViewModel.LoadAsync(FileId);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Closed, or the vault locked and the page took the viewer away: nothing decrypted is kept.
            ViewModel.Clear();
        }

        base.Dispose(disposing);
    }

    private Task CloseAsync()
    {
        if (ViewModel.IsFullScreen)
        {
            // Escape, with the image filling the page: back to the file's details first.
            ViewModel.ToggleFullScreenCommand.Execute(null);
            return Task.CompletedTask;
        }

        return OnClose.InvokeAsync();
    }

    private Task DetailsAsync() => ViewModel.FileId is { } id ? OnDetails.InvokeAsync(id) : Task.CompletedTask;

    private Task DeleteAsync() => ViewModel.FileId is { } id ? OnDelete.InvokeAsync(id) : Task.CompletedTask;
}
