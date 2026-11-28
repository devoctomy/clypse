using Clypse.Portal.Models.Files;
using CommunityToolkit.Mvvm.Input;

namespace Clypse.Portal.Application.ViewModels;

/// <content>
/// Changes made elsewhere to the open file: a sync that changes or deletes it, and the vault locking. With unsaved
/// edits, the user is asked which version to keep.
/// </content>
public partial class TextFileEditorViewModel
{
    private DecodedText? theirText;
    private bool isRemoteChangeDialogOpen;
    private bool isRemoteDeleted;

    /// <summary>
    /// Gets a value indicating whether the dialog asking what to do about a file changed by a sync is showing.
    /// </summary>
    public bool IsRemoteChangeDialogOpen
    {
        get => isRemoteChangeDialogOpen;
        private set => SetProperty(ref isRemoteChangeDialogOpen, value);
    }

    /// <summary>
    /// Gets a value indicating whether the sync deleted the file (rather than changed it).
    /// </summary>
    public bool IsRemoteDeleted
    {
        get => isRemoteDeleted;
        private set => SetProperty(ref isRemoteDeleted, value);
    }

    /// <summary>
    /// Keeps the user's unsaved edits after a sync changed or deleted the file. Saving writes them over the synced
    /// version; if the file was deleted, saving adds it again.
    /// </summary>
    [RelayCommand]
    public void KeepMine()
    {
        theirText = null;
        IsRemoteChangeDialogOpen = false;
    }

    /// <summary>
    /// Discards the user's unsaved edits and takes the synced version; if the file was deleted, closes the editor.
    /// </summary>
    [RelayCommand]
    public void TakeTheirs()
    {
        var theirs = theirText;
        theirText = null;
        IsRemoteChangeDialogOpen = false;
        if (IsRemoteDeleted || theirs == null)
        {
            RequestClose();
            return;
        }

        Show(theirs);
    }

    private async void OnSessionChanged(object? sender, EventArgs e)
    {
        var shown = file;
        if (shown == null || IsSaving)
        {
            // Nothing open, or this editor is what changed it.
            return;
        }

        if (!session.IsUnlocked)
        {
            // Locking forgets everything decrypted, unsaved edits included.
            Clear();
            return;
        }

        try
        {
            await FollowAsync(shown);
        }
        catch (Exception)
        {
            // Event handlers must not throw. The editor is left as it is; the user can still save their version.
        }
    }

    private async Task FollowAsync(VaultFileEntry shown)
    {
        var current = session.Entries.FirstOrDefault(f => f.Kind == VaultFileKind.File && f.Id == shown.Id);
        if (current == null)
        {
            if (isDeleted)
            {
                // Already known, and the user kept their version.
                return;
            }

            if (!IsDirty)
            {
                // Deleted on another device, with nothing to lose here.
                RequestClose();
                return;
            }

            isDeleted = true;
            IsRemoteDeleted = true;
            IsRemoteChangeDialogOpen = true;
            return;
        }

        if (current.LastUpdatedAt == knownUpdatedAt)
        {
            // Something else in the vault changed.
            return;
        }

        var opened = version;
        knownUpdatedAt = current.LastUpdatedAt;
        file = current;
        RaiseFileChanged();
        var theirs = await ReadTextAsync(current.Id);
        if (opened != version || string.Equals(theirs.Text, savedContent, StringComparison.Ordinal))
        {
            // Closed meanwhile, or only its details changed (e.g. it was renamed).
            return;
        }

        if (IsDirty)
        {
            theirText = theirs;
            IsRemoteDeleted = false;
            IsRemoteChangeDialogOpen = true;
        }
        else
        {
            Show(theirs);
        }
    }
}
