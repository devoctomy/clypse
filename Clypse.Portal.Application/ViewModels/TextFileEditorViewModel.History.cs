using System.ComponentModel;
using Devoctomy.Blazwiz.UI.Sync.ViewModels;

namespace Clypse.Portal.Application.ViewModels;

/// <content>
/// The file's history: its past versions, one of which can be shown read only in place of the text.
/// </content>
public partial class TextFileEditorViewModel
{
    private string versionText = string.Empty;
    private string versionPreviewHtml = string.Empty;

    /// <summary>
    /// Gets the file's past versions: the History dropdown, and the bar above a past version shown.
    /// </summary>
    public FileHistoryViewModel History { get; }

    /// <summary>
    /// Gets a value indicating whether a past version is shown, read only, in place of the text.
    /// </summary>
    public bool IsViewingVersion => History.IsViewingVersion;

    /// <summary>
    /// Gets or sets what the editor shows: the text being edited, or a past version's text while one is shown. A past
    /// version cannot be edited, so setting it then does nothing.
    /// </summary>
    public string EditorText
    {
        get => IsViewingVersion ? versionText : Content;
        set
        {
            if (!IsViewingVersion)
            {
                Content = value;
            }
        }
    }

    /// <summary>
    /// Gets the preview shown: of the text being edited, or of a past version's text while one is shown.
    /// </summary>
    public string ShownPreviewHtml => IsViewingVersion ? versionPreviewHtml : PreviewHtml;

    /// <inheritdoc/>
    public bool HasUnsavedChanges => IsDirty;

    /// <inheritdoc/>
    public Task<bool> SaveChangesAsync() => TrySaveAsync();

    /// <inheritdoc/>
    public void DiscardChanges()
    {
        Content = savedContent;
    }

    // The file's history is of its content, which is a synced file of the vault's own.
    private void SetHistory(string id)
    {
        if (session.Vault is { } vault && session.GetContentKey(id) is { } key)
        {
            History.SetFile(vault.Id, key, this);
        }
    }

    private void RenderVersionPreview()
    {
        versionPreviewHtml = isMarkdown && !IsPreviewCollapsed && IsViewingVersion ? renderer.ToHtml(versionText) : string.Empty;
        OnPropertyChanged(nameof(ShownPreviewHtml));
    }

    private void ClearVersion()
    {
        versionText = string.Empty;
        versionPreviewHtml = string.Empty;
        OnPropertyChanged(nameof(EditorText));
        OnPropertyChanged(nameof(ShownPreviewHtml));
    }

    // The page shows the history's dropdown and bar, so it is shown again whenever they change.
    private void OnHistoryChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(History));
        if (e.PropertyName == nameof(FileHistoryViewModel.IsViewingVersion))
        {
            OnPropertyChanged(nameof(IsViewingVersion));
            OnPropertyChanged(nameof(EditorText));
            OnPropertyChanged(nameof(ShownPreviewHtml));
        }
    }

    // A past version is shown as its text. Going back to the current one needs nothing read: a restore saves it
    // through the session, and the editor follows the session's changes.
    private void OnShownVersionChanged(object? sender, EventArgs e)
    {
        if (History.SelectedContent is not { } shown || file == null)
        {
            ClearVersion();
            return;
        }

        try
        {
            // The history holds the version's content, to restore it: it is not wiped here.
            versionText = Decode(shown.Content).Text;
        }
        catch (Exception ex)
        {
            Error = errorText.Describe(ex);
            History.BackToCurrent();
            return;
        }

        OnPropertyChanged(nameof(EditorText));
        RenderVersionPreview();
    }
}
