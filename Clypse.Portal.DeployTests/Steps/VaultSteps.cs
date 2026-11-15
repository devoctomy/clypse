using Clypse.Portal.DeployTests.Support;
using static Microsoft.Playwright.Assertions;

namespace Clypse.Portal.DeployTests.Steps;

/// <summary>
/// Steps for the vaults page and the test vault: finding it, creating, unlocking and locking it. The test user has at
/// most one vault, the test vault.
/// </summary>
/// <param name="session">The scenario's browser.</param>
/// <param name="flow">Ends the scenario early when there is nothing for it to do.</param>
[Binding]
public sealed class VaultSteps(BrowserSession session, ScenarioFlow flow)
{
    private readonly BrowserSession session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly ScenarioFlow flow = flow ?? throw new ArgumentNullException(nameof(flow));
    private string? vaultId;

    private IPage Page => session.Page;

    private ILocator VaultCards => Page.Locator("#vaults-page .vault-card");

    /// <summary>
    /// Opens the vaults page from the sidebar and waits for it to show its vaults, or that there are none.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Given("I have opened the vaults page")]
    public async Task GivenIHaveOpenedTheVaultsPage()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#nav-vaults").ClickAsync();
        await Expect(Page.Locator("#vaults-page")).ToBeVisibleAsync();
        await Expect(Page.Locator("#vaults, #no-vaults").First).ToBeVisibleAsync();
    }

    /// <summary>
    /// Goes on only when the test user has no vault; otherwise the test vault exists already, and the scenario ends,
    /// passed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Given("the test user has no vault yet, or the scenario ends here")]
    public async Task GivenTheTestUserHasNoVaultYetOrTheScenarioEndsHere()
    {
        if (flow.HasEnded)
        {
            return;
        }

        var count = await VaultCards.CountAsync();
        if (count > 0)
        {
            flow.End($"the test user already has {Vaults(count)}, so the test vault exists.");
            return;
        }

        await Expect(Page.Locator("#no-vaults")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Goes on only when the test user has their one vault, the test vault; with none it is not created yet (the first
    /// run), and the scenario ends, passed. More than one fails: the test user only ever has the test vault.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Given("the test user has a vault, or the scenario ends here")]
    public async Task GivenTheTestUserHasAVaultOrTheScenarioEndsHere()
    {
        if (flow.HasEnded)
        {
            return;
        }

        var count = await VaultCards.CountAsync();
        if (count == 0)
        {
            flow.End("the test user has no vault yet, so there is no test vault to use. It is created by \"The test vault is created on the first run\".");
            return;
        }

        if (count > 1)
        {
            throw new InvalidOperationException($"The test user has {Vaults(count)}: they should only ever have the test vault.");
        }

        vaultId = await VaultCards.First.GetAttributeAsync("data-vault");
    }

    /// <summary>
    /// Creates the test vault from the empty vaults page, which opens it, unlocked.
    /// </summary>
    /// <param name="name">The vault's name.</param>
    /// <param name="passphrase">Its passphrase.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I create the test vault named {string} with the passphrase {string}")]
    public async Task WhenICreateTheTestVault(string name, string passphrase)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#create-first-vault-button").ClickAsync();
        await Page.Locator("#vault-name").FillAsync(name);
        await Page.Locator("#vault-passphrase").FillAsync(passphrase);
        await Page.Locator("#vault-confirm-passphrase").FillAsync(passphrase);
        await Page.Locator("#vault-acknowledge").CheckAsync();
        await Page.Locator("#vault-create").ClickAsync();
        await Expect(Page.Locator("#vault-page")).ToBeVisibleAsync(new() { Timeout = BrowserSession.KeyDerivationTimeoutMs });
        vaultId = Page.Url[(Page.Url.LastIndexOf('/') + 1)..];
    }

    /// <summary>
    /// Opens the test vault from the vaults page and unlocks it with its passphrase.
    /// </summary>
    /// <param name="passphrase">Its passphrase.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I unlock the test vault with the passphrase {string}")]
    public async Task WhenIUnlockTheTestVault(string passphrase)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await TestVaultCard().Locator(".vault-card-main").ClickAsync();
        await Page.Locator("#e2e-unlock-passphrase").FillAsync(passphrase);
        await Page.Locator("#e2e-unlock-button").ClickAsync();
        await Expect(Page.Locator("#e2e-unlock-panel")).ToHaveCountAsync(0, new() { Timeout = BrowserSession.KeyDerivationTimeoutMs });
        await Expect(Page.Locator("#vault-page")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Locks the open vault, which goes back to the vaults page.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I lock the vault")]
    public async Task WhenILockTheVault()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#lock-vault-button").ClickAsync();
        await Expect(Page.Locator("#vaults-page")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Checks the vaults page lists the test vault, locked.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the test vault is listed as locked")]
    public async Task ThenTheTestVaultIsListedAsLocked()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Expect(TestVaultCard()).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-locked\b"));
    }

    private static string Vaults(int count) => count == 1 ? "1 vault" : $"{count} vaults";

    private ILocator TestVaultCard()
    {
        var id = vaultId ?? throw new InvalidOperationException("The test vault has not been found or created in this scenario.");
        return Page.Locator($"#vaults-page .vault-card[data-vault='{id}']");
    }
}
