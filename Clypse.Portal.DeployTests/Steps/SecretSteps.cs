using System.Text.RegularExpressions;
using Clypse.Portal.DeployTests.Support;
using static Microsoft.Playwright.Assertions;

namespace Clypse.Portal.DeployTests.Steps;

/// <summary>
/// Steps for the secrets in the open vault: adding a login, finding one, and copying its fields.
/// </summary>
/// <param name="session">The scenario's browser.</param>
/// <param name="flow">Says whether the scenario has ended early.</param>
[Binding]
public sealed class SecretSteps(BrowserSession session, ScenarioFlow flow)
{
    private readonly BrowserSession session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly ScenarioFlow flow = flow ?? throw new ArgumentNullException(nameof(flow));
    private ILocator? foundRow;
    private string? generatedName;
    private string? generatedPassword;

    private IPage Page => session.Page;

    private string GeneratedName => generatedName ?? throw new InvalidOperationException("No login with a generated password has been added in this scenario.");

    /// <summary>
    /// Adds a login to the open vault named with the date and time (UTC), so each run adds a new one, and with a
    /// random password from the app's generator. The name and password are kept for the steps that check it later.
    /// </summary>
    /// <param name="name">The start of the login's name.</param>
    /// <param name="userName">Its user name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I add a login named {string} with the date and time, user name {string} and a random password from the generator")]
    public async Task WhenIAddALoginWithAGeneratedPassword(string name, string userName)
    {
        if (flow.HasEnded)
        {
            return;
        }

        generatedName = $"{name} {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        await Page.Locator("#new-secret-button").ClickAsync();
        await Page.Locator("#secret-field-Name").FillAsync(generatedName);
        await Page.Locator("#secret-field-UserName").FillAsync(userName);

        await Page.Locator(".secret-generate").ClickAsync();
        await Expect(Page.Locator("#generator-dialog")).ToBeVisibleAsync();
        var random = Page.Locator("#generator-mode-random");
        if (await random.CountAsync() > 0)
        {
            // Only offered when memorable passwords can be made; the user's default may be memorable.
            await random.ClickAsync();
            await Expect(random).ToHaveAttributeAsync("aria-pressed", "true");
        }

        var shown = Page.Locator(".generated-password-text");
        await Expect(shown).Not.ToBeEmptyAsync();
        var password = (await shown.TextContentAsync())?.Trim();
        await Page.Locator("#generator-use").ClickAsync();
        await Expect(Page.Locator("#generator-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#secret-field-Password")).ToHaveValueAsync(password!);
        generatedPassword = password;

        await Page.Locator("#secret-save").ClickAsync();
        await Expect(Page.Locator("#secret-dialog")).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Searches the open vault for the login added with a generated password.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I search for the generated login")]
    public Task WhenISearchForTheGeneratedLogin()
    {
        return WhenISearchForTheSecret(flow.HasEnded ? string.Empty : GeneratedName);
    }

    /// <summary>
    /// Checks the search found exactly the login added with a generated password.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the generated login is found")]
    public Task ThenTheGeneratedLoginIsFound()
    {
        return ThenTheSecretIsFound(flow.HasEnded ? string.Empty : GeneratedName);
    }

    /// <summary>
    /// Opens the found login in the secret editor and checks its password is the one the generator made.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("its password is the one the generator made")]
    public async Task ThenItsPasswordIsTheOneTheGeneratorMade()
    {
        if (flow.HasEnded)
        {
            return;
        }

        var row = foundRow ?? throw new InvalidOperationException("No secret has been found in this scenario.");
        var password = generatedPassword ?? throw new InvalidOperationException("No password has been generated in this scenario.");
        await row.Locator(".secret-row").ClickAsync();
        await Expect(Page.Locator("#secret-dialog")).ToBeVisibleAsync();
        await Expect(Page.Locator("#secret-field-Password")).ToHaveValueAsync(password);
    }

    /// <summary>
    /// Adds a login to the open vault with the secret editor.
    /// </summary>
    /// <param name="name">The login's name.</param>
    /// <param name="userName">Its user name.</param>
    /// <param name="password">Its password.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I add a login named {string} with user name {string} and password {string}")]
    public async Task WhenIAddALogin(string name, string userName, string password)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#new-secret-button").ClickAsync();
        await Page.Locator("#secret-field-Name").FillAsync(name);
        await Page.Locator("#secret-field-UserName").FillAsync(userName);
        await Page.Locator("#secret-field-Password").FillAsync(password);
        await Page.Locator("#secret-save").ClickAsync();
        await Expect(Page.Locator("#secret-dialog")).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Searches the open vault's secrets.
    /// </summary>
    /// <param name="name">What to search for.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I search for the secret {string}")]
    public async Task WhenISearchForTheSecret(string name)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#secret-search").FillAsync(name);
    }

    /// <summary>
    /// Checks the search found exactly one secret with that exact name; the following steps act on it. The search
    /// matches part of a name, so it can find others too (e.g. the dated logins whose names start with it).
    /// </summary>
    /// <param name="name">The secret's name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the secret {string} is found")]
    public async Task ThenTheSecretIsFound(string name)
    {
        if (flow.HasEnded)
        {
            return;
        }

        // Each secret is an item holding its row (name, summary, tags) and, beside it, its quick copy buttons.
        var exactName = new Regex($@"^\s*{Regex.Escape(name)}\s*$");
        var items = Page.Locator("#secrets .secret-item").Filter(new()
        {
            Has = Page.Locator(".secret-name", new() { HasTextRegex = exactName }),
        });
        await Expect(items).ToHaveCountAsync(1);
        foundRow = items.First;
    }

    /// <summary>
    /// Copies the found secret's user name with its quick copy button.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I copy its user name")]
    public Task WhenICopyItsUserName()
    {
        return flow.HasEnded ? Task.CompletedTask : CopyAsync("UserName");
    }

    /// <summary>
    /// Copies the found secret's password with its quick copy button.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I copy its password")]
    public Task WhenICopyItsPassword()
    {
        return flow.HasEnded ? Task.CompletedTask : CopyAsync("Password");
    }

    /// <summary>
    /// Checks what is on the clipboard, waiting for the copy to land there.
    /// </summary>
    /// <param name="expected">What should be on it.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the clipboard holds {string}")]
    public async Task ThenTheClipboardHolds(string expected)
    {
        if (flow.HasEnded)
        {
            return;
        }

        var deadline = DateTime.UtcNow.AddMilliseconds(BrowserSession.StepTimeoutMs);
        string? actual;
        do
        {
            actual = await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
            if (actual == expected)
            {
                return;
            }

            await Page.WaitForTimeoutAsync(250);
        }
        while (DateTime.UtcNow < deadline);

        throw new InvalidOperationException($"The clipboard holds \"{actual}\", not \"{expected}\".");
    }

    private Task CopyAsync(string field)
    {
        var row = foundRow ?? throw new InvalidOperationException("No secret has been found in this scenario.");
        return row.Locator($".secret-quick-copy[data-field='{field}']").ClickAsync();
    }
}
