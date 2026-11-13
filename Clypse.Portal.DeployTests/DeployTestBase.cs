using System.Text.RegularExpressions;
using Microsoft.Playwright.MSTest;

namespace Clypse.Portal.DeployTests;

/// <summary>
/// Base class for the deploy tests: UI tests of the deployed portal, signing in with the real user pool. Each test has a
/// fresh browser context, so local storage (session, settings) starts empty.
/// </summary>
/// <remarks>
/// First time only, install the browser:
/// <c>pwsh Clypse.Portal.DeployTests/bin/Debug/net10.0/playwright.ps1 install chromium</c>.
/// Then set <c>CLYPSE_DEPLOYTEST_PORTAL_URL</c>, <c>CLYPSE_DEPLOYTEST_EMAIL</c> and <c>CLYPSE_DEPLOYTEST_PASSWORD</c>.
/// </remarks>
[TestClass]
public class DeployTestBase : PageTest
{
    /// <summary>
    /// Maximum time for a single Playwright action or assertion. Signing in goes to Cognito over the internet.
    /// </summary>
    protected const int StepTimeoutMs = 30000;

    /// <summary>
    /// Maximum time for the app to start after a page is loaded. Each test has a fresh browser context, so the first
    /// load downloads the whole WebAssembly app from CloudFront.
    /// </summary>
    protected const int AppStartTimeoutMs = 120000;

    private static readonly string ScreenshotDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "Screenshots");
    private static readonly Regex LoginUrlPattern = new("/login$");
    private DeployTestSettings? settings;

    /// <summary>
    /// Gets the settings. Only null before <see cref="BaseTestInitialize"/> has run.
    /// </summary>
    protected DeployTestSettings Settings => settings ?? throw new InvalidOperationException("No deploy test settings.");

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            ColorScheme = ColorScheme.Light,
        };
    }

    [TestInitialize]
    public void BaseTestInitialize()
    {
        settings = DeployTestSettings.FromEnvironment();
        if (settings == null)
        {
            Assert.Inconclusive($"No deployed environment. Set {DeployTestSettings.PortalUrlVariable}, {DeployTestSettings.EmailVariable} and {DeployTestSettings.PasswordVariable} to run the deploy tests.");
        }

        Directory.CreateDirectory(ScreenshotDirectory);
        Page.SetDefaultTimeout(StepTimeoutMs);
        Assertions.SetDefaultExpectTimeout(StepTimeoutMs);
    }

    [TestCleanup]
    public async Task BaseTestCleanup()
    {
        if (TestContext.CurrentTestOutcome == UnitTestOutcome.Failed)
        {
            await Page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotDirectory, $"{TestContext.TestName}_FAILED.png") });
        }
    }

    /// <summary>
    /// Opens the portal and waits for the unauthenticated redirect to the login page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task GoToLoginAsync()
    {
        await Page.GotoAsync($"{Settings.PortalUrl}/");
        await Expect(Page.Locator("#app > .loading-progress")).ToHaveCountAsync(0, new() { Timeout = AppStartTimeoutMs });
        await ExpectLoginPageAsync();
    }

    /// <summary>
    /// Fills in the login form and submits it. The page must show the login page.
    /// </summary>
    /// <param name="password">The password to sign in with.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task SubmitLoginAsync(string password)
    {
        await Page.Locator("#username").FillAsync(Settings.Email);
        await Page.Locator("#password").FillAsync(password);
        await Page.Locator("#login-button").ClickAsync();
    }

    /// <summary>
    /// Signs in as the test user and waits for the vaults page (the home page).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task LoginAsync()
    {
        await GoToLoginAsync();
        await SubmitLoginAsync(Settings.Password);
        await Expect(Page.Locator("#vaults-page")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Asserts that the browser has been sent to the login page.
    /// </summary>
    /// <remarks>
    /// Blazor redirects with client-side (pushState) navigation, so use a polling URL assertion.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task ExpectLoginPageAsync()
    {
        await Expect(Page).ToHaveURLAsync(LoginUrlPattern);
        await Expect(Page.Locator("#login-button")).ToBeVisibleAsync();
    }
}
