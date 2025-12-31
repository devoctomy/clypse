using clypse.portal.setup.Services.Orchestration;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services;

public class SetupProgram : IProgram
{
    private readonly AwsServiceOptions _options;
    private readonly ISetupInteractiveMenuService _setupInteractiveMenuService;
    private readonly IClypseAwsSetupOrchestration _clypseAwsSetupOrchestration;
    private readonly ILogger<SetupProgram> _logger;

    public SetupProgram(
        AwsServiceOptions options,
        ISetupInteractiveMenuService setupInteractiveMenuService,
        IClypseAwsSetupOrchestration clypseAwsSetupOrchestration,
        ILogger<SetupProgram> logger)
    {
        _options = options;
        _setupInteractiveMenuService = setupInteractiveMenuService;
        _clypseAwsSetupOrchestration = clypseAwsSetupOrchestration;
        _logger = logger;
    }

    public async Task<int> Run()
    {
        try
        {
            if(_options.InteractiveMode)
            {
                var continueSetup = _setupInteractiveMenuService.Run(_options);
                if (!continueSetup)
                {
                    _logger.LogInformation("Setup cancelled by user.");
                    return 0;
                }
            }

            await _clypseAwsSetupOrchestration.SetupClypseOnAwsAsync(CancellationToken.None);

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the setup process.");
            return 1;
        }
    }
}