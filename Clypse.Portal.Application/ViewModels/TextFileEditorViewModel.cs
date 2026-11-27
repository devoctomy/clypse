using System.Security.Cryptography;
using System.Text;
using Blazing.Mvvm.ComponentModel;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Files;
using CommunityToolkit.Mvvm.Input;
using Devoctomy.Blazwiz.UI.Text.Interfaces;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for the text editor: a vault's plain text files and markdown documents are edited in the app. A markdown
/// document has formatting buttons and a live preview; plain text has the text alone. The text is decrypted when the
/// file is opened and held in memory only. It is dropped when the editor closes, and when the vault locks, unsaved
/// edits included: locking forgets everything decrypted.
/// </summary>
/// <remarks>
/// Anything that would lose unsaved edits (closing the editor, showing another tab, leaving the page) is held back
/// while the user is asked whether to save them, discard them or keep editing. When a sync changes the open file, it is
/// read again, or (with unsaved edits) the user is asked which version to keep.
/// </remarks>
public partial class TextFileEditorViewModel : ViewModelBase
{
    /// <summary>
    /// The id of the editor's text area.
    /// </summary>
    public const string EditorElementId = "text-editor";

    /// <summary>
    /// The longest text the editor takes, in megabytes. A larger text file is stored and synced like any other file.
    /// </summary>
    public const int MaximumTextMegabytes = 1;

    /// <summary>
    /// The longest text the editor takes, in bytes.
    /// </summary>
    public const long MaximumTextSize = MaximumTextMegabytes * 1024 * 1024;

    /// <summary>
    /// The formatting buttons of a markdown document's toolbar, in their groups, each with its icon. A button's title
    /// is the text <c>Editor.Format.{format}</c>.
    /// </summary>
    public static readonly IReadOnlyList<IReadOnlyList<(MarkdownFormat Format, string Icon)>> ToolbarGroups =
    [
        [
            (MarkdownFormat.Bold, "bi-type-bold"),
            (MarkdownFormat.Italic, "bi-type-italic"),
            (MarkdownFormat.Strikethrough, "bi-type-strikethrough"),
            (MarkdownFormat.InlineCode, "bi-code"),
        ],
        [
            (MarkdownFormat.Heading1, "bi-type-h1"),
            (MarkdownFormat.Heading2, "bi-type-h2"),
            (MarkdownFormat.Heading3, "bi-type-h3"),
        ],
        [
            (MarkdownFormat.BulletList, "bi-list-ul"),
            (MarkdownFormat.NumberedList, "bi-list-ol"),
            (MarkdownFormat.TaskList, "bi-list-check"),
        ],
        [
            (MarkdownFormat.Quote, "bi-quote"),
            (MarkdownFormat.CodeBlock, "bi-code-square"),
            (MarkdownFormat.Link, "bi-link-45deg"),
            (MarkdownFormat.HorizontalRule, "bi-hr"),
        ],
    ];

    private const char ByteOrderMark = '﻿';

    // Strict, so content that is not text is refused rather than opened, and damaged when it is saved again.
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    private readonly IVaultFileSession session;
    private readonly IFileTypeService fileTypes;
    private readonly IMarkdownRenderer renderer;
    private readonly IMarkdownFormatService formatService;
    private readonly ITextEditorInteropService interop;
    private readonly IUnsavedChangesGuard guard;
    private readonly IErrorText errorText;
    private readonly Func<Action, bool> holder;

