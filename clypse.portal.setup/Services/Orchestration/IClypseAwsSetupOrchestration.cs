namespace clypse.portal.setup.Services.Orchestration;

public interface IClypseAwsSetupOrchestration
{
    public Task<bool> PrepareSetup(CancellationToken cancellationToken);

    public Task<bool> SetupClypseOnAwsAsync(CancellationToken cancellationToken);
}
