namespace clypse.portal.Models.Settings;

/// <summary>
/// Represents optional deployment metadata for the current portal deployment.
/// </summary>
public class DeploymentSettings
{
    /// <summary>
    /// Gets or sets the GitHub username of the user who deployed this build.
    /// Optional field. When present, links to the user's GitHub profile.
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

    /// <summary>
    /// Gets a value indicating whether any deployment metadata is available.
    /// </summary>
    public bool HasAnyData =>
        !string.IsNullOrWhiteSpace(DeployedBy) ||
        !string.IsNullOrWhiteSpace(DeployedAt) ||
        !string.IsNullOrWhiteSpace(DeploymentActionUrl);
}
