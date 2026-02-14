namespace clypse.portal.Models.Changes;

/// <summary>
/// Represents a single change entry within a version.
/// </summary>
public class ChangeEntry
{
    /// <summary>
    /// Gets or sets the type of change (e.g., "Feature", "Bug Fix", "Enhancement").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the change.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
