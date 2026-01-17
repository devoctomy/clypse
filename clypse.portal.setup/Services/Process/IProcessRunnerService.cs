using System.Diagnostics;

namespace clypse.portal.setup.Services.Process;

/// <summary>
/// Runs external processes and captures their output.
/// </summary>
public interface IProcessRunnerService
{
    /// <summary>
    /// Starts the provided process and waits for completion.
    /// </summary>
    /// <param name="startInfo">Process configuration to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Execution result including success flag, exit code, and captured output streams.</returns>
    public Task<(bool Success, int ExitCode, string OutputStreamText, string ErrorStreamText)> Run(
        ProcessStartInfo startInfo,
        CancellationToken cancellationToken = default);
}
