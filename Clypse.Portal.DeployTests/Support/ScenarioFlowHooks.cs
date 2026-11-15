namespace Clypse.Portal.DeployTests.Support;

/// <summary>
/// Notes in the test's output each step that is not run because the scenario has ended early.
/// </summary>
/// <param name="flow">Says whether the scenario has ended.</param>
/// <param name="output">The test's output.</param>
[Binding]
public sealed class ScenarioFlowHooks(ScenarioFlow flow, IReqnrollOutputHelper output)
{
    private readonly ScenarioFlow flow = flow ?? throw new ArgumentNullException(nameof(flow));
    private readonly IReqnrollOutputHelper output = output ?? throw new ArgumentNullException(nameof(output));

    /// <summary>
    /// Writes that the step is not run, once the scenario has ended.
    /// </summary>
    [BeforeStep]
    public void NoteStepNotRun()
    {
        if (flow.HasEnded)
        {
            output.WriteLine("(not run: the scenario has ended)");
        }
    }
}
