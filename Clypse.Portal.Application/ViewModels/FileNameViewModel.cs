using Blazing.Mvvm.ComponentModel;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Devoctomy.Blazwiz.UI.Text.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for the dialog that asks for a name: a new folder's, a new markdown document's or text file's, or a
/// folder's new one. A new document or text file is made empty, to be written in the editor.
/// </summary>
public partial class FileNameViewModel : ViewModelBase
{
    /// <summary>
    /// The extension a new markdown document is given, unless its name already has one.
    /// </summary>
    public const string MarkdownExtension = ".md";

    /// <summary>
    /// The extension a new text file is given, unless its name already has it.
    /// </summary>
    public const string TextExtension = ".txt";

    private static readonly string[] MarkdownExtensions = [MarkdownExtension, ".markdown"];
    private static readonly string[] TextExtensions = [TextExtension];

    private readonly IVaultFileSession session;
    private readonly IVaultFileTreeService tree;
    private readonly IErrorText errorText;
    private readonly IStringLocalizer<ClypseText> text;

    private FileNameMode mode;
    private string folder = string.Empty;
    private string name = string.Empty;
    private bool isWorking;
    private string? error;
    private string? createdFileId;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileNameViewModel"/> class.
    /// </summary>
    /// <param name="session">The open vault's files, which makes the folder or file, or renames the folder.</param>
    /// <param name="tree">Checks a folder's name, and works out its path.</param>
    /// <param name="errorText">Describes errors.</param>
    /// <param name="text">Clypse's text.</param>
    public FileNameViewModel(
        IVaultFileSession session,
        IVaultFileTreeService tree,
        IErrorText errorText,
        IStringLocalizer<ClypseText> text)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.tree = tree ?? throw new ArgumentNullException(nameof(tree));
        this.errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// What the dialog is naming.
    /// </summary>
    public enum FileNameMode
    {
        /// <summary>
        /// A new folder.
        /// </summary>
        NewFolder,

        /// <summary>
        /// A folder that is being renamed.
        /// </summary>
        RenameFolder,

        /// <summary>
        /// A new markdown document.
        /// </summary>
        NewMarkdown,

        /// <summary>
        /// A new text file.
        /// </summary>
        NewText,
    }

    /// <summary>
    /// Gets what the dialog is naming.
    /// </summary>
    public FileNameMode Mode
    {
        get => mode;
        private set => SetProperty(ref mode, value);
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value ?? string.Empty);
    }

    /// <summary>
    /// Gets the dialog's title.
    /// </summary>
    public string Title => text["Files.Name." + Mode + ".Title"];

    /// <summary>
    /// Gets what the name is called: a folder's name, or a file's.
    /// </summary>
    public string Label => IsFolder ? text["Files.Name.Folder.Label"] : text["Files.Name.File.Label"];

    /// <summary>
    /// Gets the icon of what is being named.
    /// </summary>
    public string Icon => Mode switch
    {
        FileNameMode.NewMarkdown => "bi-file-earmark-richtext",
        FileNameMode.NewText => "bi-file-earmark-text",
        _ => "bi-folder",
    };

    /// <summary>
    /// Gets what the button that saves says.
    /// </summary>
    public string SaveText => Mode == FileNameMode.RenameFolder ? text["Files.Name.Rename"] : text["Files.Name.Create"];

    /// <summary>
    /// Gets a value indicating whether it is being made or renamed.
    /// </summary>
    public bool IsWorking
    {
        get => isWorking;
        private set => SetProperty(ref isWorking, value);
    }

    /// <summary>
    /// Gets why it was not made or renamed, or null.
    /// </summary>
    public string? Error
    {
        get => error;
        private set => SetProperty(ref error, value);
    }

    /// <summary>
    /// Gets the id of the markdown document or text file that was made, to open in the editor, or null.
    /// </summary>
    public string? CreatedFileId
    {
        get => createdFileId;
        private set => SetProperty(ref createdFileId, value);
    }

    private bool IsFolder => Mode == FileNameMode.NewFolder || Mode == FileNameMode.RenameFolder;

    /// <summary>
    /// Sets what the dialog is for.
    /// </summary>
    /// <param name="forMode">What it is naming.</param>
    /// <param name="forFolder">For something new, the path of the folder it goes in. For a rename, the folder's path.</param>
    public void Load(FileNameMode forMode, string? forFolder)
    {
        Mode = forMode;
        folder = tree.Normalize(forFolder);
        Name = forMode == FileNameMode.RenameFolder ? tree.GetName(folder) : string.Empty;
        Error = null;
        CreatedFileId = null;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(SaveText));
    }

    /// <summary>
    /// Makes the folder, document or text file, or renames the folder.
    /// </summary>
    /// <returns>True if it was done.</returns>
    public async Task<bool> SaveAsync()
    {
        if (IsWorking)
        {
            return false;
        }

        IsWorking = true;
        Error = null;
        try
        {
            switch (Mode)
            {
                case FileNameMode.RenameFolder:
                    await session.MoveFolderAsync(folder, tree.Combine(tree.GetParent(folder), FolderName()));
                    break;
                case FileNameMode.NewMarkdown:
                    CreatedFileId = await AddEmptyFileAsync(MarkdownExtensions, FileTypeService.MarkdownType);
                    break;
                case FileNameMode.NewText:
                    CreatedFileId = await AddEmptyFileAsync(TextExtensions, FileTypeService.TextType);
                    break;
                default:
                    await session.CreateFolderAsync(tree.Combine(folder, FolderName()));
                    break;
            }

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

    private string FolderName()
    {
        return tree.IsValidName(Name) ? Name : throw new ClypseException(ClypseErrorCodes.FolderNameInvalid);
    }

    // The name as given, with the kind's extension added unless it already ends with one of them.
    private async Task<string> AddEmptyFileAsync(string[] extensions, string contentType)
    {
        var fileName = Name.Trim();
        if (fileName.Length == 0)
        {
            throw new ClypseException(ClypseErrorCodes.FileNameRequired);
        }

        if (!extensions.Any(e => fileName.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
        {
            fileName += extensions[0];
        }

        return (await session.AddFileAsync(fileName, folder, contentType, [])).Id;
    }
}
