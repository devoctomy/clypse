namespace clypse.portal.setup.Services.Build;

/// <summary>
/// Executes the portal build workflow.
/// </summary>
public interface IPortalBuildService
{
    /// <summary>
    /// Builds the portal WebAssembly project and returns the output location.
    /// </summary>
    /// <returns>The build result including success state and output path.</returns>
    public Task<PortalBuildResult> Run();
}
