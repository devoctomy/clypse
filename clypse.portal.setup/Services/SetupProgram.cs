using clypse.portal.setup.Services.Orchestration;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services;

public class SetupProgram(
    AwsServiceOptions options,
    ISetupInteractiveMenuService setupInteractiveMenuService,
    IClypseAwsSetupOrchestration clypseAwsSetupOrchestration,
    ILogger<SetupProgram> logger) : IProgram
{
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