namespace clypse.portal.setup.Services;

/// <summary>
/// Entry point abstraction for running the setup workflow.
/// </summary>
public interface IProgram
{
    /// <summary>
    /// Runs the setup process.
    /// </summary>
    /// <returns>Exit code representing success or failure.</returns>
    Task<int> Run();
}
