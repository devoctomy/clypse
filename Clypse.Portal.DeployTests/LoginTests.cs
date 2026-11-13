namespace Clypse.Portal.DeployTests;

[TestClass]
public class LoginTests : DeployTestBase
{
    [TestMethod]
    public async Task GivenTestUser_WhenSigningIn_ThenTheVaultsPageIsShown()
    {
        // Arrange
        await GoToLoginAsync();

        // Act
        await SubmitLoginAsync(Settings.Password);

        // Assert
        await Expect(Page.Locator("#vaults-page")).ToBeVisibleAsync();
        await Expect(Page.Locator("#login-button")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task GivenTestUser_WhenSigningInWithAWrongPassword_ThenAnErrorIsShown()
    {
        // Arrange
        await GoToLoginAsync();

        // Act
        await SubmitLoginAsync(Settings.Password + "-wrong");

        // Assert
        await Expect(Page.Locator("#login-error")).ToBeVisibleAsync();
        await Expect(Page.Locator("#login-error")).Not.ToBeEmptyAsync();
        await ExpectLoginPageAsync();
    }

    [TestMethod]
    public async Task GivenSignedInTestUser_WhenSigningOut_ThenTheLoginPageIsShown()
    {
        // Arrange
        await LoginAsync();

        // Act
        await Page.Locator("#logout-button").ClickAsync();

        // Assert
        await ExpectLoginPageAsync();
    }
}
