namespace Clypse.Portal.UITests;

/// <summary>
/// The UI tests use the placeholder sign-in with no AWS, so the keys are kept in the browser and there is no passkey
/// PRF. Keeping them in Secrets Manager, and unlocking with the passkey, are tested on a deployed portal.
/// </summary>
[TestClass]
public class E2eEncryptionTests : TestBase
{
    [TestMethod]
    public async Task GivenNoProfiles_WhenSignedIn_ThenNothingAsksForAPassphraseAndNoneAreListed()
    {
        // Clypse defines no sync profiles in code: every vault is a protected profile the user creates.
        await LoginAsync();

        await Expect(Page.Locator("#e2e-banner")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#e2e-lock-indicator")).ToHaveCountAsync(0);
        await Page.Locator("#nav-settings").ClickAsync();
        await Expect(Page.Locator("#e2e-no-created")).ToBeVisibleAsync();
        await Expect(Page.Locator("#e2e-app-profiles .e2e-profile")).ToHaveCountAsync(0);
        await ScreenshotAsync("EncryptionSection");
    }

    [TestMethod]
    public async Task GivenNoProtectedProfiles_WhenOneIsCreated_ThenItIsListedByIdAndCanBeDeleted()
    {
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();

        await Page.Locator("#e2e-new").ClickAsync();
        await EnterNewPassphraseAsync();
        await Expect(Page.Locator("#e2e-finished")).ToContainTextAsync("Created ");
        await ScreenshotAsync("Created");
        await CloseFinishedDialogAsync();

        var profile = Page.Locator("#e2e-created-profiles .e2e-profile");
        await Expect(profile).ToHaveCountAsync(1);
        var id = await profile.GetAttributeAsync("data-profile");
        Assert.IsTrue(Guid.TryParseExact(id, "D", out _), $"'{id}' is not a GUID");
        await Expect(profile.Locator(".e2e-profile-name")).ToHaveTextAsync(id!);
        await Expect(profile.Locator(".e2e-profile-state")).ToHaveTextAsync("Unlocked");
        await Expect(Page.Locator("#e2e-lock-indicator")).ToContainTextAsync("1 unlocked");

        await profile.Locator(".e2e-delete").ClickAsync();
        await Page.Locator("#e2e-delete-confirmation").FillAsync("not the id");
        await Page.Locator("#e2e-confirm").ClickAsync();
        await Expect(Page.Locator("#e2e-error")).ToHaveTextAsync("Type the id exactly to confirm.");
        await Page.Locator("#e2e-delete-confirmation").FillAsync(id!);
        await Page.Locator("#e2e-confirm").ClickAsync();
        await CloseFinishedDialogAsync();
        await Expect(Page.Locator("#e2e-no-created")).ToBeVisibleAsync();
        await Expect(Page.Locator("#e2e-lock-indicator")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task GivenAProtectedProfile_WhenReloaded_ThenItIsStillListedAndLocked()
    {
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();
        await Page.Locator("#e2e-new").ClickAsync();
        await EnterNewPassphraseAsync();
        await CloseFinishedDialogAsync();

        await ReloadAsync();

        var profile = Page.Locator("#e2e-created-profiles .e2e-profile");
        await Expect(profile).ToHaveCountAsync(1);
        await Expect(profile.Locator(".e2e-profile-state")).ToHaveTextAsync("Locked");
    }

    [TestMethod]
    public async Task GivenArgon2idConfigured_WhenOpeningSettings_ThenArgon2idPanelShowsTheConfiguredSettings()
    {
        // The UI tests run the portal in the Development environment, whose appsettings lighten the Release settings.
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();

        await Expect(Page.Locator("#argon2id-card")).ToBeVisibleAsync();
        await Expect(Page.Locator("#argon2id-memory")).ToHaveTextAsync("19 MiB");
        await Expect(Page.Locator("#argon2id-iterations")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#argon2id-parallelism")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#argon2id-key-length")).ToHaveTextAsync("32 bytes");
        await Expect(Page.Locator("#argon2id-benchmark")).ToBeEnabledAsync();
        await ScreenshotAsync("Argon2idPanel");
    }
}
