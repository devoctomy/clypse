namespace clypse.portal.setup.Services.Build;

public class PortalBuildResult(
    bool success,
    string outputPath)
{
    public bool Success { get; } = success;
    public string OutputPath { get; } = outputPath;
}
