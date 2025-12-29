namespace clypse.portal.setup.Services.Orchestration;

public interface IClypseAwsSetupVerifier
{
    public Task VerifyClypseOnAwsAsync(CancellationToken cancellationToken);
}
