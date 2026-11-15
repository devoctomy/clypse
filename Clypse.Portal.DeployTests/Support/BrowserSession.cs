using System.Text.RegularExpressions;

namespace Clypse.Portal.DeployTests.Support;

/// <summary>
/// One scenario's browser: a fresh context, so local storage (session, settings) starts empty, and its page, on the
/// deployed portal. Reqnroll makes one for each scenario and gives the same one to its hooks and steps.
/// </summary>
/// <param name="scenario">The scenario, to name its screenshots.</param>
public sealed partial class BrowserSession(ScenarioContext scenario)
{
    /// <summary>
    /// Maximum time for a single Playwright action or assertion. Signing in goes to Cognito over the internet.
    /// </summary>
    public const int StepTimeoutMs = 30000;

    /// <summary>
    /// Maximum time for the app to start after a page is loaded. Each scenario has a fresh browser context, so the
    /// first load downloads the whole WebAssembly app from CloudFront.
    /// </summary>
    public const int AppStartTimeoutMs = 120000;

    /// <summary>
    /// Maximum time to create or unlock a vault. Its key is derived from the passphrase with Argon2id at the deployed
    /// settings (512 MiB of memory), which takes far longer in the browser than a step.
    /// </summary>
    public const int KeyDerivationTimeoutMs = 180000;

    /// <summary>
    /// Maximum time for a sync pass to finish, including the first after sign in, which downloads the user's vaults.
    /// </summary>
    public const int SyncTimeoutMs = 120000;

    private static readonly string ScreenshotDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "Screenshots");
    private readonly ScenarioContext scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
    private IBrowserContext? context;
    private IPage? page;
    private DeployTestSettings? settings;
    private int screenshotCounter;

    /// <summary>
    /// Gets a value indicating whether the browser has been started for the scenario.
    /// </summary>
    public bool IsStarted => page != null;

    /// <summary>
    /// Gets the page. Only available once started.
    /// </summary>
    public IPage Page => page ?? throw new InvalidOperationException("The browser has not been started.");

    /// <summary>
    /// Gets the deployed environment and test user. Only available once started.
    /// </summary>
    public DeployTestSettings Settings => settings ?? throw new InvalidOperationException("The browser has not been started.");

    /// <summary>
    /// Opens a fresh context and page in the browser.
    /// </summary>
    /// <param name="browser">The browser, shared by every scenario.</param>
    /// <param name="deployTestSettings">The deployed environment and test user.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StartAsync(IBrowser browser, DeployTestSettings deployTestSettings)
    {
        ArgumentNullException.ThrowIfNull(browser);
        ArgumentNullException.ThrowIfNull(deployTestSettings);

        settings = deployTestSettings;
        context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            ColorScheme = ColorScheme.Light,

            // So the steps can read what the app copied.
            Permissions = ["clipboard-read", "clipboard-write"],
        });
        page = await context.NewPageAsync();
        page.SetDefaultTimeout(StepTimeoutMs);
        Assertions.SetDefaultExpectTimeout(StepTimeoutMs);
        Directory.CreateDirectory(ScreenshotDirectory);
    }

    /// <summary>
    /// Closes the context and its page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        if (context != null)
        {
            await context.CloseAsync();
        }

        context = null;
        page = null;
    }

    /// <summary>
    /// Saves a screenshot to TestResults/Screenshots, named after the scenario.
    /// </summary>
    /// <param name="name">A short description of what is being captured.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ScreenshotAsync(string name)
    {
        screenshotCounter++;
        var title = NotAllowedInFileName().Replace(scenario.ScenarioInfo.Title, "_");
        var fileName = $"{title}_{screenshotCounter:D2}_{name}.png";
        await Page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotDirectory, fileName) });
    }

    [GeneratedRegex(@"[^A-Za-z0-9\-]+")]
    private static partial Regex NotAllowedInFileName();
}
