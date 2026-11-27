namespace Clypse.Portal.Models.Files;

/// <summary>
/// What a metadata file under a vault's <c>files/meta/</c> describes.
/// </summary>
public enum VaultFileKind
{
    /// <summary>
    /// A file, whose content is under <c>files/blobs/</c>.
    /// </summary>
    File,

    /// <summary>
    /// An empty folder. A folder with a file in it needs no record: the file's metadata names it.
    /// </summary>
    Folder,
}
