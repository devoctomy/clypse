namespace clypse.portal.Models.Changes;

/// <summary>
/// Represents a collection of version change entries for the application.
/// </summary>
public class ChangeLog
{
    /// <summary>
    /// Gets or sets the list of version entries containing changes.
    /// </summary>
    public List<VersionEntry> Versions { get; set; } = [];
}
