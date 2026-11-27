namespace Clypse.Portal.Models.Files;

/// <summary>
/// One folder of a vault's files, as the page shows it: where it is, and what is in it.
/// </summary>
public class VaultFolderView
{
    /// <summary>
    /// Gets the folder's path, empty for the top level.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Gets the folders from the top level down to this one, outermost first. Empty for the top level.
    /// </summary>
    public IReadOnlyList<VaultFolder> Breadcrumb { get; init; } = [];

    /// <summary>
    /// Gets the folders in this folder, by name.
    /// </summary>
    public IReadOnlyList<VaultFolder> Folders { get; init; } = [];

    /// <summary>
    /// Gets the files in this folder, by name.
    /// </summary>
    public IReadOnlyList<VaultFileEntry> Files { get; init; } = [];
}
