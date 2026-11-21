namespace Clypse.Portal.UITests;

/// <summary>
/// The UI tests use the placeholder sign-in with no AWS, so the keys are kept in the browser and there is no passkey
/// PRF. Keeping them in Secrets Manager, and unlocking with the passkey, are tested on a deployed portal.
/// </summary>
[TestClass]
public class E2eEncryptionTests : TestBase
{
    [TestMethod]
    public async Task GivenNoVaults_WhenSignedIn_ThenNothingAsksForAPassphrase_AndSettingsHasNoEncryptionSection()
    {
        // Clypse defines no sync profiles in code: every vault is a protected profile the user creates. Vaults are
        // created, locked, renamed, deleted and given a new passphrase on clypse's own pages, so the framework's End to
        // end encryption section is taken off the Settings page.
        await LoginAsync();

        await Expect(Page.Locator("#e2e-banner")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#e2e-lock-indicator")).ToHaveCountAsync(0);
        await Page.Locator("#nav-settings").ClickAsync();
        await Expect(Page.Locator("#sync-card")).ToBeVisibleAsync();
        await Expect(Page.Locator("#e2e-section")).ToHaveCountAsync(0);
        await ScreenshotAsync("Settings");
    }

    [TestMethod]
    public async Task GivenAVault_WhenReloaded_ThenItIsStillListedAndLocked()
    {
        await LoginAsync();
        var id = await CreateVaultAsync("Personal");
        await Expect(Page.Locator("#e2e-lock-indicator")).ToContainTextAsync("1 unlocked");

        await ReloadAsync();
        await Page.Locator("#nav-vaults").ClickAsync();

        var card = Page.Locator($".vault-card[data-vault='{id}']");
        await Expect(card).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-locked"));
        await Expect(Page.Locator("#e2e-lock-indicator")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task GivenArgon2idConfigured_WhenOpeningSettings_ThenArgon2idPanelShowsTheConfiguredSettings()
    {
        // The UI tests run the portal in the Development environment, whose appsettings lighten the Release settings.
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();

        await Expect(Page.Locator("#argon2id-card")).ToBeVisibleAsync();
        await Expect(Page.Locator("#argon2id-memory")).ToHaveValueAsync("19");
        await Expect(Page.Locator("#argon2id-iterations")).ToHaveValueAsync("2");
        await Expect(Page.Locator("#argon2id-parallelism")).ToHaveValueAsync("1");
        await Expect(Page.Locator("#argon2id-key-length")).ToHaveValueAsync("32");
        await Expect(Page.Locator("#argon2id-benchmark")).ToBeEnabledAsync();
        await ScreenshotAsync("Argon2idPanel");
    }
}
