using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using Blazing.Mvvm.ComponentModel;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Files;
using CommunityToolkit.Mvvm.Input;
using Devoctomy.Blazwiz.UI.Sync.ViewModels;
using Devoctomy.Blazwiz.UI.Text.Interfaces;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for the file viewer. Opening a file reads and decrypts its content into memory, and an image is shown from
/// a <c>data:</c> URI: nothing decrypted is written to the browser's storage. Nothing is kept, either: each time a file
/// is opened it is decrypted again, and what was decrypted is dropped when the viewer closes or the vault locks. A file
/// of a kind that cannot be shown has only its details shown, and its content is not read at all. An image's past
/// versions are in <see cref="History"/>: one picked there is shown in place of the image, until the user goes back to
/// the current version or restores it.
/// </summary>
public partial class FileViewerViewModel : ViewModelBase, IFileHistoryHost
{
    private readonly IVaultFileSession session;
    private readonly IFileTypeService fileTypes;
    private readonly IErrorText errorText;

    private VaultFileEntry? file;
    private FileViewKind kind;
    private bool isLoading;
    private string? imageSource;
    private string? error;
    private bool isFullScreen;
    private int version;
    private string? versionImageSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileViewerViewModel"/> class.
    /// </summary>
    /// <param name="session">The open vault's files, which reads and decrypts the content.</param>
    /// <param name="fileTypes">Says what kind of file it is, and so how it is shown.</param>
    /// <param name="errorText">Describes errors.</param>
    /// <param name="history">The image's past versions.</param>
    public FileViewerViewModel(IVaultFileSession session, IFileTypeService fileTypes, IErrorText errorText, FileHistoryViewModel history)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.fileTypes = fileTypes ?? throw new ArgumentNullException(nameof(fileTypes));
        this.errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
        History = history ?? throw new ArgumentNullException(nameof(history));
        this.session.Changed += OnSessionChanged;
        History.PropertyChanged += OnHistoryChanged;
        History.ShownChanged += OnShownVersionChanged;
    }

    /// <summary>
    /// Gets the image's past versions: the History dropdown, and the bar above a past version shown.
    /// </summary>
    public FileHistoryViewModel History { get; }

    /// <summary>
    /// Gets a value indicating whether a past version of the image is shown in place of the current one.
    /// </summary>
    public bool IsViewingVersion => History.IsViewingVersion;

    /// <inheritdoc/>
    /// <remarks>The viewer changes nothing, so it never has unsaved changes.</remarks>
    public bool HasUnsavedChanges => false;

    /// <summary>
    /// Gets a value indicating whether the file was found.
    /// </summary>
    public bool IsFound => file != null;

    /// <summary>
    /// Gets the id of the file being shown, or null.
    /// </summary>
    public string? FileId => file?.Id;

    /// <summary>
    /// Gets the file's name.
    /// </summary>
    public string Name => file?.Name ?? string.Empty;

    /// <summary>
    /// Gets the path of the folder the file is in, empty for the top level.
    /// </summary>
    public string Folder => file?.Folder ?? string.Empty;

    /// <summary>
    /// Gets the user's notes on the file, or null if it has none.
    /// </summary>
    public string? Notes => string.IsNullOrWhiteSpace(file?.Notes) ? null : file.Notes;

    /// <summary>
    /// Gets how the file is shown.
    /// </summary>
    public FileViewKind Kind => kind;

    /// <summary>
    /// Gets the icon of the file's type.
    /// </summary>
    public string Icon => file == null ? "bi-file-earmark" : fileTypes.GetIcon(file);

    /// <summary>
    /// Gets what the file's type is called.
    /// </summary>
    public string TypeName => file == null ? string.Empty : fileTypes.GetTypeName(file);

    /// <summary>
    /// Gets the size of the file's content, as text.
    /// </summary>
    public string SizeText => file == null ? string.Empty : fileTypes.FormatSize(file.Size ?? 0);

    /// <summary>
    /// Gets when the file was added, in the user's time and culture.
    /// </summary>
    public string AddedText => Describe(file?.CreatedAt);

    /// <summary>
    /// Gets when the file last changed, in the user's time and culture.
    /// </summary>
    public string UpdatedText => Describe(file?.LastUpdatedAt);

    /// <summary>
    /// Gets a value indicating whether the file's content is being read and decrypted.
    /// </summary>
    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    /// <summary>
    /// Gets the image as a <c>data:</c> URI, for an <c>&lt;img&gt;</c>, or null when the file is not an image or has
    /// not been decrypted. It is held in memory only.
    /// </summary>
    public string? ImageSource
    {
        get => imageSource;
        private set
        {
            if (SetProperty(ref imageSource, value))
            {
                OnPropertyChanged(nameof(ShownImageSource));
            }
        }
    }

    /// <summary>
    /// Gets the image shown, as a <c>data:</c> URI: the current one, or a past version while one is shown. It is held
    /// in memory only.
    /// </summary>
    public string? ShownImageSource => IsViewingVersion ? versionImageSource : ImageSource;

    /// <summary>
    /// Gets a value indicating whether the image fills the whole page: centred, and scaled so all of it shows (made
    /// larger if it is small). Otherwise it is shown above the file's details.
    /// </summary>
    public bool IsFullScreen
    {
        get => isFullScreen;
        private set => SetProperty(ref isFullScreen, value);
    }

    /// <summary>
    /// Gets why the file could not be shown (e.g. its content is not on this device yet), or null.
    /// </summary>
    public string? Error
    {
        get => error;
        private set => SetProperty(ref error, value);
    }

    /// <summary>
    /// Gets a value indicating whether the file is of a kind that cannot be shown in the app: only its details are.
    /// </summary>
    public bool IsNotViewable => file != null && kind != FileViewKind.Image;

    /// <summary>
    /// Opens a file: shows its details and, if it is an image, reads and decrypts it to show.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <returns>A <see cref="Task"/> that completes when the file is shown, or could not be.</returns>
    public async Task LoadAsync(string? id)
    {
        Clear();
        var opened = version;
        file = session.Entries.FirstOrDefault(e => e.Kind == VaultFileKind.File && e.Id == id);
        kind = file == null ? FileViewKind.None : fileTypes.GetViewKind(file);
        RaiseFileChanged();
        if (file == null || kind != FileViewKind.Image)
        {
            return;
        }

        var shown = file;
        IsLoading = true;
        string? source = null;
        string? failure = null;
        try
        {
            // Decrypting cannot be interrupted, and the page cannot respond until it is done: let the viewer say it is
            // working first.
            await Task.Yield();
            source = await ReadImageAsync(shown);
        }
        catch (Exception ex)
        {
            failure = errorText.Describe(ex);
        }

        if (opened != version)
        {
            // Closed, locked or another file opened meanwhile: nothing of this one is kept.
            return;
        }

        ImageSource = source;
        Error = failure;
        IsLoading = false;
        if (source != null && session.Vault is { } vault && session.GetContentKey(shown.Id) is { } key)
        {
            // The image's history is of its content, which is a synced file of the vault's own.
            History.SetFile(vault.Id, key, this);
        }
    }

    /// <inheritdoc/>
    public Task<bool> SaveChangesAsync() => Task.FromResult(true);

    /// <inheritdoc/>
    public void DiscardChanges()
    {
        // Nothing is ever changed here.
    }

    /// <summary>
    /// Shows the image full screen, or goes back to the file's details. Does nothing when no image is showing.
    /// </summary>
    [RelayCommand]
    public void ToggleFullScreen()
    {
        IsFullScreen = ImageSource != null && !IsFullScreen;
    }

    /// <summary>
    /// Drops the file and what was decrypted of it.
    /// </summary>
    public void Clear()
    {
        version++;
        file = null;
        kind = FileViewKind.None;
        IsFullScreen = false;
        ImageSource = null;
        Error = null;
        IsLoading = false;

        // A past version shown is dropped too: nothing decrypted is kept.
        History.BackToCurrent();
        SetVersionImage(null);
        RaiseFileChanged();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            session.Changed -= OnSessionChanged;
            Clear();
            History.PropertyChanged -= OnHistoryChanged;
            History.ShownChanged -= OnShownVersionChanged;
        }

        base.Dispose(disposing);
    }

    private static string Describe(DateTimeOffset? time)
    {
        return time == null ? string.Empty : time.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
    }

    // Reads and decrypts an image, as a data: URI the page can show straight from memory.
    private async Task<string> ReadImageAsync(VaultFileEntry image)
    {
        var content = await session.ReadContentAsync(image.Id) ?? throw new ClypseException(ClypseErrorCodes.FileContentMissing);
        try
        {
            return ToDataUri(image, content);
        }
        finally
        {
            // Only the data: URI is needed from here on.
            CryptographicOperations.ZeroMemory(content);
        }
    }

    private string ToDataUri(VaultFileEntry image, byte[] content)
    {
        return "data:" + fileTypes.GetContentType(image.Name, image.ContentType) + ";base64," + Convert.ToBase64String(content);
    }

    private void SetVersionImage(string? source)
    {
        versionImageSource = source;
        OnPropertyChanged(nameof(ShownImageSource));
    }

    private async void OnSessionChanged(object? sender, EventArgs e)
    {
        var shown = file;
        if (shown == null)
        {
            return;
        }

        var current = session.Entries.FirstOrDefault(f => f.Kind == VaultFileKind.File && f.Id == shown.Id);
        if (current == null)
        {
            // Deleted (here or on another device), or the vault locked: nothing decrypted is kept.
            Clear();
            return;
        }

        if (ReferenceEquals(current, shown))
        {
            return;
        }

        // Its details changed, and its content may have too: a past version restored, here or on another device.
        file = current;
        RaiseFileChanged();
        if (ImageSource != null && current.LastUpdatedAt != shown.LastUpdatedAt)
        {
            await ReloadImageAsync(current);
        }
    }

    private async Task ReloadImageAsync(VaultFileEntry image)
    {
        var opened = version;
        try
        {
            var source = await ReadImageAsync(image);
            if (opened == version)
            {
                ImageSource = source;
                Error = null;
            }
        }
        catch (Exception ex)
        {
            // Event handlers must not throw: the image shown stays, and the viewer says why it could not be read.
            if (opened == version)
            {
                Error = errorText.Describe(ex);
            }
        }
    }

    // The page shows the history's dropdown and bar, so it is shown again whenever they change.
    private void OnHistoryChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(History));
        if (e.PropertyName == nameof(FileHistoryViewModel.IsViewingVersion))
        {
            OnPropertyChanged(nameof(IsViewingVersion));
            OnPropertyChanged(nameof(ShownImageSource));
        }
    }

    // A past version is shown from its content, which the history holds (to restore it): it is not wiped here.
    private void OnShownVersionChanged(object? sender, EventArgs e)
    {
        SetVersionImage(History.SelectedContent is { } shown && file != null ? ToDataUri(file, shown.Content) : null);
    }

    private void RaiseFileChanged()
    {
        OnPropertyChanged(nameof(IsFound));
        OnPropertyChanged(nameof(FileId));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Folder));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(Kind));
        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(TypeName));
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(AddedText));
        OnPropertyChanged(nameof(UpdatedText));
        OnPropertyChanged(nameof(IsNotViewable));
    }
}
