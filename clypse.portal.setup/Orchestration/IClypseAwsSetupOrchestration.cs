namespace clypse.portal.setup.Orchestration;

public interface IClypseAwsSetupOrchestration
{
    public Task SetupClypseOnAwsAsync(CancellationToken cancellationToken);
}
