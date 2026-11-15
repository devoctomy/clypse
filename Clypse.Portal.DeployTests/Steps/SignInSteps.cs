using System.Text.RegularExpressions;
using Clypse.Portal.DeployTests.Support;
using static Microsoft.Playwright.Assertions;

namespace Clypse.Portal.DeployTests.Steps;

/// <summary>
/// Steps for the sign-in page, signing in as the test user, and signing out.
/// </summary>
/// <param name="session">The scenario's browser.</param>
/// <param name="flow">Says whether the scenario has ended early.</param>
[Binding]
public sealed partial class SignInSteps(BrowserSession session, ScenarioFlow flow)
{
    private readonly BrowserSession session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly ScenarioFlow flow = flow ?? throw new ArgumentNullException(nameof(flow));

    private IPage Page => session.Page;

    /// <summary>
    /// Opens the portal and waits for the app to start and send the browser to the sign-in page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Given("the sign-in page is open")]
    public async Task GivenTheSignInPageIsOpen()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.GotoAsync($"{session.Settings.PortalUrl}/");
        await Expect(Page.Locator("#app > .loading-progress")).ToHaveCountAsync(0, new() { Timeout = BrowserSession.AppStartTimeoutMs });
        await ThenTheSignInPageIsShown();
    }

    /// <summary>
    /// Signs in as the test user from the sign-in page, and waits for the vaults page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Given("I have signed in as the test user")]
    public async Task GivenIHaveSignedInAsTheTestUser()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await WhenISignInAsTheTestUser();
        await ThenTheVaultsPageIsShown();
    }

    /// <summary>
    /// Submits the sign-in form with the test user's email and password.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I sign in as the test user")]
    public Task WhenISignInAsTheTestUser()
    {
        if (flow.HasEnded)
        {
            return Task.CompletedTask;
        }

        return SubmitSignInAsync(session.Settings.Password);
    }

    /// <summary>
    /// Submits the sign-in form with the test user's email and a password that is not theirs.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I sign in as the test user with a wrong password")]
    public Task WhenISignInAsTheTestUserWithAWrongPassword()
    {
        if (flow.HasEnded)
        {
            return Task.CompletedTask;
        }

        return SubmitSignInAsync(session.Settings.Password + "-wrong");
    }

    /// <summary>
    /// Signs out with the sidebar's button.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I sign out")]
    public Task WhenISignOut()
    {
        if (flow.HasEnded)
        {
            return Task.CompletedTask;
        }

        return Page.Locator("#logout-button").ClickAsync();
    }

    /// <summary>
    /// Checks the vaults page (the home page) is shown.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the vaults page is shown")]
    public Task ThenTheVaultsPageIsShown()
    {
        if (flow.HasEnded)
        {
            return Task.CompletedTask;
        }

        return Expect(Page.Locator("#vaults-page")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Checks the sign-in form is no longer on the page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the sign-in form is gone")]
    public Task ThenTheSignInFormIsGone()
    {
        if (flow.HasEnded)
        {
            return Task.CompletedTask;
        }

        return Expect(Page.Locator("#login-button")).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Checks the sign-in page shows an error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("a sign-in error is shown")]
    public async Task ThenASignInErrorIsShown()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Expect(Page.Locator("#login-error")).ToBeVisibleAsync();
        await Expect(Page.Locator("#login-error")).Not.ToBeEmptyAsync();
    }

    /// <summary>
    /// Checks the browser is on the sign-in page.
    /// </summary>
    /// <remarks>
    /// Blazor redirects with client-side (pushState) navigation, so this uses a polling URL assertion.
    /// </remarks>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the sign-in page is shown")]
    public async Task ThenTheSignInPageIsShown()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Expect(Page).ToHaveURLAsync(LoginUrl());
        await Expect(Page.Locator("#login-button")).ToBeVisibleAsync();
    }

    [GeneratedRegex("/login$")]
    private static partial Regex LoginUrl();

    private async Task SubmitSignInAsync(string password)
    {
        await Page.Locator("#username").FillAsync(session.Settings.Email);
        await Page.Locator("#password").FillAsync(password);
        await Page.Locator("#login-button").ClickAsync();
    }
}
