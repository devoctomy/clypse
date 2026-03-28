namespace clypse.portal.Models.Changes;

/// <summary>
/// Represents a single version entry in the change log.
/// </summary>
public class VersionEntry
{
    /// <summary>
    /// Gets or sets the version number.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of changes for this version.
    /// </summary>
    public List<ChangeEntry> Changes { get; set; } = [];
}