    private VaultFileEntry? file;
    private bool isMarkdown;
    private bool isReady;
    private bool isDeleted;
    private bool hasByteOrderMark;
    private bool usesWindowsLineEndings;
    private DateTimeOffset knownUpdatedAt;
    private string content = string.Empty;
    private string savedContent = string.Empty;
    private string previewHtml = string.Empty;
    private string? theirContent;
    private bool isLoading;
    private bool isSaving;
    private bool isPreviewCollapsed;
    private bool isLeaveDialogOpen;
    private bool isRemoteChangeDialogOpen;
    private bool isRemoteDeleted;
    private string? error;
    private DateTimeOffset? lastSavedAt;
    private Action? pendingAction;
    private int version;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextFileEditorViewModel"/> class.
    /// </summary>
    /// <param name="session">The open vault's files, which reads, decrypts and saves the text.</param>
    /// <param name="fileTypes">Says whether a file is plain text or markdown.</param>
    /// <param name="renderer">The markdown renderer, for the preview.</param>
    /// <param name="formatService">Works out the edits made by the formatting buttons.</param>
    /// <param name="interop">Access to the editor's text and selection in the browser.</param>
    /// <param name="guard">Holds back what would lose unsaved edits, e.g. showing another tab.</param>
    /// <param name="errorText">Describes errors.</param>
    public TextFileEditorViewModel(
        IVaultFileSession session,
        IFileTypeService fileTypes,
        IMarkdownRenderer renderer,
        IMarkdownFormatService formatService,
        ITextEditorInteropService interop,
        IUnsavedChangesGuard guard,
        IErrorText errorText)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.fileTypes = fileTypes ?? throw new ArgumentNullException(nameof(fileTypes));
        this.renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        this.formatService = formatService ?? throw new ArgumentNullException(nameof(formatService));
        this.interop = interop ?? throw new ArgumentNullException(nameof(interop));
        this.guard = guard ?? throw new ArgumentNullException(nameof(guard));
        this.errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
        holder = Hold;
        this.session.Changed += OnSessionChanged;
        this.guard.SetHolder(holder);
    }

    /// <summary>
    /// Raised when the editor should close: the user closed it, or the file it was showing is gone.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Gets a value indicating whether the file was found, and is one the editor takes.
    /// </summary>
    public bool IsFound => file != null;

    /// <summary>
    /// Gets the file's name.
    /// </summary>
    public string Name => file?.Name ?? string.Empty;

    /// <summary>
    /// Gets the path of the folder the file is in, empty for the top level.
    /// </summary>
    public string Folder => file?.Folder ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the file is a markdown document, which has formatting buttons and a preview.
    /// </summary>
    public bool IsMarkdown => isMarkdown;

    /// <summary>
    /// Gets a value indicating whether the text has been decrypted and can be edited.
    /// </summary>
    public bool IsReady => isReady;

    /// <summary>
    /// Gets or sets the text being edited. Setting it updates the preview, and counts as using the vault.
    /// </summary>
    public string Content
    {
        get => content;
        set
        {
            if (SetProperty(ref content, value ?? string.Empty))
            {
                RenderPreview();
                OnPropertyChanged(nameof(IsDirty));
                if (isReady)
                {
                    // Typing reads and writes nothing, so the vault would lock itself in the middle of writing.
                    session.KeepUnlocked();
                }
            }
        }
    }

    /// <summary>
    /// Gets the preview of a markdown document as HTML. Raw HTML in the markdown is not passed through, so it is safe
    /// to show.
    /// </summary>
    public string PreviewHtml
    {
        get => previewHtml;
        private set => SetProperty(ref previewHtml, value);
    }

    /// <summary>
    /// Gets a value indicating whether there are unsaved changes.
    /// </summary>
    public bool IsDirty => isReady && (isDeleted || !string.Equals(content, savedContent, StringComparison.Ordinal));

    /// <summary>
    /// Gets a value indicating whether the text is being read and decrypted.
    /// </summary>
    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    /// <summary>
    /// Gets a value indicating whether the text is being saved.
    /// </summary>
    public bool IsSaving
    {
        get => isSaving;
        private set => SetProperty(ref isSaving, value);
    }

    /// <summary>
    /// Gets a value indicating whether the preview pane is hidden.
    /// </summary>
    public bool IsPreviewCollapsed
    {
        get => isPreviewCollapsed;
        private set => SetProperty(ref isPreviewCollapsed, value);
    }

    /// <summary>
    /// Gets a value indicating whether the dialog asking what to do with unsaved changes is showing.
    /// </summary>
    public bool IsLeaveDialogOpen
    {
        get => isLeaveDialogOpen;
        private set => SetProperty(ref isLeaveDialogOpen, value);
    }

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
    /// Gets the last error, e.g. why the file could not be opened or saved.
    /// </summary>
    public string? Error
    {
        get => error;
        private set => SetProperty(ref error, value);
    }

    /// <summary>
    /// Gets when the text was last saved in this editor.
    /// </summary>
    public DateTimeOffset? LastSavedAt
    {
        get => lastSavedAt;
        private set => SetProperty(ref lastSavedAt, value);
    }

    /// <summary>
    /// Opens a file: reads and decrypts its text.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <returns>A <see cref="Task"/> that completes when the text can be edited, or could not be read.</returns>
    public async Task LoadAsync(string? id)
    {
        Clear();
        var opened = version;
        var entry = session.Entries.FirstOrDefault(e => e.Kind == VaultFileKind.File && e.Id == id);
        var kind = entry == null ? FileViewKind.None : fileTypes.GetViewKind(entry);
        if (entry == null || (kind != FileViewKind.Text && kind != FileViewKind.Markdown))
        {
            return;
        }

        file = entry;
        isMarkdown = kind == FileViewKind.Markdown;
        knownUpdatedAt = entry.LastUpdatedAt;
        RaiseFileChanged();
        IsLoading = true;
        try
        {
            // Decrypting cannot be interrupted, and the page cannot respond until it is done: let the editor say it is
            // working first.
            await Task.Yield();
            var text = await ReadTextAsync(entry.Id);
            if (opened != version)
            {
                // Closed, locked or another file opened meanwhile: nothing of this one is kept.
                return;
            }

            // No longer loading before it is ready, so the page never shows both at once.
            IsLoading = false;
            Show(text);
        }
        catch (Exception ex)
        {
            if (opened == version)
            {
                Error = errorText.Describe(ex);
            }
        }
        finally
        {
            if (opened == version)
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>
    /// Saves the text.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task SaveAsync()
    {
        await TrySaveAsync();
    }

    /// <summary>
    /// Applies a format to the selected text of a markdown document, or removes it if it is already applied.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task FormatAsync(MarkdownFormat format)
    {
        if (!isReady || !isMarkdown)
        {
            return;
        }

        var selection = await interop.GetSelectionAsync(EditorElementId);
        if (selection == null)
        {
            return;
        }

        var edit = formatService.Apply(selection.Text, selection.Start, selection.End, format);
        await interop.ApplyEditAsync(EditorElementId, edit);

        // The editor raises an input event which updates Content too; setting it here keeps the view model in step
        // straight away.
        Content = edit.ApplyTo(selection.Text);
    }

    /// <summary>
    /// Shows or hides the preview pane.
    /// </summary>
    [RelayCommand]
    public void TogglePreview()
    {
        IsPreviewCollapsed = !IsPreviewCollapsed;
        RenderPreview();
    }

    /// <summary>
    /// Closes the editor, asking first if there are unsaved changes.
    /// </summary>
    [RelayCommand]
    public void Close()
    {
        if (!Hold(RequestClose))
        {
            RequestClose();
        }
    }

    /// <summary>
    /// Holds back an action that would lose unsaved changes: the user is asked whether to save them, discard them or
    /// keep editing, and the action runs once they have saved or discarded.
    /// </summary>
    /// <param name="action">The action, e.g. showing another tab, or going to another page.</param>
    /// <returns>True if the action is held; false if there are no unsaved changes, so it can go ahead now.</returns>
    public bool Hold(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!IsDirty)
        {
            return false;
        }

        pendingAction = action;
        IsLeaveDialogOpen = true;
        return true;
    }

    /// <summary>
    /// Closes the unsaved changes dialog, and keeps editing: what was held back does not happen.
    /// </summary>
    [RelayCommand]
    public void KeepEditing()
    {
        pendingAction = null;
        IsLeaveDialogOpen = false;
    }

    /// <summary>
    /// Discards the unsaved changes, and carries on with what was held back.
    /// </summary>
    [RelayCommand]
    public void DiscardAndLeave()
    {
        var action = pendingAction;
        Clear();
        action?.Invoke();
    }

    /// <summary>
    /// Saves, then carries on with what was held back. If saving fails, the editor stays open.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task SaveAndLeaveAsync()
    {
        var action = pendingAction;
        pendingAction = null;
        IsLeaveDialogOpen = false;
        if (await TrySaveAsync())
        {
            action?.Invoke();
        }
    }

    /// <summary>
    /// Keeps the user's unsaved edits after a sync changed or deleted the file. Saving writes them over the synced
    /// version; if the file was deleted, saving adds it again.
    /// </summary>
    [RelayCommand]
    public void KeepMine()
    {
        theirContent = null;
        IsRemoteChangeDialogOpen = false;
    }

    /// <summary>
    /// Discards the user's unsaved edits and takes the synced version; if the file was deleted, closes the editor.
    /// </summary>
    [RelayCommand]
    public void TakeTheirs()
    {
        var theirs = theirContent;
        theirContent = null;
        IsRemoteChangeDialogOpen = false;
        if (IsRemoteDeleted || theirs == null)
        {
            RequestClose();
            return;
        }

        Show(theirs);
    }

    /// <summary>
    /// Hides the error.
    /// </summary>
    [RelayCommand]
    public void DismissError()
    {
        Error = null;
    }

    /// <summary>
    /// Drops the file and its text, unsaved edits included.
    /// </summary>
    public void Clear()
    {
        version++;
        file = null;
        isMarkdown = false;
        isReady = false;
        isDeleted = false;
        hasByteOrderMark = false;
        usesWindowsLineEndings = false;
        savedContent = string.Empty;
        theirContent = null;
        pendingAction = null;
        content = string.Empty;
        OnPropertyChanged(nameof(Content));
        PreviewHtml = string.Empty;
        Error = null;
        LastSavedAt = null;
        IsLoading = false;
        IsSaving = false;
        IsLeaveDialogOpen = false;
        IsRemoteChangeDialogOpen = false;
        IsRemoteDeleted = false;
        RaiseFileChanged();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            session.Changed -= OnSessionChanged;
            guard.ClearHolder(holder);
            Clear();
        }

        base.Dispose(disposing);
    }

    // The text without a byte order mark, and with plain line breaks: as the editor holds it.
    private static string Normalize(string text)
    {
        var withoutMark = text.Length > 0 && text[0] == ByteOrderMark ? text[1..] : text;
        return withoutMark.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private void RequestClose()
    {
        Clear();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task<string> ReadTextAsync(string id)
    {
        var bytes = await session.ReadContentAsync(id) ?? throw new ClypseException(ClypseErrorCodes.FileContentMissing);
        try
        {
            if (bytes.LongLength > MaximumTextSize)
            {
                throw new ClypseException(ClypseErrorCodes.TextFileTooLarge, MaximumTextMegabytes);
            }

            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException ex)
        {
            throw new ClypseException(ClypseErrorCodes.TextFileNotText, ex);
        }
        finally
        {
            // Only the text is needed from here on.
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    // Shows a file's text as the editor holds it: without a byte order mark, and with plain line breaks, as a text area
    // gives them. What the file had is remembered, and put back when it is saved.
    private void Show(string fileText)
    {
        hasByteOrderMark = fileText.Length > 0 && fileText[0] == ByteOrderMark;
        usesWindowsLineEndings = fileText.Contains("\r\n", StringComparison.Ordinal);
        var text = Normalize(fileText);
        savedContent = text;
        isDeleted = false;
        isReady = true;
        content = text;
        OnPropertyChanged(nameof(Content));
        OnPropertyChanged(nameof(IsReady));
        OnPropertyChanged(nameof(IsDirty));
        RenderPreview();
    }

    private byte[] Encode(string text)
    {
        if (usesWindowsLineEndings)
        {
            text = text.Replace("\n", "\r\n", StringComparison.Ordinal);
        }

        return Encoding.UTF8.GetBytes(hasByteOrderMark ? ByteOrderMark + text : text);
    }

    private async Task<bool> TrySaveAsync()
    {
        var saving = file;
        if (saving == null || !isReady || IsSaving)
        {
            return false;
        }

        IsSaving = true;
        Error = null;
        var opened = version;
        var text = Content;
        var bytes = Encode(text);
        try
        {
            if (bytes.LongLength > MaximumTextSize)
            {
                throw new ClypseException(ClypseErrorCodes.TextFileTooLarge, MaximumTextMegabytes);
            }

            // Deleted on another device, and the user kept their version: it is added again.
            var saved = isDeleted
                ? await session.AddFileAsync(saving.Name ?? string.Empty, saving.Folder, saving.ContentType, bytes)
                : await session.SaveContentAsync(saving.Id, bytes);
            if (opened != version)
            {
                // Closed or locked while it was saved: it is saved, and nothing of it is kept here.
                return true;
            }

            file = saved;
            isDeleted = false;
            knownUpdatedAt = saved.LastUpdatedAt;
            savedContent = text;
            LastSavedAt = saved.LastUpdatedAt;
            RaiseFileChanged();
            return true;
        }
        catch (Exception ex)
        {
            if (opened == version)
            {
                Error = errorText.Describe(ex);
            }

            return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
            IsSaving = false;
        }
    }

    private void RenderPreview()
    {
        // Only a markdown document has a preview, and it is only worked out while it shows.
        PreviewHtml = isMarkdown && !IsPreviewCollapsed ? renderer.ToHtml(content) : string.Empty;
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
        if (opened != version || string.Equals(Normalize(theirs), savedContent, StringComparison.Ordinal))
        {
            // Closed meanwhile, or only its details changed (e.g. it was renamed).
            return;
        }

        if (IsDirty)
        {
            theirContent = theirs;
            IsRemoteDeleted = false;
            IsRemoteChangeDialogOpen = true;
        }
        else
        {
            Show(theirs);
        }
    }

    private void RaiseFileChanged()
    {
        OnPropertyChanged(nameof(IsFound));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Folder));
        OnPropertyChanged(nameof(IsMarkdown));
        OnPropertyChanged(nameof(IsReady));
        OnPropertyChanged(nameof(IsDirty));
    }
}
