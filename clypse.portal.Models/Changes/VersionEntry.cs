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

    /// <summary>
    /// Gets or sets the GitHub username of the user who deployed this build.
    /// Optional field. If provided, can be used to link to the user's GitHub profile.
    /// </summary>
    public string? DeployedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this version was deployed.
    /// Optional field.
    /// </summary>
    public string? DeployedAt { get; set; }

    /// <summary>
    /// Gets or sets the URL to the GitHub Actions workflow run that deployed this build.
    /// Optional field.
    /// </summary>
    public string? DeploymentActionUrl { get; set; }
}
