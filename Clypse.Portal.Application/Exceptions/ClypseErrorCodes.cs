namespace Clypse.Portal.Application.Exceptions;

/// <summary>
/// The codes of <see cref="ClypseException"/>. Each has its text in <c>ClypseText.resx</c>, as <c>Error.{code}</c>.
/// </summary>
public static class ClypseErrorCodes
{
    /// <summary>
    /// A vault was given no name.
    /// </summary>
    public const string VaultNameRequired = "Clypse.VaultNameRequired";

    /// <summary>
    /// The vault no longer exists (e.g. it was deleted on another device).
    /// </summary>
    public const string VaultNotFound = "Clypse.VaultNotFound";

    /// <summary>
    /// A secret is missing required fields. Argument: the fields, comma separated.
    /// </summary>
    public const string SecretInvalid = "Clypse.SecretInvalid";

    /// <summary>
    /// A secret's file could not be read as a secret.
    /// </summary>
    public const string SecretUnreadable = "Clypse.SecretUnreadable";

    /// <summary>
    /// A secret's id is not a GUID, so it cannot name a file.
    /// </summary>
    public const string SecretIdInvalid = "Clypse.SecretIdInvalid";

    /// <summary>
    /// No vault is open.
    /// </summary>
    public const string NoVaultOpen = "Clypse.NoVaultOpen";

    /// <summary>
    /// An import file held no secrets.
    /// </summary>
    public const string ImportNothingFound = "Clypse.ImportNothingFound";

    /// <summary>
    /// An import file could not be read in the format chosen. Argument: the format's name.
    /// </summary>
    public const string ImportUnreadable = "Clypse.ImportUnreadable";

    /// <summary>
    /// A file's id is not a GUID, so it cannot name a file.
    /// </summary>
    public const string FileIdInvalid = "Clypse.FileIdInvalid";

    /// <summary>
    /// A file's metadata could not be read.
    /// </summary>
    public const string FileUnreadable = "Clypse.FileUnreadable";

    /// <summary>
    /// A file was given no name.
    /// </summary>
    public const string FileNameRequired = "Clypse.FileNameRequired";

    /// <summary>
    /// The file no longer exists (e.g. it was deleted on another device).
    /// </summary>
    public const string FileNotFound = "Clypse.FileNotFound";

    /// <summary>
    /// A file is larger than a vault takes. Argument: the limit, in megabytes.
    /// </summary>
    public const string FileTooLarge = "Clypse.FileTooLarge";

    /// <summary>
    /// A file's content is not on this device (e.g. it has not synced here yet).
    /// </summary>
    public const string FileContentMissing = "Clypse.FileContentMissing";

    /// <summary>
    /// A text file's text is longer than can be edited in the app. Argument: the limit, in megabytes.
    /// </summary>
    public const string TextFileTooLarge = "Clypse.TextFileTooLarge";

    /// <summary>
    /// A text file's content is not text the editor can read (it is not UTF-8), so it cannot be edited.
    /// </summary>
    public const string TextFileNotText = "Clypse.TextFileNotText";

    /// <summary>
    /// A folder was given no name, or one with a <c>/</c> in it.
    /// </summary>
    public const string FolderNameInvalid = "Clypse.FolderNameInvalid";

    /// <summary>
    /// There is already a folder with that path.
    /// </summary>
    public const string FolderExists = "Clypse.FolderExists";

    /// <summary>
    /// A folder cannot be moved into itself.
    /// </summary>
    public const string FolderMoveInvalid = "Clypse.FolderMoveInvalid";
}
