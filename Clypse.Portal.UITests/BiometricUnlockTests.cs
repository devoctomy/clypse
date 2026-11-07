namespace Clypse.Portal.UITests;

/// <summary>
/// Unlocking a vault with biometrics, with Chromium's virtual authenticator standing in for the device's passkey (with
/// the PRF extension, which the keys are derived from).
/// </summary>
[TestClass]
public class BiometricUnlockTests : TestBase
{
    [TestMethod]
    public async Task GivenARememberedUserWithAPasskey_WhenBiometricsAreTurnedOnForAVault_ThenItOpensWithThemFromTheVaultsPage_UntilForgotten()
    {
        await AddVirtualAuthenticatorAsync();
        await LoginRememberedWithPasskeyAsync("bio.user@example.com");
        var id = await CreateVaultAsync("Personal");
        var card = Page.Locator($".vault-card[data-vault='{id}']");

        // Turned on when unlocking with the passphrase.
        await Page.Locator("#lock-vault-button").ClickAsync();
        await card.Locator(".vault-card-main").ClickAsync();
        await Page.Locator("#e2e-unlock-passphrase").FillAsync(Passphrase);
        await Page.Locator("#e2e-unlock-remember-biometrics").CheckAsync();
        await ScreenshotAsync("RememberBiometrics");
        await Page.Locator("#e2e-unlock-button").ClickAsync();
        await Expect(Page.Locator("#e2e-unlock-panel")).ToHaveCountAsync(0);

        // Locked, the vault shows the fingerprint, and opens straight onto its secrets.
        await Page.Locator("#lock-vault-button").ClickAsync();
        await Expect(card.Locator(".bi-fingerprint")).ToBeVisibleAsync();
        await Expect(card.Locator(".vault-card-forget")).ToBeVisibleAsync();
        await ScreenshotAsync("BiometricCard");
        await card.Locator(".vault-card-main").ClickAsync();
        await Expect(Page.Locator("#vault-page")).ToBeVisibleAsync();
        await Expect(Page.Locator("#e2e-unlock-panel")).ToHaveCountAsync(0);

        // After a reload the passkey's key is gone from memory: the passkey is asked for again.
        await ReloadAsync();
        await Page.Locator("#nav-vaults").ClickAsync();
        await Expect(card.Locator(".bi-fingerprint")).ToBeVisibleAsync();
        await card.Locator(".vault-card-main").ClickAsync();
        await Expect(Page.Locator("#vault-page")).ToBeVisibleAsync();
        await Expect(Page.Locator("#e2e-unlock-panel")).ToHaveCountAsync(0);

        // Forgotten: back to the passphrase.
        await Page.Locator("#lock-vault-button").ClickAsync();
        await card.Locator(".vault-card-forget").ClickAsync();
        await Expect(card.Locator(".bi-fingerprint")).ToHaveCountAsync(0);
        await Expect(card.Locator(".vault-card-forget")).ToHaveCountAsync(0);
        await card.Locator(".vault-card-main").ClickAsync();
        await Expect(Page.Locator("#e2e-unlock-panel")).ToBeVisibleAsync();
        await Expect(Page.Locator("#e2e-unlock-passkey-button")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#e2e-unlock-remember-biometrics")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task GivenAPasswordSignInWithoutRememberMe_WhenAVaultIsLocked_ThenBiometricsAreNotOffered()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");

        await Page.Locator("#lock-vault-button").ClickAsync();
        await Page.Locator(".vault-card .vault-card-main").First.ClickAsync();

        await Expect(Page.Locator("#e2e-unlock-panel")).ToBeVisibleAsync();
        await Expect(Page.Locator("#e2e-unlock-remember-biometrics")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#e2e-unlock-passkey-button")).ToHaveCountAsync(0);
    }

    private async Task AddVirtualAuthenticatorAsync()
    {
        var cdp = await Context.NewCDPSessionAsync(Page);
        await cdp.SendAsync("WebAuthn.enable");
        await cdp.SendAsync("WebAuthn.addVirtualAuthenticator", new Dictionary<string, object>
        {
            ["options"] = new Dictionary<string, object>
            {
                ["protocol"] = "ctap2",
                ["transport"] = "internal",
                ["hasResidentKey"] = true,
                ["hasUserVerification"] = true,
                ["isUserVerified"] = true,
                ["hasPrf"] = true,
                ["automaticPresenceSimulation"] = true,
            },
        });
    }

    private async Task LoginRememberedWithPasskeyAsync(string username)
    {
        await GoToLoginAsync();
        await Page.Locator("#username").FillAsync(username);
        await Page.Locator("#password").FillAsync("password123");
        await Page.Locator("#remember-me").CheckAsync();
        await Page.Locator("#login-button").ClickAsync();
        await Page.Locator("#webauthn-enable").ClickAsync();
        await Expect(Page.Locator("#vaults-page")).ToBeVisibleAsync();
    }
}
