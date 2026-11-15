using Reqnroll.UnitTestProvider;

namespace Clypse.Portal.DeployTests.Support;

/// <summary>
/// Starts a browser for each scenario, and skips the scenario when no deployed environment is given.
/// </summary>
/// <remarks>
/// First time only, install the browser:
/// <c>pwsh Clypse.Portal.DeployTests/bin/Debug/net10.0/playwright.ps1 install chromium</c>.
/// Then set <c>CLYPSE_DEPLOYTEST_PORTAL_URL</c>, <c>CLYPSE_DEPLOYTEST_EMAIL</c> and <c>CLYPSE_DEPLOYTEST_PASSWORD</c>.
/// Set <c>HEADED=1</c> to watch the browser.
/// </remarks>
/// <param name="session">The scenario's browser.</param>
/// <param name="scenario">The scenario, to see whether it failed.</param>
/// <param name="runtime">Skips the scenario.</param>
[Binding]
public sealed class BrowserHooks(BrowserSession session, ScenarioContext scenario, IUnitTestRuntimeProvider runtime)
{
    private static readonly SemaphoreSlim BrowserLock = new(1, 1);
    private static IPlaywright? playwright;
    private static IBrowser? browser;

    private readonly BrowserSession session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly ScenarioContext scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
    private readonly IUnitTestRuntimeProvider runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));

    /// <summary>
    /// Opens the scenario's browser, or skips the scenario when no deployed environment is given.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [BeforeScenario]
    public async Task StartBrowserAsync()
    {
        var settings = DeployTestSettings.FromEnvironment();
        if (settings == null)
        {
            runtime.TestInconclusive($"No deployed environment. Set {DeployTestSettings.PortalUrlVariable}, {DeployTestSettings.EmailVariable} and {DeployTestSettings.PasswordVariable} to run the deploy tests.");
            return;
        }

        await session.StartAsync(await GetBrowserAsync(), settings);
    }

    /// <summary>
    /// Takes a screenshot if the scenario failed, and closes its browser.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [AfterScenario]
    public async Task StopBrowserAsync()
    {
        if (!session.IsStarted)
        {
            return;
        }

        try
        {
            if (scenario.TestError != null)
            {
                await session.ScreenshotAsync("FAILED");
            }
        }
        finally
        {
            await session.StopAsync();
        }
    }

    /// <summary>
    /// Closes the browser once every scenario has run.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [AfterTestRun]
    public static async Task CloseBrowserAsync()
    {
        if (browser != null)
        {
            await browser.CloseAsync();
            browser = null;
        }

        playwright?.Dispose();
        playwright = null;
    }

    // Started by the first scenario that runs, so a run with no deployed environment never needs the browser installed.
    private static async Task<IBrowser> GetBrowserAsync()
    {
        await BrowserLock.WaitAsync();
        try
        {
            playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();
            browser ??= await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = Environment.GetEnvironmentVariable("HEADED") != "1",
            });
            return browser;
        }
        finally
        {
            BrowserLock.Release();
        }
    }
}
