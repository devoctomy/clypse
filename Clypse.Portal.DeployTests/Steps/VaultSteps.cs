using Clypse.Portal.DeployTests.Support;
using static Microsoft.Playwright.Assertions;

namespace Clypse.Portal.DeployTests.Steps;

/// <summary>
/// Steps for the vaults page and the scenario's vault: finding the test vault, creating, unlocking, locking and
/// deleting a vault. Between scenarios the test user has at most one vault, the test vault; a scenario that creates
/// another deletes it again.
/// </summary>
/// <param name="session">The scenario's browser.</param>
/// <param name="flow">Ends the scenario early when there is nothing for it to do.</param>
[Binding]
public sealed class VaultSteps(BrowserSession session, ScenarioFlow flow)
{
    private readonly BrowserSession session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly ScenarioFlow flow = flow ?? throw new ArgumentNullException(nameof(flow));
    private string? vaultId;
    private string? deletedName;

    private IPage Page => session.Page;

    private ILocator VaultCards => Page.Locator("#vaults-page .vault-card");

    /// <summary>
    /// Opens the vaults page from the sidebar and waits for it to show its vaults, or that there are none. A new browser
    /// shows the page before it has read the user's vaults from the cloud; it says it is loading until it has.
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

        var count = await CountVaultsAsync();
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

        var count = await CountVaultsAsync();
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
    /// Creates a vault from the vaults page (with the button shown when there are none, or the New vault button), which
    /// opens it, unlocked. It is the vault the later steps of the scenario use.
    /// </summary>
    /// <param name="name">The vault's name.</param>
    /// <param name="passphrase">Its passphrase.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I create the test vault named {string} with the passphrase {string}")]
    [When("I create a vault named {string} with the passphrase {string}")]
    public async Task WhenICreateTheTestVault(string name, string passphrase)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#create-first-vault-button, #new-vault-button").First.ClickAsync();
        await Page.Locator("#vault-name").FillAsync(name);
        await Page.Locator("#vault-passphrase").FillAsync(passphrase);
        await Page.Locator("#vault-confirm-passphrase").FillAsync(passphrase);
        await Page.Locator("#vault-acknowledge").CheckAsync();
        await Page.Locator("#vault-create").ClickAsync();
        await Expect(Page.Locator("#vault-page")).ToBeVisibleAsync(new() { Timeout = BrowserSession.KeyDerivationTimeoutMs });
        vaultId = Page.Url[(Page.Url.LastIndexOf('/') + 1)..];
    }

    /// <summary>
    /// Opens the scenario's vault (the test vault, or the one it created) from the vaults page and unlocks it with its
    /// passphrase.
    /// </summary>
    /// <param name="passphrase">Its passphrase.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I unlock the test vault with the passphrase {string}")]
    [When("I unlock the vault with the passphrase {string}")]
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
    /// Checks the vaults page lists the scenario's vault, locked.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the test vault is listed as locked")]
    [Then("the vault is listed as locked")]
    public async Task ThenTheTestVaultIsListedAsLocked()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Expect(TestVaultCard()).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-locked\b"));
    }

    /// <summary>
    /// Deletes the open vault from its settings, typing its name to confirm. The vaults page is shown at once.
    /// </summary>
    /// <param name="name">The vault's name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I delete the vault named {string}")]
    public async Task WhenIDeleteTheVault(string name)
    {
        if (flow.HasEnded)
        {
            return;
        }

        deletedName = name;
        await Page.Locator("#vault-settings-button").ClickAsync();
        await Page.Locator("#vault-delete-confirmation").FillAsync(name);
        await Page.Locator("#vault-delete-button").ClickAsync();
        await Expect(Page.Locator("#vaults-page")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Checks the vaults page lists the scenario's vault as being deleted: it says so, and cannot be opened.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the vault is listed as being deleted")]
    public async Task ThenTheVaultIsListedAsBeingDeleted()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Expect(TestVaultCard()).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bis-deleting\b"));
        await Expect(TestVaultCard().Locator(".vault-card-main")).ToBeDisabledAsync();
    }

    /// <summary>
    /// Waits for the vault being deleted to be gone: the app says it was deleted, and it is no longer listed.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the vault is deleted")]
    public async Task ThenTheVaultIsDeleted()
    {
        if (flow.HasEnded)
        {
            return;
        }

        var name = deletedName ?? throw new InvalidOperationException("No vault has been deleted in this scenario.");
        await Expect(Page.Locator(".bw-toast", new() { HasText = $"{name} deleted." })).ToBeVisibleAsync(new() { Timeout = BrowserSession.SyncTimeoutMs });
        await Expect(TestVaultCard()).ToHaveCountAsync(0);
    }

    private static string Vaults(int count) => count == 1 ? "1 vault" : $"{count} vaults";

    // Counts the vaults as a user would see them. The page says it is loading until it has read the user's vaults from
    // the cloud, and only then shows them (all at once), or that there are none.
    private async Task<int> CountVaultsAsync()
    {
        await Expect(Page.Locator("#vaults, #no-vaults").First).ToBeVisibleAsync();
        return await VaultCards.CountAsync();
    }

    private ILocator TestVaultCard()
    {
        var id = vaultId ?? throw new InvalidOperationException("The test vault has not been found or created in this scenario.");
        return Page.Locator($"#vaults-page .vault-card[data-vault='{id}']");
    }
}
