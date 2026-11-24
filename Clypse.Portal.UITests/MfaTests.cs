namespace Clypse.Portal.UITests;

/// <summary>
/// Two-step verification with an authenticator app, which each user turns on or not: off until they turn it on in the
/// Profile page, then the code step at sign in. Run against the placeholder sign in, which accepts any six-digit code;
/// the Cognito calls, and remembered devices, are covered by blazwiz's own tests.
/// </summary>
[TestClass]
public class MfaTests : TestBase
{
    [TestMethod]
    public async Task GivenNewUser_WhenSigningIn_ThenNoCodeIsAskedFor_AndTwoStepVerificationIsOff()
    {
        // Arrange
        await LoginAsync();

        // Act
        await Page.Locator("#nav-profile").ClickAsync();

        // Assert
        await Expect(Page.Locator("#profile-mfa-enable")).ToBeVisibleAsync();
        await Expect(Page.Locator("#profile-mfa-disable")).ToHaveCountAsync(0);
        await ScreenshotAsync("Off");
    }

    [TestMethod]
    public async Task GivenSignedInUser_WhenTurningOnTwoStepVerification_ThenTheSetUpShowsEveryWayToAddTheApp()
    {
        // Arrange
        await LoginAsync();
        await Page.Locator("#nav-profile").ClickAsync();

        // Act
        await Page.Locator("#profile-mfa-enable").ClickAsync();

        // Assert
        await Expect(Page.Locator("#profile-mfa-qr")).ToBeVisibleAsync();
        await Expect(Page.Locator("#profile-mfa-open-app")).ToHaveAttributeAsync("href", new System.Text.RegularExpressions.Regex("^otpauth://totp/"));
        await Expect(Page.Locator("#profile-mfa-secret")).ToBeVisibleAsync();
        await ScreenshotAsync("SetUp");
    }

    [TestMethod]
    public async Task GivenTwoStepVerificationTurnedOn_WhenSigningInAgain_ThenTheCodeIsAskedFor()
    {
        // Arrange
        await LoginAsync();
        await TurnOnTwoStepVerificationAsync();
        await Page.Locator("#logout-button").ClickAsync();
        await ExpectLoginPageAsync();

        // Act
        await SignInWithPasswordAsync();
        await Expect(Page.Locator("#mfa-code")).ToBeVisibleAsync();
        await ScreenshotAsync("CodeStep");
        await Page.Locator("#mfa-code").FillAsync("654321");
        await Page.Locator("#mfa-button").ClickAsync();

        // Assert
        await Expect(Page.Locator("#vaults-page")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task GivenTwoStepVerificationOn_WhenTurningItOff_ThenSigningInAsksForNoCode()
    {
        // Arrange
        await LoginAsync();
        await TurnOnTwoStepVerificationAsync();

        // Act
        await Page.Locator("#profile-mfa-disable").ClickAsync();
        await Page.Locator("#mfa-disable-confirm").ClickAsync();

        // Assert
        await Expect(Page.Locator("#profile-mfa-enable")).ToBeVisibleAsync();
        await Page.Locator("#logout-button").ClickAsync();
        await LoginAsync();
    }

    private async Task TurnOnTwoStepVerificationAsync()
    {
        await Page.Locator("#nav-profile").ClickAsync();
        await Page.Locator("#profile-mfa-enable").ClickAsync();
        await Page.Locator("#profile-mfa-code").FillAsync("123456");
        await Page.Locator("#profile-mfa-confirm").ClickAsync();
        await Expect(Page.Locator("#profile-mfa-done")).ToBeVisibleAsync();
        await Expect(Page.Locator("#profile-mfa-disable")).ToBeVisibleAsync();
    }

    private async Task SignInWithPasswordAsync()
    {
        await Page.Locator("#username").FillAsync("bob.hoskins@example.com");
        await Page.Locator("#password").FillAsync("password123");
        await Page.Locator("#login-button").ClickAsync();
    }
}
