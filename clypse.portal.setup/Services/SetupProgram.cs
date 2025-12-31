using clypse.portal.setup.Services.Orchestration;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services;

public class SetupProgram : IProgram
{
    private readonly AwsServiceOptions _options;
    private readonly IClypseAwsSetupOrchestration _clypseAwsSetupOrchestration;
    private readonly ILogger<SetupProgram> _logger;

    public Func<string> GetCommandLine { get; set; }

    public SetupProgram(
        AwsServiceOptions options,
        IClypseAwsSetupOrchestration clypseAwsSetupOrchestration,
        ILogger<SetupProgram> logger)
    {
        _options = options;
        _clypseAwsSetupOrchestration = clypseAwsSetupOrchestration;
        _logger = logger;
        GetCommandLine = DefaultGetCommandLine;
    }

    public async Task<int> Run()
    {
        try
        {
            if(_options.InteractiveMode)
            {
                // use spectre menu here to setup parameters before continuing
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

    private string DefaultGetCommandLine()
    {
        return Environment.CommandLine;
    }
}