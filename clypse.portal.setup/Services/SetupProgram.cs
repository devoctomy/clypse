using clypse.portal.setup.Services.Orchestration;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services;

/// <inheritdoc cref="IProgram" />
public class SetupProgram(
    SetupOptions options,
    ISetupInteractiveMenuService setupInteractiveMenuService,
    IClypseAwsSetupOrchestration clypseAwsSetupOrchestration,
    ILogger<SetupProgram> logger) : IProgram
{
    /// <inheritdoc />
    public async Task<int> Run()
    {
        try
        {
            if(options.InteractiveMode)
            {
                var continueSetup = setupInteractiveMenuService.Run(options);
                if (!continueSetup)
                {
                    logger.LogInformation("Setup cancelled by user.");
                    return 0;
                }
            }

            var prepared = await clypseAwsSetupOrchestration.PrepareSetup(CancellationToken.None);
            if(!prepared)
            {
                logger.LogError("Setup preparation failed. Exiting.");
                return 1;
            }

            await clypseAwsSetupOrchestration.SetupClypseOnAwsAsync(CancellationToken.None);

            if (options.InteractiveMode)
            {
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the setup process.");
            return 1;
        }
    }
}