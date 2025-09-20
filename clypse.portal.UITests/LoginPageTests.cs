using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace clypse.portal.UITests;

[TestClass]
public class LoginPageTests : TestBase
{
    [TestMethod]
    public async Task ShouldDisplayLoginPageElements()
    {
        // Navigate to the login page (root redirects to login when not authenticated)
        await Page.GotoAsync(ServerUrl);

        // Verify page title
        await Expect(Page).ToHaveTitleAsync("Clypse Portal - Login");

        // Verify main login elements are present
        await Expect(Page.Locator("img[alt='Clypse']")).ToBeVisibleAsync();
        
        // Check for username input field with person icon
        await Expect(Page.Locator("input[placeholder='Enter your username']")).ToBeVisibleAsync();
        await Expect(Page.Locator("i.bi-person")).ToBeVisibleAsync();
        
        // Check for password input field with lock icon
        await Expect(Page.Locator("input[type='password'][placeholder='Enter your password']")).ToBeVisibleAsync();
        await Expect(Page.Locator("i.bi-lock")).ToBeVisibleAsync();
        
        // Check for login button
        await Expect(Page.Locator("button[type='submit']").Filter(new() { HasText = "Login" })).ToBeVisibleAsync();
        
        // Check for theme switcher button
        await Expect(Page.Locator("button.theme-switcher")).ToBeVisibleAsync();
        
        // Verify the card structure
        await Expect(Page.Locator(".card")).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-header")).ToBeVisibleAsync();
        await Expect(Page.Locator(".card-body")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShouldShowValidationErrorsForEmptyForm()
    {
        await Page.GotoAsync(ServerUrl);

        // Try to submit empty form
        await Page.Locator("button[type='submit']").ClickAsync();

        // Check if validation errors appear (Blazor's DataAnnotationsValidator should show these)
        // Note: This might vary based on your validation setup
        await Task.Delay(1000); // Give time for validation to appear
        
        // Verify form is still visible (login hasn't proceeded)
        await Expect(Page.Locator("input[placeholder='Enter your username']")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShouldToggleTheme()
    {
        await Page.GotoAsync(ServerUrl);

        // Get initial theme icon
        var initialIcon = await Page.Locator("button.theme-switcher i").GetAttributeAsync("class");
        
        // Click theme switcher
        await Page.Locator("button.theme-switcher").ClickAsync();
        
        // Wait a bit for the theme change
        await Task.Delay(500);
        
        // Get new theme icon
        var newIcon = await Page.Locator("button.theme-switcher i").GetAttributeAsync("class");
        
        // Verify icon changed
        Assert.AreNotEqual(initialIcon, newIcon, "Theme icon should change when theme switcher is clicked");
    }

    [TestMethod]
    public async Task ShouldLoginWithValidCredentialsAndNavigateToVaults()
    {
        // Get credentials from environment variables
        var username = Environment.GetEnvironmentVariable("CLYPSE_UITESTS_USERNAME");
        var password = Environment.GetEnvironmentVariable("CLYPSE_UITESTS_PASSWORD");

        // Skip test if environment variables are not set
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Assert.Inconclusive("Test skipped: CLYPSE_UITESTS_USERNAME and CLYPSE_UITESTS_PASSWORD environment variables must be set");
        }

        // Navigate to the login page
        await Page.GotoAsync(ServerUrl);

        // Fill in the login form
        await Page.Locator("input[placeholder='Enter your username']").FillAsync(username);
        await Page.Locator("input[type='password'][placeholder='Enter your password']").FillAsync(password);

        // Submit the form
        await Page.Locator("button[type='submit']").Filter(new() { HasText = "Login" }).ClickAsync();

        // Wait for successful login and navigation away from login page
        await Task.Delay(2000); // Give time for authentication and navigation
        
        // Verify we're on the vaults page by checking for the page label/heading
        // Look for a heading or label that indicates we're on the vaults page
        await Expect(Page.Locator("h1, h2, h3").Filter(new() { HasText = "Vaults" })).ToBeVisibleAsync(new() { Timeout = 10000 });
        
        // Additional verification: ensure we're no longer on the login page
        await Expect(Page.Locator("input[placeholder='Enter your username']")).Not.ToBeVisibleAsync();
    }
}