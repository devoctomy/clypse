using Clypse.Portal.DeployTests.Support;
using static Microsoft.Playwright.Assertions;

namespace Clypse.Portal.DeployTests.Steps;

/// <summary>
/// Steps for importing secrets into the open vault from a CSV file in TestData, and for what the open vault holds.
/// </summary>
/// <param name="session">The scenario's browser.</param>
/// <param name="flow">Says whether the scenario has ended early.</param>
[Binding]
public sealed class ImportSteps(BrowserSession session, ScenarioFlow flow)
{
    private readonly BrowserSession session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly ScenarioFlow flow = flow ?? throw new ArgumentNullException(nameof(flow));

    private IPage Page => session.Page;

    /// <summary>
    /// Imports a KeePass 1.x CSV file from TestData into the open vault, and waits for the import to finish.
    /// </summary>
    /// <param name="fileName">The file's name in TestData.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I import the KeePass file {string}")]
    public async Task WhenIImportTheKeePassFile(string fileName)
    {
        if (flow.HasEnded)
        {
            return;
        }

        var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"There is no test file '{fileName}' in TestData.", path);
        }

        await Page.Locator("#import-button").ClickAsync();
        await Expect(Page.Locator("#import-dialog")).ToBeVisibleAsync();
        await Page.Locator("#import-format").SelectOptionAsync("KeePassCsv1_x");
        await Page.Locator("#import-file").SetInputFilesAsync(path);
        await Expect(Page.Locator("#import-found")).ToBeVisibleAsync();
        await Page.Locator("#import-confirm").ClickAsync();
        await Expect(Page.Locator("#import-done, #import-error").First).ToBeVisibleAsync(new() { Timeout = BrowserSession.SyncTimeoutMs });
    }

    /// <summary>
    /// Checks the import said how many secrets it imported, then closes its dialog.
    /// </summary>
    /// <param name="count">How many.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("{int} secrets are imported")]
    public async Task ThenSecretsAreImported(int count)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Expect(Page.Locator("#import-error")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#import-done")).ToHaveTextAsync($"{count} secrets imported.");
        await Page.Locator("#import-close").ClickAsync();
        await Expect(Page.Locator("#import-dialog")).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Waits for the open vault to finish reading its secrets, and checks how many it holds.
    /// </summary>
    /// <param name="count">How many.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the vault holds {int} secrets")]
    public async Task ThenTheVaultHoldsSecrets(int count)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Expect(Page.Locator("#secrets-loading")).ToHaveCountAsync(0, new() { Timeout = BrowserSession.SyncTimeoutMs });
        await Expect(Page.Locator("#secrets-count")).ToHaveTextAsync($"Showing {count}/{count} secrets");
    }
}
