namespace clypse.portal.setup.Services.Orchestration;

public interface IClypseAwsSetupOrchestration
{
    public Task SetupClypseOnAwsAsync(CancellationToken cancellationToken);
}
