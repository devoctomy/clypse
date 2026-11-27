using System.Globalization;
using Blazing.Mvvm.ComponentModel;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Files;
using Devoctomy.Blazwiz.UI.Text.Interfaces;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for a file's details: its name, the folder it is in and its notes can be changed; its type, size and times
/// are shown. Its content is not read.
/// </summary>
public partial class FileDetailsViewModel : ViewModelBase
{
    private readonly IVaultFileSession session;
    private readonly IVaultFileTreeService tree;
    private readonly IFileTypeService fileTypes;
    private readonly IErrorText errorText;

    private VaultFileEntry? file;
    private string name = string.Empty;
    private string folder = string.Empty;
    private string notes = string.Empty;
    private IReadOnlyList<string> folders = [];
    private bool isWorking;
    private string? error;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDetailsViewModel"/> class.
    /// </summary>
    /// <param name="session">The open vault's files, which saves the changes.</param>
    /// <param name="tree">Lists the folders the file can be moved to.</param>
    /// <param name="fileTypes">Says what kind of file it is.</param>
    /// <param name="errorText">Describes errors.</param>
    public FileDetailsViewModel(
        IVaultFileSession session,
        IVaultFileTreeService tree,
        IFileTypeService fileTypes,
        IErrorText errorText)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.tree = tree ?? throw new ArgumentNullException(nameof(tree));
        this.fileTypes = fileTypes ?? throw new ArgumentNullException(nameof(fileTypes));
        this.errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
    }

    /// <summary>
    /// Gets a value indicating whether the file was found.
    /// </summary>
    public bool IsFound => file != null;

    /// <summary>
    /// Gets or sets the file's name.
    /// </summary>
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the path of the folder the file is in, empty for the top level.
    /// </summary>
    public string Folder
    {
        get => folder;
        set => SetProperty(ref folder, tree.Normalize(value));
    }

    /// <summary>
    /// Gets or sets the user's notes on the file.
    /// </summary>
    public string Notes
    {
        get => notes;
        set => SetProperty(ref notes, value ?? string.Empty);
    }

    /// <summary>
    /// Gets the folders the file can be in, in order. The top level, the empty path, is not among them.
    /// </summary>
    public IReadOnlyList<string> Folders
    {
        get => folders;
        private set => SetProperty(ref folders, value);
    }

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
    /// Gets a value indicating whether the changes are being saved.
    /// </summary>
    public bool IsWorking
    {
        get => isWorking;
        private set => SetProperty(ref isWorking, value);
    }

    /// <summary>
    /// Gets why the changes were not saved, or null.
    /// </summary>
    public string? Error
    {
        get => error;
        private set => SetProperty(ref error, value);
    }

    /// <summary>
    /// Shows a file's details.
    /// </summary>
    /// <param name="id">The file's id.</param>
    public void Load(string? id)
    {
        var entries = session.Entries;
        file = entries.FirstOrDefault(e => e.Kind == VaultFileKind.File && e.Id == id);
        Name = file?.Name ?? string.Empty;
        Folders = tree.GetFolders(entries);
        Folder = file?.Folder ?? string.Empty;
        Notes = file?.Notes ?? string.Empty;
        Error = null;
        OnPropertyChanged(nameof(IsFound));
        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(TypeName));
        OnPropertyChanged(nameof(SizeText));
        OnPropertyChanged(nameof(AddedText));
        OnPropertyChanged(nameof(UpdatedText));
    }

    /// <summary>
    /// Saves the file's name, folder and notes.
    /// </summary>
    /// <returns>True if they were saved.</returns>
    public async Task<bool> SaveAsync()
    {
        if (IsWorking || file == null)
        {
            return false;
        }

        IsWorking = true;
        Error = null;
        try
        {
            await session.UpdateFileAsync(file.Id, Name, Folder, Notes);
            return true;
        }
        catch (Exception ex)
        {
            Error = errorText.Describe(ex);
            return false;
        }
        finally
        {
            IsWorking = false;
        }
    }

    private static string Describe(DateTimeOffset? time)
    {
        return time == null ? string.Empty : time.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
    }
}
