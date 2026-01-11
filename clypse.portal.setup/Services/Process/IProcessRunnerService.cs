using System.Diagnostics;

namespace clypse.portal.setup.Services.Process;

public interface IProcessRunnerService
{
    public Task<(bool Success, int ExitCode, string OutputStreamText, string ErrorStreamText)> Run(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default);
}
