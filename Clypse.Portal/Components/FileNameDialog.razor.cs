using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Clypse.Portal.Components;

/// <summary>
/// Code-behind for the dialog that asks for a name. Logic lives in <see cref="FileNameViewModel"/>; this only passes in
/// what is being named, and says when the dialog is done.
/// </summary>
public partial class FileNameDialog : MvvmComponentBase<FileNameViewModel>
{
    private bool loaded;
    private FileNameViewModel.FileNameMode loadedMode;
    private string? loadedFolder;

    /// <summary>
    /// Gets or sets what the dialog is naming.
    /// </summary>
    [Parameter]
    public FileNameViewModel.FileNameMode Mode { get; set; }

    /// <summary>
    /// Gets or sets, for a new folder, the path of the folder it goes in; for a rename, the folder's path.
    /// </summary>
    [Parameter]
    public string? Folder { get; set; }

    /// <summary>
    /// Gets or sets what happens when the dialog is closed, or the name saved.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <summary>
    /// Gets or sets what happens when a markdown document or text file has been made, given its id (e.g. it is opened
    /// in the editor). The dialog is not told to close as well: this takes its place.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnCreated { get; set; }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!loaded || loadedMode != Mode || loadedFolder != Folder)
        {
            loaded = true;
            loadedMode = Mode;
            loadedFolder = Folder;
            ViewModel.Load(Mode, Folder);
        }
    }

    private async Task SaveAsync()
    {
        if (!await ViewModel.SaveAsync())
        {
            return;
        }

        if (ViewModel.CreatedFileId is { } id && OnCreated.HasDelegate)
        {
            await OnCreated.InvokeAsync(id);
        }
        else
        {
            await OnClose.InvokeAsync();
        }
    }

    private Task CloseAsync() => ViewModel.IsWorking ? Task.CompletedTask : OnClose.InvokeAsync();
}
