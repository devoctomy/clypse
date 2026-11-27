using System.Text.RegularExpressions;

namespace Clypse.Portal.UITests;

/// <summary>
/// Vaults and their secrets, in the browser: each vault is a protected profile, kept (encrypted) in IndexedDB. The UI
/// tests have no AWS, so nothing is synced to S3.
/// </summary>
[TestClass]
public partial class VaultTests : TestBase
{
    [TestMethod]
    public async Task GivenNoVaults_WhenSignedIn_ThenTheEmptyStateOffersToCreateOne()
    {
        await LoginAsync();

        await Expect(Page.Locator("#no-vaults")).ToBeVisibleAsync();
        await Expect(Page.Locator("#create-first-vault-button")).ToBeVisibleAsync();
        await Expect(Page.Locator("#new-vault-button")).ToHaveCountAsync(0);
        await ScreenshotAsync("NoVaults");
    }

    [TestMethod]
    public async Task GivenTheNewVaultDialog_WhenThePassphrasesDiffer_ThenItSaysSoAndCreatesNothing()
    {
        await LoginAsync();
        await Page.Locator("#create-first-vault-button").ClickAsync();
        await Page.Locator("#vault-name").FillAsync("Personal");
        await Page.Locator("#vault-passphrase").FillAsync(Passphrase);
        await Page.Locator("#vault-confirm-passphrase").FillAsync("something else entirely");
        await Page.Locator("#vault-acknowledge").CheckAsync();

        await Page.Locator("#vault-create").ClickAsync();

        await Expect(Page.Locator("#vault-error")).ToHaveTextAsync("The passphrases do not match.");
        await Page.Locator("#vault-dialog .btn-secondary").ClickAsync();
        await Expect(Page.Locator("#no-vaults")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task GivenANameAndPassphrase_WhenAVaultIsCreated_ThenItOpensEmptyAndIsListedByName()
    {
        await LoginAsync();

        var id = await CreateVaultAsync("Personal", "Everyday logins");

        Assert.IsTrue(Guid.TryParseExact(id, "D", out _), $"'{id}' is not a GUID");
        await Expect(Page.Locator("#report-button")).ToBeVisibleAsync();
        await Expect(Page.Locator("#report-alert")).ToHaveCountAsync(0);
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Personal");
        await Expect(Page.Locator(".page-subtitle")).ToHaveTextAsync("Everyday logins");
        await Expect(Page.Locator("#no-secrets")).ToBeVisibleAsync();
        await Page.Locator("#nav-vaults").ClickAsync();
        var card = Page.Locator($".vault-card[data-vault='{id}']");
        await Expect(card.Locator(".vault-card-name")).ToHaveTextAsync("Personal");
        await Expect(card.Locator(".vault-card-state")).ToHaveTextAsync("Unlocked");
        await ScreenshotAsync("Vaults");
    }

    [TestMethod]
    public async Task GivenAVault_WhenSecretsAreAdded_ThenTheyAreListedAndCanBeSearchedAndFilteredByTag()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");

        await AddLoginAsync("Gmail", "bob", "hunter2hunter2", "email, personal");
        await AddLoginAsync("Bank", "bob.hoskins", "correct horse", "money");

        var rows = Page.Locator("#secrets .secret-row");
        await Expect(rows).ToHaveCountAsync(2);
        await Expect(rows.First.Locator(".secret-name")).ToHaveTextAsync("Bank");
        await Expect(Page.Locator("#secrets-count")).ToHaveTextAsync("Showing 2/2 secrets");
        await ScreenshotAsync("Secrets");

        await Page.Locator("#secret-search").FillAsync("gma");
        await Expect(rows).ToHaveCountAsync(1);
        await Expect(rows.Locator(".secret-name")).ToHaveTextAsync("Gmail");
        await Page.Locator("#secret-search").FillAsync("nothing like it");
        await Expect(Page.Locator("#no-matches")).ToBeVisibleAsync();
        await Page.Locator("#secret-search").FillAsync(string.Empty);

        await Page.Locator(".tag-filter[data-tag='money']").ClickAsync();
        await Expect(rows).ToHaveCountAsync(1);
        await Expect(rows.Locator(".secret-name")).ToHaveTextAsync("Bank");
        await Expect(Page.Locator("#secrets-count")).ToHaveTextAsync("Showing 1/2 secrets");
        await Page.Locator(".tag-filter[data-tag='']").ClickAsync();
        await Expect(rows).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task GivenASecret_WhenEditedAndThenDeleted_ThenTheListFollows()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await AddLoginAsync("Gmail", "bob", "hunter2hunter2");

        await Page.Locator("#secrets .secret-row").ClickAsync();
        await Expect(Page.Locator("#secret-field-Name")).ToHaveValueAsync("Gmail");
        await Expect(Page.Locator("#secret-field-Password")).ToHaveAttributeAsync("type", "password");
        await Page.Locator(".secret-reveal").ClickAsync();
        await Expect(Page.Locator("#secret-field-Password")).ToHaveValueAsync("hunter2hunter2");
        await Page.Locator("#secret-field-Name").FillAsync("Google mail");
        await Page.Locator("#secret-save").ClickAsync();
        await Expect(Page.Locator("#secrets .secret-name")).ToHaveTextAsync("Google mail");

        await Page.Locator("#secrets .secret-row").ClickAsync();
        await Page.Locator("#secret-delete").ClickAsync();
        await Page.Locator("#secret-delete-yes").ClickAsync();

        await Expect(Page.Locator("#no-secrets")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task GivenASecretWithoutAName_WhenSaved_ThenItSaysWhatIsMissing()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#new-secret-button").ClickAsync();
        await Page.Locator("#secret-field-UserName").FillAsync("bob");

        await Page.Locator("#secret-save").ClickAsync();

        await Expect(Page.Locator("#secret-error")).ToHaveTextAsync("The secret is missing: Name.");
        await Expect(Page.Locator("#secret-dialog")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task GivenTheSecretEditor_WhenAPasswordIsGenerated_ThenItIsPutInThePasswordField()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#new-secret-button").ClickAsync();

        await Page.Locator(".secret-generate").ClickAsync();
        await Expect(Page.Locator("#generator-dialog")).ToBeVisibleAsync();
        var generated = (await Page.Locator(".generated-password-text").TextContentAsync())!.Trim();
        Assert.AreEqual(20, generated.Length);
        await ScreenshotAsync("Generator");
        await Page.Locator("#generator-use").ClickAsync();

        await Expect(Page.Locator("#generator-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#secret-field-Password")).ToHaveValueAsync(generated);
        await Expect(Page.Locator(".strength-meter")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task GivenTheSecretEditor_WhenACommonPasswordIsTyped_ThenTheMeterSaysItIsCommon()
    {
        // The UI tests run with the stand-in list of weak known passwords (appsettings.Development.json), which has it.
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#new-secret-button").ClickAsync();

        await Page.Locator("#secret-field-Password").FillAsync("password123");

        await Expect(Page.Locator(".strength-meter")).ToHaveAttributeAsync("data-strength", "VeryWeak");
        await Expect(Page.Locator(".strength-text")).ToHaveTextAsync("Very weak: it is a commonly used password.");
    }

    [TestMethod]
    public async Task GivenTheGenerator_WhenMemorableIsChosen_ThenAPasswordIsMadeFromTheTemplate()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#new-secret-button").ClickAsync();
        await Page.Locator(".secret-generate").ClickAsync();

        await Page.Locator("#generator-mode-memorable").ClickAsync();

        await Expect(Page.Locator("#generator-template")).ToBeVisibleAsync();
        await Expect(Page.Locator(".generated-password-text")).ToHaveTextAsync(new Regex(@"^[A-Za-z]+\d{2}[A-Za-z]+[!\-=_][A-Za-z]+$"));
    }

    [TestMethod]
    public async Task GivenAnUnlockedVault_WhenLocked_ThenTheVaultsPageShowsItLocked_AndItsSecretsAreBackOnceUnlocked()
    {
        await LoginAsync();
        var id = await CreateVaultAsync("Personal");
        await AddLoginAsync("Gmail", "bob", "hunter2hunter2");

        await Page.Locator("#lock-vault-button").ClickAsync();
        await Expect(Page.Locator("#vaults-page")).ToBeVisibleAsync();
        var card = Page.Locator($".vault-card[data-vault='{id}']");
        await Expect(card.Locator(".vault-card-state")).ToHaveTextAsync("Locked");
        await card.Locator(".vault-card-main").ClickAsync();
        await Expect(Page.Locator("#secrets")).ToHaveCountAsync(0);
        await UnlockAsync();

        await Expect(Page.Locator("#secrets .secret-name")).ToHaveTextAsync("Gmail");
    }

    [TestMethod]
    public async Task GivenAVaultWithASecret_WhenReloaded_ThenItIsListedLockedById_AndByNameAfterUnlocking()
    {
        // Signed in with a password, so the vault's name is not kept on the device (it is only kept encrypted with the
        // key derived from the passkey).
        await LoginAsync();
        var id = await CreateVaultAsync("Personal");
        await AddLoginAsync("Gmail", "bob", "hunter2hunter2");
        var kept = await Page.EvaluateAsync<int>("() => Object.keys(localStorage).filter(k => k.includes('e2e_labels_')).length");
        Assert.AreEqual(0, kept);

        await GoToAsync("/");
        var card = Page.Locator($".vault-card[data-vault='{id}']");
        await Expect(card.Locator(".vault-card-name")).ToHaveTextAsync(id);
        await Expect(card.Locator(".vault-card-state")).ToHaveTextAsync("Locked");
        await card.Locator(".vault-card-main").ClickAsync();
        await UnlockAsync();

        await Expect(Page.Locator("#secrets .secret-name")).ToHaveTextAsync("Gmail");
        await Page.Locator("#nav-vaults").ClickAsync();
        await Expect(card.Locator(".vault-card-name")).ToHaveTextAsync("Personal");
    }

    [TestMethod]
    public async Task GivenAVaultsPage_WhenTheBrowserIsReloaded_ThenItAsksForThePassphrase_RatherThanSayingTheVaultDoesNotExist()
    {
        // The app starts on the vault's page before it has read the user's vaults, so it cannot know yet whether the
        // vault exists.
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await AddLoginAsync("Gmail", "bob", "hunter2hunter2");

        await ReloadAsync();

        await Expect(Page.Locator("#e2e-unlock-panel")).ToBeVisibleAsync();
        await Expect(Page.Locator("#vault-not-found")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#vault-finding")).ToHaveCountAsync(0);
        await UnlockAsync();
        await Expect(Page.Locator("#secrets .secret-name")).ToHaveTextAsync("Gmail");
    }

    [TestMethod]
    public async Task GivenAVault_WhenRenamedInItsSettings_ThenItsNewNameIsShownHereAndInTheList()
    {
        await LoginAsync();
        var id = await CreateVaultAsync("Personal", "Logins");

        await Page.Locator("#vault-settings-button").ClickAsync();
        await Expect(Page.Locator("#vault-settings-name")).ToHaveValueAsync("Personal");
        await Expect(Page.Locator("#vault-settings-auto-lock")).ToHaveValueAsync("5");
        await ScreenshotAsync("VaultSettings");
        await Page.Locator("#vault-settings-name").FillAsync("Home");
        await Page.Locator("#vault-settings-description").FillAsync("Shared at home");
        await Page.Locator("#vault-settings-auto-lock").SelectOptionAsync(string.Empty);
        await Page.Locator("#vault-settings-save").ClickAsync();

        await Expect(Page.Locator("#vault-settings-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#toasts")).ToContainTextAsync("Vault settings saved.");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Home");
        await Expect(Page.Locator(".page-subtitle")).ToHaveTextAsync("Shared at home");
        await Page.Locator("#vault-settings-button").ClickAsync();
        await Expect(Page.Locator("#vault-settings-auto-lock")).ToHaveValueAsync(string.Empty);
        await Page.Locator("#vault-settings-dialog .btn-secondary").ClickAsync();
        await Page.Locator("#nav-vaults").ClickAsync();
        await Expect(Page.Locator($".vault-card[data-vault='{id}'] .vault-card-name")).ToHaveTextAsync("Home");
    }

    [TestMethod]
    public async Task GivenTheNewVaultDialog_WhenItsArgon2idSettingsAreChanged_ThenTheVaultIsOnlyCreatedOnceTheyAreBenchmarked()
    {
        await LoginAsync();
        await Page.Locator("#create-first-vault-button").ClickAsync();
        await Expect(Page.Locator("#vault-tab-passphrase")).ToHaveTextAsync("Vault");
        await Expect(Page.Locator("#vault-tab-advanced")).ToHaveTextAsync("Advanced");
        await Page.Locator("#vault-name").FillAsync("Personal");
        await Page.Locator("#vault-passphrase").FillAsync(Passphrase);
        await Page.Locator("#vault-confirm-passphrase").FillAsync(Passphrase);
        await Page.Locator("#vault-acknowledge").CheckAsync();

        await Page.Locator("#vault-tab-advanced").ClickAsync();
        await Expect(Page.Locator("#vault-argon2id-memory")).ToHaveValueAsync("19");
        await Expect(Page.Locator("#vault-argon2id-key-length")).ToBeDisabledAsync();
        await Page.Locator("#vault-argon2id-memory").FillAsync("1");
        await Page.Locator("#vault-argon2id-memory").BlurAsync();
        await Expect(Page.Locator("#vault-argon2id-needs-benchmark")).ToBeVisibleAsync();
        await Expect(Page.Locator("#vault-create")).ToBeDisabledAsync();
        await ScreenshotAsync("NewVaultAdvanced");

        await Page.Locator("#vault-argon2id-benchmark").ClickAsync();
        await Expect(Page.Locator("#vault-argon2id-result")).ToBeVisibleAsync();
        await Expect(Page.Locator("#vault-create")).ToBeEnabledAsync();
        await Page.Locator("#vault-create").ClickAsync();

        await Expect(Page.Locator("#vault-page")).ToBeVisibleAsync();
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Personal");
    }

    [TestMethod]
    public async Task GivenAVault_WhenItsPassphraseIsChangedFromItsSettings_ThenSignedOutAndOnlyTheNewPassphraseOpensIt()
    {
        const string newPassphrase = "a brand new passphrase";
        await LoginAsync();
        var id = await CreateVaultAsync("Personal");
        await AddLoginAsync("Gmail", "bob", "hunter2hunter2");

        await Page.Locator("#vault-settings-button").ClickAsync();
        await Page.Locator("#vault-change-passphrase-button").ClickAsync();
        await Expect(Page.Locator("#vault-settings-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#e2e-tab-advanced")).ToBeVisibleAsync();
        await Page.Locator("#e2e-current-passphrase").FillAsync(Passphrase);
        await Page.Locator("#e2e-passphrase").FillAsync(newPassphrase);
        await Page.Locator("#e2e-confirm-passphrase").FillAsync(newPassphrase);
        await Page.Locator("#e2e-acknowledge").CheckAsync();
        await ScreenshotAsync("ChangePassphrase");
        await Page.Locator("#e2e-confirm").ClickAsync();
        await Expect(Page.Locator("#e2e-rotation-estimate")).ToBeVisibleAsync();
        await Page.Locator("#e2e-rotation-start").ClickAsync();

        // Changing the passphrase derives a new Argon2id key and encrypts every file again: seconds here, far longer than
        // a step on a busy CI runner.
        await ExpectLoginPageAsync(PassphraseChangeTimeoutMs);

        await LoginAsync();
        await Page.Locator($".vault-card[data-vault='{id}'] .vault-card-main").ClickAsync();
        await Page.Locator("#e2e-unlock-passphrase").FillAsync(Passphrase);
        await Page.Locator("#e2e-unlock-button").ClickAsync();
        await Expect(Page.Locator("#e2e-unlock-error")).ToBeVisibleAsync();
        await Page.Locator("#e2e-unlock-passphrase").FillAsync(newPassphrase);
        await Page.Locator("#e2e-unlock-button").ClickAsync();
        await Expect(Page.Locator("#e2e-unlock-panel")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#secrets .secret-row")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task GivenAVault_WhenDeletedFromItsSettings_ThenItIsGoneOnlyOnceItsNameIsTyped()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#vault-settings-button").ClickAsync();

        await Page.Locator("#vault-delete-confirmation").FillAsync("personal");
        await Page.Locator("#vault-delete-button").ClickAsync();
        await Expect(Page.Locator("#vault-settings-error")).ToHaveTextAsync("Type the vault's name exactly to delete it.");
        await Page.Locator("#vault-delete-confirmation").FillAsync("Personal");
        await Page.Locator("#vault-delete-button").ClickAsync();

        await Expect(Page.Locator("#no-vaults")).ToBeVisibleAsync();
        await Expect(Page.Locator("#toasts")).ToContainTextAsync("Personal deleted.");
    }

    [TestMethod]
    public async Task GivenAnOwnTemplate_WhenAddedInSettings_ThenTheGeneratorOffersIt()
    {
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();
        await Expect(Page.Locator("#generator-settings")).ToBeVisibleAsync();
        await Expect(Page.Locator("#generator-settings-length")).ToBeVisibleAsync();
        await Expect(Page.Locator("#generator-template-name")).ToHaveCountAsync(0);
        await Page.Locator("#generator-settings-memorable").ClickAsync();
        await Expect(Page.Locator("#generator-settings-length")).ToHaveCountAsync(0);
        await Page.Locator("#generator-template-name").FillAsync("Two words");
        await Page.Locator("#generator-template-pattern").FillAsync("{dict(adjective):initialupper}{dict(noun):initialupper}");
        await Page.Locator("#generator-template-try").ClickAsync();
        await Expect(Page.Locator("#generator-template-example")).ToContainTextAsync(new Regex(@"Example: [A-Z]\S+"));
        await Page.Locator("#generator-template-add").ClickAsync();
        await Expect(Page.Locator(".generator-template[data-template='Two words']")).ToBeVisibleAsync();
        await Page.Locator("#generator-settings-template").SelectOptionAsync("Two words");
        await Page.Locator("#generator-settings-save").ClickAsync();
        await Expect(Page.Locator("#toasts")).ToContainTextAsync("Password generator settings saved.");
        await ScreenshotAsync("GeneratorSettings");

        await CreateVaultAsync("Personal");
        await Page.Locator("#new-secret-button").ClickAsync();
        await Page.Locator(".secret-generate").ClickAsync();

        await Expect(Page.Locator("#generator-mode-memorable")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Page.Locator("#generator-template")).ToHaveValueAsync("Two words");
        await Expect(Page.Locator(".generated-password-text")).ToHaveTextAsync(new Regex(@"^[A-Z]\S+$"));
    }

    [TestMethod]
    public async Task GivenTheShell_WhenShown_ThenTheLogoIsClypsesPng()
    {
        await LoginAsync();

        await Expect(Page.Locator("img.brand-icon")).ToHaveAttributeAsync("src", "images/logo.png");
    }

    [TestMethod]
    public async Task GivenAnIdThatIsNoVault_WhenOpened_ThenItSaysSo()
    {
        await LoginAsync();

        await GoToAsync($"/vaults/{Guid.NewGuid():D}");

        await Expect(Page.Locator("#vault-not-found")).ToBeVisibleAsync();
    }

    /// <summary>
    /// Turns the mouse wheel once, waits for the page to stop scrolling (smooth scrolling animates it), and checks it
    /// moved by no more than the wheel asked for.
    /// </summary>
    private async Task WheelAsync(int deltaY)
    {
        var before = await Page.EvaluateAsync<double>("() => window.scrollY");
        await Page.Mouse.WheelAsync(0, deltaY);
        var after = await Page.EvaluateAsync<double>(
            """
            () => new Promise(resolve => {
                let last = window.scrollY;
                let still = 0;
                const check = () => {
                    still = window.scrollY === last ? still + 1 : 0;
                    last = window.scrollY;
                    if (still >= 10) { resolve(last); } else { setTimeout(check, 30); }
                };
                setTimeout(check, 30);
            })
            """);
        var moved = Math.Abs(after - before);
        Assert.IsTrue(moved <= Math.Abs(deltaY) + 1, $"One turn of the wheel ({deltaY}px) scrolled the page {after - before}px, from {before} to {after}.");
    }
}
