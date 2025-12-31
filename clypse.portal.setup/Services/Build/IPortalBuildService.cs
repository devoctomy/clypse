namespace clypse.portal.setup.Services.Build;

public interface IPortalBuildService
{
    public Task<PortalBuildResult> Run();
}
