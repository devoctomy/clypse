using Blazing.Mvvm.ComponentModel;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Devoctomy.Blazwiz.UI.Text.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for the dialog that asks for a name: a new folder's, or a folder's new one.
/// </summary>
public partial class FileNameViewModel : ViewModelBase
{
    private readonly IVaultFileSession session;
    private readonly IVaultFileTreeService tree;
    private readonly IErrorText errorText;
    private readonly IStringLocalizer<ClypseText> text;

    private FileNameMode mode;
    private string folder = string.Empty;
    private string name = string.Empty;
    private bool isWorking;
    private string? error;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileNameViewModel"/> class.
    /// </summary>
    /// <param name="session">The open vault's files, which makes and renames the folder.</param>
    /// <param name="tree">Checks the name, and works out the folder's path.</param>
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
    public string Title => Mode == FileNameMode.RenameFolder ? text["Files.Name.RenameFolder.Title"] : text["Files.Name.NewFolder.Title"];

    /// <summary>
    /// Gets what the button that saves says.
    /// </summary>
    public string SaveText => Mode == FileNameMode.RenameFolder ? text["Files.Name.Rename"] : text["Files.Name.Create"];

    /// <summary>
    /// Gets a value indicating whether the folder is being made or renamed.
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
    /// Sets what the dialog is for.
    /// </summary>
    /// <param name="forMode">What it is naming.</param>
    /// <param name="forFolder">For a new folder, the path of the folder it goes in. For a rename, the folder's path.</param>
    public void Load(FileNameMode forMode, string? forFolder)
    {
        Mode = forMode;
        folder = tree.Normalize(forFolder);
        Name = forMode == FileNameMode.RenameFolder ? tree.GetName(folder) : string.Empty;
        Error = null;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SaveText));
    }

    /// <summary>
    /// Makes the folder, or renames it.
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
            if (!tree.IsValidName(Name))
            {
                throw new ClypseException(ClypseErrorCodes.FolderNameInvalid);
            }

            if (Mode == FileNameMode.RenameFolder)
            {
                await session.MoveFolderAsync(folder, tree.Combine(tree.GetParent(folder), Name));
            }
            else
            {
                await session.CreateFolderAsync(tree.Combine(folder, Name));
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
}
