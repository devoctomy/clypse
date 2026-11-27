using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Clypse.Portal.Components;

/// <summary>
/// Code-behind for the text editor, shown in the Files tab in place of the list. Logic lives in
/// <see cref="TextFileEditorViewModel"/>; this passes the file in, wires up the keyboard shortcuts and the check before
/// leaving the page, says when the editor is done, and has the text dropped when the editor goes.
/// </summary>
public partial class TextFileEditorPanel : MvvmComponentBase<TextFileEditorViewModel>
{
    private DotNetObjectReference<TextFileEditorPanel>? selfReference;
    private bool shortcutsAttached;
    private bool subscribed;
    private bool loaded;
    private string? loadedId;

    /// <summary>
    /// Gets or sets the id of the file to edit.
    /// </summary>
    [Parameter]
    public string? FileId { get; set; }

    /// <summary>
    /// Gets or sets what happens when the editor closes.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Handles Ctrl+S, Ctrl+B and Ctrl+I, called from <c>clypseEditor.attachShortcuts</c>.
    /// </summary>
    /// <param name="key">The key pressed with Ctrl (or Cmd).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [JSInvokable]
    public Task OnShortcut(string key)
    {
        return key switch
        {
            "s" => ViewModel.SaveCommand.ExecuteAsync(null),
            "b" => ViewModel.FormatCommand.ExecuteAsync(MarkdownFormat.Bold),
            "i" => ViewModel.FormatCommand.ExecuteAsync(MarkdownFormat.Italic),
            _ => Task.CompletedTask,
        };
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (!subscribed)
        {
            subscribed = true;
            ViewModel.CloseRequested += OnCloseRequested;
        }

        if (!loaded || loadedId != FileId)
        {
            loaded = true;
            loadedId = FileId;
            await ViewModel.LoadAsync(FileId);
        }
    }

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // The text area only exists once the text has been decrypted. The script says whether it found it, so the
        // shortcuts are attached on the first render that has it.
        if (!ViewModel.IsReady)
        {
            shortcutsAttached = false;
        }
        else if (!shortcutsAttached)
        {
            selfReference ??= DotNetObjectReference.Create(this);
            shortcutsAttached = await JSRuntime.InvokeAsync<bool>("clypseEditor.attachShortcuts", TextFileEditorViewModel.EditorElementId, selfReference);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (JSRuntime is IJSInProcessRuntime inProcessRuntime)
            {
                inProcessRuntime.InvokeVoid("clypseEditor.detachShortcuts", TextFileEditorViewModel.EditorElementId);
            }

            selfReference?.Dispose();
            ViewModel.CloseRequested -= OnCloseRequested;

            // Closed, or the vault locked and the page took the editor away: nothing decrypted is kept.
            ViewModel.Clear();
        }

        base.Dispose(disposing);
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        _ = InvokeAsync(() => OnClose.InvokeAsync());
    }

    private void OnBeforeInternalNavigation(LocationChangingContext context)
    {
        // Held while the user is asked about unsaved changes, and carried on with once they are saved or discarded.
        var target = context.TargetLocation;
        if (ViewModel.Hold(() => Navigation.NavigateTo(target)))
        {
            context.PreventNavigation();
        }
    }
}
