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

    private IPage Page => session.Page;

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
    /// Checks the search found exactly the one secret with that name; the following steps act on it.
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
        var items = Page.Locator("#secrets .secret-item");
        await Expect(items).ToHaveCountAsync(1);
        await Expect(items.Locator(".secret-name")).ToHaveTextAsync(name);
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
