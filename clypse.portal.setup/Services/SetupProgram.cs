using clypse.portal.setup.Services.Build;
using clypse.portal.setup.Services.Orchestration;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services;

/// <inheritdoc cref="IProgram" />
public class SetupProgram(
    SetupOptions options,
    ISetupInteractiveMenuService setupInteractiveMenuService,
    IClypseAwsSetupOrchestration clypseAwsSetupOrchestration,
    IPortalBuildService portalBuildService,
    ILogger<SetupProgram> logger) : IProgram
{
    /// <inheritdoc />
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var mode =
                options.EnableUpgradeMode ?
                Enums.SetupMode.Upgrade :
                Enums.SetupMode.FullCreate;
            if (options.InteractiveMode)
            {
                mode = setupInteractiveMenuService.Run(options);
                if (mode == Enums.SetupMode.None)
                {
                    logger.LogInformation("Setup cancelled by user.");
                    return 0;
                }
            }

            // Build portal if in non-interactive mode, upgrading, and BuildPortal is enabled
            if (!options.InteractiveMode && mode == Enums.SetupMode.Upgrade && options.BuildPortal)
            {
                logger.LogInformation("BuildPortal is enabled. Building portal before upgrade.");
                var buildResult = await portalBuildService.Run();
                if (!buildResult.Success)
                {
                    logger.LogError("Portal build failed. Aborting upgrade.");
                    return 1;
                }
                logger.LogInformation("Portal build succeeded. Output: {outputPath}", buildResult.OutputPath);
                
                // Update the portal build output path with the build result
                options.PortalBuildOutputPath = buildResult.OutputPath;
            }

            ////var prepared = await clypseAwsSetupOrchestration.PrepareSetup(CancellationToken.None);
            ////if(!prepared)
            ////{
            ////    logger.LogError("Setup preparation failed. Exiting.");
            ////    return 1;
            ////}

            switch(mode)
            {
                case Enums.SetupMode.FullCreate:
                    await clypseAwsSetupOrchestration.SetupClypseOnAwsAsync(CancellationToken.None);
                    break;

                case Enums.SetupMode.Upgrade:
                    await clypseAwsSetupOrchestration.UpgradePortalAsync(CancellationToken.None);
                    break;
            }

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