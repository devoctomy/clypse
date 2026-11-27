using System.Globalization;
using Blazing.Mvvm.ComponentModel;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Files;
using Clypse.Portal.Models.Settings;
using CommunityToolkit.Mvvm.Input;
using Devoctomy.Blazwiz.Core.Text.Interfaces;
using Devoctomy.Blazwiz.UI.Text.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for the Files tab of a vault's page: the open folder and what is in it, search across every folder, adding
/// files, the New menu, and which dialog is open. It works from the metadata in the <see cref="IVaultFileSession"/>;
/// no file's content is read to show the list. A file's content is only read, and decrypted, when it is opened in the
/// viewer (<see cref="FileViewerViewModel"/>).
/// </summary>
public partial class VaultFilesViewModel : ViewModelBase
{
    /// <summary>
    /// How many files can be added in one go.
    /// </summary>
    public const int MaximumFilesAtOnce = 20;

    private const long BytesPerMegabyte = 1024 * 1024;

    private readonly IVaultFileSession session;
    private readonly IVaultFileTreeService tree;
    private readonly IFileTypeService fileTypes;
    private readonly ClypseSettings settings;
    private readonly IErrorText errorText;
    private readonly ITextPlurals plurals;
    private readonly IStringLocalizer<ClypseText> text;

