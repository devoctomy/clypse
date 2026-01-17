namespace clypse.portal.setup.Services.Build;

/// <summary>
/// Represents the outcome of a portal build operation.
/// </summary>
public class PortalBuildResult(
    bool success,
    string outputPath)
{
    /// <summary>
    /// Indicates whether the build succeeded.
    /// </summary>
    public bool Success { get; } = success;

    /// <summary>
    /// Gets the path to the wwwroot output when the build succeeds.
    /// </summary>
    public string OutputPath { get; } = outputPath;
}
