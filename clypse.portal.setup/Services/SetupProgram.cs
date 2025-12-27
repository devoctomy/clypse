using clypse.portal.setup.Services.CommandLineParser;
using clypse.portal.setup.Services.Orchestration;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services;

public class SetupProgram : IProgram
{
    private readonly IClypseAwsSetupOrchestration _clypseAwsSetupOrchestration;
    private readonly ICommandLineArgumentsService _commandLineArgumentService;
    private readonly ICommandLineParserService _commandLineParserService;
    private readonly ILogger<SetupProgram> _logger;

    public Func<string> GetCommandLine { get; set; }

    public SetupProgram(
        IClypseAwsSetupOrchestration clypseAwsSetupOrchestration,
        ICommandLineArgumentsService commandLineArgumentService,
        ICommandLineParserService commandLineParserService,
        ILogger<SetupProgram> logger)
    {
        _clypseAwsSetupOrchestration = clypseAwsSetupOrchestration;
        _commandLineArgumentService = commandLineArgumentService;
        _commandLineParserService = commandLineParserService;
        _logger = logger;
        GetCommandLine = DefaultGetCommandLine;
    }

    public async Task<int> Run()
    {
        var arguments = _commandLineArgumentService.GetArguments(GetCommandLine());

        try
        {
            await _clypseAwsSetupOrchestration.SetupClypseOnAwsAsync(CancellationToken.None);

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the setup process.");
            return -1;
        }
    }

    private string DefaultGetCommandLine()
    {
        return Environment.CommandLine;
    }
}