    private string folder = string.Empty;
    private string search = string.Empty;
    private VaultFolderView view = new VaultFolderView();
    private IReadOnlyList<VaultFileEntry> results = [];
    private FilesDialog activeDialog;
    private string? dialogFolder;
    private string? dialogFileId;
    private string? editingFileId;
    private bool isNewMenuOpen;
    private bool isWorking;
    private string? workingText;
    private string? errorMessage;
    private IReadOnlyList<string> addErrors = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultFilesViewModel"/> class.
    /// </summary>
    /// <param name="session">Holds the metadata of the open vault's files.</param>
    /// <param name="tree">Works out the folders from the metadata.</param>
    /// <param name="fileTypes">Says what kind of file each is.</param>
    /// <param name="settings">Clypse's settings, for the largest file a vault takes.</param>
    /// <param name="errorText">Describes errors.</param>
    /// <param name="plurals">Words counts.</param>
    /// <param name="text">Clypse's text.</param>
    public VaultFilesViewModel(
        IVaultFileSession session,
        IVaultFileTreeService tree,
        IFileTypeService fileTypes,
        ClypseSettings settings,
        IErrorText errorText,
        ITextPlurals plurals,
        IStringLocalizer<ClypseText> text)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.tree = tree ?? throw new ArgumentNullException(nameof(tree));
        this.fileTypes = fileTypes ?? throw new ArgumentNullException(nameof(fileTypes));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
        this.plurals = plurals ?? throw new ArgumentNullException(nameof(plurals));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
        this.session.Changed += OnSessionChanged;
        Refresh();
    }

    /// <summary>
    /// The dialogs the tab can show.
    /// </summary>
    public enum FilesDialog
    {
        /// <summary>
        /// No dialog.
        /// </summary>
        None,

        /// <summary>
        /// Naming a new folder, made in <see cref="DialogFolder"/>.
        /// </summary>
        NewFolder,

        /// <summary>
        /// Naming a new markdown document, made in <see cref="DialogFolder"/>.
        /// </summary>
        NewMarkdown,

        /// <summary>
        /// Naming a new text file, made in <see cref="DialogFolder"/>.
        /// </summary>
        NewText,

        /// <summary>
        /// Renaming the folder <see cref="DialogFolder"/>.
        /// </summary>
        RenameFolder,

        /// <summary>
        /// The viewer, showing the file <see cref="DialogFileId"/>.
        /// </summary>
        Viewer,

        /// <summary>
        /// The details of the file <see cref="DialogFileId"/>: its name, folder and notes.
        /// </summary>
        Details,

        /// <summary>
        /// Asking whether to delete the file <see cref="DialogFileId"/>.
        /// </summary>
        DeleteFile,

        /// <summary>
        /// Asking whether to delete the folder <see cref="DialogFolder"/> and what is in it.
        /// </summary>
        DeleteFolder,
    }

    /// <summary>
    /// Gets the open vault's name, the start of the breadcrumb.
    /// </summary>
    public string VaultName => session.Vault?.Name ?? string.Empty;

    /// <summary>
    /// Gets the path of the open folder, empty for the top level.
    /// </summary>
    public string Folder => folder;

    /// <summary>
    /// Gets the folders from the top level down to the open one, outermost first.
    /// </summary>
    public IReadOnlyList<VaultFolder> Breadcrumb => view.Breadcrumb;

    /// <summary>
    /// Gets the folders in the open folder, by name. None while searching.
    /// </summary>
    public IReadOnlyList<VaultFolder> Folders => IsSearching ? [] : view.Folders;

    /// <summary>
    /// Gets the files to list, by name: those in the open folder, or while searching, those in any folder that match.
    /// </summary>
    public IReadOnlyList<VaultFileEntry> Files => IsSearching ? results : view.Files;

    /// <summary>
    /// Gets or sets the search text. Files are searched in every folder, by name and notes.
    /// </summary>
    public string Search
    {
        get => search;
        set
        {
            if (SetProperty(ref search, value ?? string.Empty))
            {
                Refresh();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the list shows search results.
    /// </summary>
    public bool IsSearching => Search.Trim().Length > 0;

    /// <summary>
    /// Gets a value indicating whether the vault has no files and no folders.
    /// </summary>
    public bool IsEmpty => session.Entries.Count == 0;

    /// <summary>
    /// Gets a value indicating whether the files' metadata is being read.
    /// </summary>
    public bool IsLoading => session.IsLoading;

    /// <summary>
    /// Gets how far through reading the metadata is, from 0 to 100.
    /// </summary>
    public int LoadingPercent => session.TotalCount == 0 ? 0 : session.LoadedCount * 100 / session.TotalCount;

    /// <summary>
    /// Gets what reading the metadata is doing, e.g. "Decrypting 2 of 5 files...".
    /// </summary>
    public string LoadingText => plurals.Format(text, "Files.Loading", session.TotalCount, session.LoadedCount);

    /// <summary>
    /// Gets how many files could not be read, as the warning says it, or null if they all could.
    /// </summary>
    public string? UnreadableText => session.UnreadableCount == 0
        ? null
        : plurals.Format(text, "Files.Unreadable", session.UnreadableCount);

    /// <summary>
    /// Gets the open dialog.
    /// </summary>
    public FilesDialog ActiveDialog
    {
        get => activeDialog;
        private set => SetProperty(ref activeDialog, value);
    }

    /// <summary>
    /// Gets the folder the open dialog is for: where a new folder goes, or the folder being renamed or deleted.
    /// </summary>
    public string? DialogFolder
    {
        get => dialogFolder;
        private set => SetProperty(ref dialogFolder, value);
    }

    /// <summary>
    /// Gets the id of the file the open dialog is for.
    /// </summary>
    public string? DialogFileId
    {
        get => dialogFileId;
        private set => SetProperty(ref dialogFileId, value);
    }

    /// <summary>
    /// Gets the id of the text file open in the editor, which takes the place of the list, or null.
    /// </summary>
    public string? EditingFileId
    {
        get => editingFileId;
        private set => SetProperty(ref editingFileId, value);
    }

    /// <summary>
    /// Gets a value indicating whether the New menu is open.
    /// </summary>
    public bool IsNewMenuOpen
    {
        get => isNewMenuOpen;
        private set => SetProperty(ref isNewMenuOpen, value);
    }

    /// <summary>
    /// Gets a value indicating whether files are being added, or something is being deleted.
    /// </summary>
    public bool IsWorking
    {
        get => isWorking;
        private set => SetProperty(ref isWorking, value);
    }

    /// <summary>
    /// Gets what is being done, e.g. "Adding 2 of 5: Passport.jpg", or null.
    /// </summary>
    public string? WorkingText
    {
        get => workingText;
        private set => SetProperty(ref workingText, value);
    }

    /// <summary>
    /// Gets an error to show, or null.
    /// </summary>
    public string? ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    /// <summary>
    /// Gets why each file that could not be added was not, one line per file.
    /// </summary>
    public IReadOnlyList<string> AddErrors
    {
        get => addErrors;
        private set => SetProperty(ref addErrors, value);
    }

    /// <summary>
    /// Gets the question the delete dialog asks, naming what is to be deleted.
    /// </summary>
    public string DeleteQuestion
    {
        get
        {
            if (ActiveDialog == FilesDialog.DeleteFile)
            {
                return text["Files.Delete.File.Confirm", FindFile(DialogFileId)?.Name ?? string.Empty];
            }

            var name = tree.GetName(DialogFolder);
            var count = tree.GetEntriesUnder(session.Entries, DialogFolder).Count(e => e.Kind == VaultFileKind.File);
            return count == 0
                ? text["Files.Delete.Folder.ConfirmEmpty", name]
                : plurals.Format(text, "Files.Delete.Folder.Confirm", count, name);
        }
    }

    private long MaximumFileSize => settings.MaxFileSizeMegabytes * BytesPerMegabyte;

    /// <summary>
    /// Gets the icon of a file's type.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <returns>The Bootstrap icon's class.</returns>
    public string GetIcon(VaultFileEntry file) => fileTypes.GetIcon(file);

    /// <summary>
    /// Gets the line shown under a file's name: its type, its size and when it was added.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <returns>The line.</returns>
    public string GetSummary(VaultFileEntry file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return text[
            "Files.Row.Summary",
            fileTypes.GetTypeName(file),
            fileTypes.FormatSize(file.Size ?? 0),
            file.CreatedAt.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)];
    }

    /// <summary>
    /// Checks whether a file's content is on this device. During a sync, its metadata can arrive first.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <returns>True if its content is here.</returns>
    public bool HasContent(VaultFileEntry file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return session.HasContent(file.Id);
    }

    /// <summary>
    /// Opens a folder, showing what is in it. Any search is cleared.
    /// </summary>
    /// <param name="path">The folder's path, or null or empty for the top level.</param>
    [RelayCommand]
    public void OpenFolder(string? path)
    {
        folder = tree.Normalize(path);
        search = string.Empty;
        IsNewMenuOpen = false;
        Refresh();
        OnPropertyChanged(nameof(Search));
    }

    /// <summary>
    /// Opens or closes the New menu.
    /// </summary>
    [RelayCommand]
    public void ToggleNewMenu()
    {
        IsNewMenuOpen = !IsNewMenuOpen;
    }

    /// <summary>
    /// Closes the New menu.
    /// </summary>
    [RelayCommand]
    public void CloseNewMenu()
    {
        IsNewMenuOpen = false;
    }

    /// <summary>
    /// Asks for the name of a new folder, to be made in the open folder.
    /// </summary>
    [RelayCommand]
    public void NewFolder()
    {
        ShowDialog(FilesDialog.NewFolder, folder, null);
    }

    /// <summary>
    /// Asks for a folder's new name.
    /// </summary>
    /// <param name="path">The folder's path.</param>
    [RelayCommand]
    public void RenameFolder(string path)
    {
        ShowDialog(FilesDialog.RenameFolder, tree.Normalize(path), null);
    }

    /// <summary>
    /// Asks for the name of a new markdown document, to be made in the open folder.
    /// </summary>
    [RelayCommand]
    public void NewMarkdown()
    {
        ShowDialog(FilesDialog.NewMarkdown, folder, null);
    }

    /// <summary>
    /// Asks for the name of a new text file, to be made in the open folder.
    /// </summary>
    [RelayCommand]
    public void NewText()
    {
        ShowDialog(FilesDialog.NewText, folder, null);
    }

    /// <summary>
    /// Opens a file, which decrypts it: a text file or markdown document in the editor, in place of the list; anything
    /// else in the viewer.
    /// </summary>
    /// <param name="id">The file's id.</param>
    [RelayCommand]
    public void OpenFile(string id)
    {
        if (IsEditable(FindFile(id)))
        {
            ShowDialog(FilesDialog.None, null, null);
            EditingFileId = id;
        }
        else
        {
            ShowDialog(FilesDialog.Viewer, null, id);
        }
    }

    /// <summary>
    /// Shows the list again, in place of the editor.
    /// </summary>
    [RelayCommand]
    public void CloseEditor()
    {
        EditingFileId = null;
    }

    /// <summary>
    /// Opens a file's details.
    /// </summary>
    /// <param name="id">The file's id.</param>
    [RelayCommand]
    public void ShowDetails(string id)
    {
        ShowDialog(FilesDialog.Details, null, id);
    }

    /// <summary>
    /// Asks whether to delete a file.
    /// </summary>
    /// <param name="id">The file's id.</param>
    [RelayCommand]
    public void AskDeleteFile(string id)
    {
        ShowDialog(FilesDialog.DeleteFile, null, id);
    }

    /// <summary>
    /// Asks whether to delete a folder and what is in it.
    /// </summary>
    /// <param name="path">The folder's path.</param>
    [RelayCommand]
    public void AskDeleteFolder(string path)
    {
        ShowDialog(FilesDialog.DeleteFolder, tree.Normalize(path), null);
    }

    /// <summary>
    /// Closes the open dialog.
    /// </summary>
    [RelayCommand]
    public void CloseDialog()
    {
        ShowDialog(FilesDialog.None, null, null);
    }

    /// <summary>
    /// Hides the errors.
    /// </summary>
    [RelayCommand]
    public void DismissError()
    {
        ErrorMessage = null;
        AddErrors = [];
    }

    /// <summary>
    /// Deletes what the delete dialog asked about, and closes it.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ConfirmDeleteAsync()
    {
        var isAsking = ActiveDialog == FilesDialog.DeleteFile || ActiveDialog == FilesDialog.DeleteFolder;
        if (IsWorking || !isAsking)
        {
            return;
        }

        IsWorking = true;
        ErrorMessage = null;
        try
        {
            if (ActiveDialog == FilesDialog.DeleteFile)
            {
                await session.DeleteFileAsync(DialogFileId!);
            }
            else
            {
                await session.DeleteFolderAsync(DialogFolder!);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = errorText.Describe(ex);
        }
        finally
        {
            IsWorking = false;
            CloseDialog();
        }
    }

    /// <summary>
    /// Adds files to the open folder, one at a time. A file over the limit is not read. Those that could not be added
    /// are listed in <see cref="AddErrors"/>, and the rest are added all the same.
    /// </summary>
    /// <param name="files">The files the user picked.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task AddFilesAsync(IReadOnlyList<FileToAdd> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        if (IsWorking || files.Count == 0)
        {
            return;
        }

        ErrorMessage = null;
        AddErrors = [];
        if (files.Count > MaximumFilesAtOnce)
        {
            ErrorMessage = text["Files.Add.TooMany", MaximumFilesAtOnce];
            return;
        }

        var failures = new List<string>();
        var target = folder;
        IsWorking = true;
        try
        {
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                WorkingText = text["Files.Adding", i + 1, files.Count, file.Name];
                try
                {
                    await AddFileAsync(file, target);
                }
                catch (Exception ex)
                {
                    failures.Add(text["Files.Add.Failed", file.Name, errorText.Describe(ex)]);
                }
            }
        }
        finally
        {
            WorkingText = null;
            IsWorking = false;
            AddErrors = failures;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            session.Changed -= OnSessionChanged;
        }

        base.Dispose(disposing);
    }

    // Ordinal comparisons, as the secrets list uses: culture-aware ones are many times slower in WebAssembly.
    private static bool Contains(string? value, string term) =>
        value != null && value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private async Task AddFileAsync(FileToAdd file, string target)
    {
        if (file.Size > MaximumFileSize)
        {
            throw new ClypseException(ClypseErrorCodes.FileTooLarge, settings.MaxFileSizeMegabytes);
        }

        // Encrypting a file cannot be interrupted, and the page cannot respond until it is done: let it show what is
        // being added first.
        await Task.Yield();
        var content = await file.ReadAsync(CancellationToken.None);
        await session.AddFileAsync(file.Name, target, fileTypes.GetContentType(file.Name, file.ContentType), content);
    }

    // Text and markdown open in the editor, up to the size it takes. A longer text file is stored and synced like any
    // other file, and shows its details.
    private bool IsEditable(VaultFileEntry? file)
    {
        if (file == null || (file.Size ?? 0) > TextFileEditorViewModel.MaximumTextSize)
        {
            return false;
        }

        var kind = fileTypes.GetViewKind(file);
        return kind == FileViewKind.Text || kind == FileViewKind.Markdown;
    }

    private VaultFileEntry? FindFile(string? id)
    {
        return id == null ? null : session.Entries.FirstOrDefault(e => e.Kind == VaultFileKind.File && e.Id == id);
    }

    private void ShowDialog(FilesDialog dialog, string? forFolder, string? forFileId)
    {
        IsNewMenuOpen = false;
        DialogFolder = forFolder;
        DialogFileId = forFileId;
        ActiveDialog = dialog;
        OnPropertyChanged(nameof(DeleteQuestion));
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    private void Refresh()
    {
        var entries = session.Entries;

        // The open folder can go (deleted here, or on another device): show the nearest one above it that is left.
        while (folder.Length > 0 && !tree.Exists(entries, folder))
        {
            folder = tree.GetParent(folder);
        }

        view = tree.GetView(entries, folder);
        var term = Search.Trim();
        results = term.Length == 0
            ? []
            : entries
                .Where(e => e.Kind == VaultFileKind.File && (Contains(e.Name, term) || Contains(e.Notes, term)))
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.Id, StringComparer.Ordinal)
                .ToList();

        OnPropertyChanged(nameof(VaultName));
        OnPropertyChanged(nameof(Folder));
        OnPropertyChanged(nameof(Breadcrumb));
        OnPropertyChanged(nameof(Folders));
        OnPropertyChanged(nameof(Files));
        OnPropertyChanged(nameof(IsSearching));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(LoadingPercent));
        OnPropertyChanged(nameof(LoadingText));
        OnPropertyChanged(nameof(UnreadableText));
    }
}
