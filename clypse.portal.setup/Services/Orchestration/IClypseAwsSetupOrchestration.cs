namespace clypse.portal.setup.Services.Orchestration;

/// <summary>
/// Coordinates the steps required to prepare, install, and upgrade the Clypse portal on AWS.
/// </summary>
public interface IClypseAwsSetupOrchestration
{
    /// <summary>
    /// Validates setup inputs and ensures required AWS resources are available before running setup.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the setup can proceed; otherwise, <see langword="false"/>.</returns>
    public Task<bool> PrepareSetup(CancellationToken cancellationToken);

    /// <summary>
    /// Executes the full AWS provisioning workflow for the Clypse portal.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the workflow completes successfully; otherwise, <see langword="false"/>.</returns>
    public Task<bool> SetupClypseOnAwsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Applies upgrade steps to an existing Clypse portal deployment.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the upgrade completes successfully; otherwise, <see langword="false"/>.</returns>
    public Task<bool> UpgradePortalAsync(CancellationToken cancellationToken);
}
