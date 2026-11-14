using System.Text.RegularExpressions;

namespace Clypse.Portal.UITests;

/// <summary>
/// Vaults and their secrets, in the browser: each vault is a protected profile, kept (encrypted) in IndexedDB. The UI
/// tests have no AWS, so nothing is synced to S3.
/// </summary>
[TestClass]
public class VaultTests : TestBase
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
        await Expect(Page.Locator("#secrets-count")).ToHaveTextAsync("2 secrets");
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
    public async Task GivenAKeePassCsv_WhenImported_ThenItsLoginsAreAdded()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        var csv = Path.Combine(Path.GetTempPath(), $"clypse-import-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(csv, "\"Account\",\"Username\",\"Password\",\"Web site\",\"Comments\"\r\n\"Gmail\",\"bob\",\"hunter2\",\"https://mail.google.com\",\"\"\r\n\"Bank\",\"bob\",\"correct horse\",\"https://bank.example\",\"savings\"\r\n");
        try
        {
            await Page.Locator("#import-button").ClickAsync();
            await Page.Locator("#import-file").SetInputFilesAsync(csv);
            await Expect(Page.Locator("#import-found")).ToHaveTextAsync("2 secrets found.");
            await ScreenshotAsync("Import");
            await Page.Locator("#import-confirm").ClickAsync();
            await Expect(Page.Locator("#import-done")).ToContainTextAsync("2 secrets imported.");
            await Page.Locator("#import-close").ClickAsync();
        }
        finally
        {
            File.Delete(csv);
        }

        await Expect(Page.Locator("#secrets .secret-row")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task GivenManySecrets_WhenTheListIsScrolledWithTheWheel_ThenItMovesOneStepAtATimeBothWays()
    {
        // The list is virtualised: its spacers change height as rows load, and the browser's scroll anchoring used to
        // chase them, so one turn of the wheel ran to the end of the list (or back to the start).
        const int count = 200;
        const int step = 400;
        await LoginAsync();
        await CreateVaultAsync("Personal");
        var csv = Path.Combine(Path.GetTempPath(), $"clypse-import-{Guid.NewGuid():N}.csv");
        var lines = Enumerable.Range(0, count).Select(i => $"\"Site {i:D4}\",\"user{i}\",\"password{i}\",\"https://site{i}.example\",\"\"");
        await File.WriteAllTextAsync(csv, "\"Account\",\"Username\",\"Password\",\"Web site\",\"Comments\"\r\n" + string.Join("\r\n", lines) + "\r\n");
        try
        {
            await Page.Locator("#import-button").ClickAsync();
            await Page.Locator("#import-file").SetInputFilesAsync(csv);
            await Page.Locator("#import-confirm").ClickAsync();
            await Expect(Page.Locator("#import-done")).ToContainTextAsync($"{count} secrets imported.");
            await Page.Locator("#import-close").ClickAsync();
        }
        finally
        {
            File.Delete(csv);
        }

        await Expect(Page.Locator("#secrets-count")).ToHaveTextAsync($"{count} secrets");
        var lastRow = Page.Locator("#secrets .secret-name", new() { HasTextString = $"Site {count - 1:D4}" });
        var firstRow = Page.Locator("#secrets .secret-name", new() { HasTextString = "Site 0000" });
        var row = await Page.Locator("#secrets .secret-row").First.BoundingBoxAsync();
        await Page.Mouse.MoveAsync(row!.X + (row.Width / 2), row.Y + (row.Height / 2));

        var steps = 0;
        while (await lastRow.CountAsync() == 0)
        {
            await WheelAsync(step);
            Assert.IsTrue(++steps < count, "The end of the list was never reached.");
        }

        await Expect(firstRow).ToHaveCountAsync(0);
        while (await firstRow.CountAsync() == 0)
        {
            await WheelAsync(-step);
            Assert.IsTrue(++steps < count * 2, "The start of the list was never reached again.");
        }

        await Expect(lastRow).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task GivenALogin_WhenItsPasswordIsCopiedFromTheList_ThenItIsOnTheClipboard_AndAToastSaysSo()
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"], new() { Origin = ServerUrl });
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await AddLoginAsync("Gmail", "bob", "hunter2hunter2");

        await Page.Locator(".secret-quick-copy[data-field='Password']").ClickAsync();

        await Expect(Page.Locator("#toasts .bw-toast")).ToContainTextAsync("Password copied. It is cleared from the clipboard in 30 seconds.");
        Assert.AreEqual("hunter2hunter2", await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"));
        await ScreenshotAsync("Copied");

        await Page.Locator(".secret-quick-copy[data-field='UserName']").ClickAsync();
        await Expect(Page.Locator("#toasts")).ToContainTextAsync("User name copied.");
        Assert.AreEqual("bob", await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"));
    }

    [TestMethod]
    public async Task GivenClipboardClearingSetInSettings_WhenAPasswordIsCopied_ThenTheSettingIsUsed()
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"], new() { Origin = ServerUrl });
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();
        await Page.Locator("#clipboard-settings-after").SelectOptionAsync("60");
        await Page.Locator("#clipboard-settings-save").ClickAsync();
        await Expect(Page.Locator("#toasts")).ToContainTextAsync("Clipboard settings saved.");
        await CreateVaultAsync("Personal");
        await AddLoginAsync("Gmail", "bob", "hunter2hunter2");

        await Page.Locator(".secret-quick-copy[data-field='Password']").ClickAsync();
        await Expect(Page.Locator("#toasts")).ToContainTextAsync("Password copied. It is cleared from the clipboard in 60 seconds.");

        await Page.Locator("#nav-settings").ClickAsync();
        await Page.Locator("#clipboard-settings-clear").UncheckAsync();
        await Expect(Page.Locator("#clipboard-settings-after")).ToHaveCountAsync(0);
        await Page.Locator("#clipboard-settings-save").ClickAsync();
        await Page.Locator("#nav-vaults").ClickAsync();
        await Page.Locator(".vault-card .vault-card-main").First.ClickAsync();
        await Page.Locator(".secret-quick-copy[data-field='Password']").ClickAsync();

        var toast = Page.Locator("#toasts .bw-toast").Last;
        await Expect(toast).ToContainTextAsync("Password copied.");
        await Expect(toast).Not.ToContainTextAsync("cleared");
        Assert.AreEqual("hunter2hunter2", await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"));
    }

    [TestMethod]
    public async Task GivenTheSecretEditor_WhenTheUserNameIsCopied_ThenItIsOnTheClipboard()
    {
        await Context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"], new() { Origin = ServerUrl });
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#new-secret-button").ClickAsync();
        await Page.Locator("#secret-field-UserName").FillAsync("bob");

        await Page.Locator(".secret-copy[data-field='UserName']").ClickAsync();

        await Expect(Page.Locator("#toasts")).ToContainTextAsync("User name copied.");
        Assert.AreEqual("bob", await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()"));
    }

    [TestMethod]
    public async Task GivenAnAwsSecret_WhenEdited_ThenItsSecretAccessKeyIsHiddenUntilShown()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#new-secret-button").ClickAsync();

        await Page.Locator(".secret-type-option[data-type='Aws']").ClickAsync();

        await Expect(Page.Locator("#secret-field-SecretAccessKey")).ToHaveAttributeAsync("type", "password");
        await Expect(Page.Locator("#secret-field-AccessKeyId")).ToHaveAttributeAsync("type", "text");
        await Expect(Page.Locator(".secret-generate")).ToHaveCountAsync(0);
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
