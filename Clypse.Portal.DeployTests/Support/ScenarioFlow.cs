namespace Clypse.Portal.DeployTests.Support;

/// <summary>
/// Lets a scenario end early and pass, when what it finds means there is nothing more for it to do (e.g. the test vault
/// does not exist yet on a first run). Once it has ended, every later step returns at once: each step checks
/// <see cref="HasEnded"/> first. Reqnroll makes one for each scenario.
/// </summary>
/// <param name="output">Writes why the scenario ended to the test's output.</param>
public sealed class ScenarioFlow(IReqnrollOutputHelper output)
{
    private readonly IReqnrollOutputHelper output = output ?? throw new ArgumentNullException(nameof(output));

    /// <summary>
    /// Gets a value indicating whether the scenario has ended early: its remaining steps do nothing.
    /// </summary>
    public bool HasEnded { get; private set; }

    /// <summary>
    /// Ends the scenario here, as passed.
    /// </summary>
    /// <param name="reason">Why, written to the test's output.</param>
    public void End(string reason)
    {
        HasEnded = true;
        output.WriteLine($"The scenario ends here, passed: {reason}");
    }
}
