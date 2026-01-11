using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace clypse.portal.setup.Services.Process;

[ExcludeFromCodeCoverage]
public class ProcessRunnerService(ILogger<ProcessRunnerService> logger) : IProcessRunnerService
{
    public async Task<(bool Success, int ExitCode, string OutputStreamText, string ErrorStreamText)> Run(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default)
    {
        using var process = new System.Diagnostics.Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                logger.LogError("Failed to start dotnet publish process.");
                return (false, -1, string.Empty, "Failed to start dotnet publish process.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start dotnet publish process.");
            return (false, -1, string.Empty, "Failed to start dotnet publish process.");
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync().ConfigureAwait(false);

        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);

        return (process.ExitCode == 0, process.ExitCode, standardOutput, standardError);
    }
}
