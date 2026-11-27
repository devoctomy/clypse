namespace Clypse.Portal.Models.Files;

/// <summary>
/// A folder in a vault's files, as the page shows it.
/// </summary>
public class VaultFolder
{
    /// <summary>
    /// Gets the folder's name: the end of its path.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the folder's path: its names joined by <c>/</c>.
    /// </summary>
    public string Path { get; init; } = string.Empty;
}
