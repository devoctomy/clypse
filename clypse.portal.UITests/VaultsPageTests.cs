using Microsoft.Playwright;

namespace clypse.portal.UITests;

[TestClass]
public class VaultsPageTests : TestBase
{
    [TestInitialize]
    public async Task TestInitialize()
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

        // Wait for successful login and navigation to vaults page
        await Expect(Page.Locator("h1, h2, h3").Filter(new() { HasText = "Vaults" })).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [TestMethod]
    public async Task ShouldCreateUnlockAndDeleteVaultSuccessfully()
    {
        // Generate unique vault data for this test
        var vaultName = $"Test Vault {Guid.NewGuid()}";
        var vaultDescription = $"Test Description {Guid.NewGuid()}";
        var passphrase = "TestPassphrase123!";

        // STEP 1: Create Vault
        // Click the Create Vault button in the navigation
        await Page.Locator("#nav-create-vault-button").ClickAsync();

        // Wait for create vault form to be visible
        await Expect(Page.Locator("#create-vault-button")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Fill in the create vault form
        await Page.Locator("#vaultName").FillAsync(vaultName);
        await Page.Locator("#vaultDescription").FillAsync(vaultDescription);
        await Page.Locator("#vaultPassphrase").FillAsync(passphrase);
        await Page.Locator("#vaultPassphraseConfirm").FillAsync(passphrase);

        // Click the create button
        await Page.Locator("#create-vault-button").ClickAsync();

        // Verify the vault list container is visible
        await Expect(Page.Locator("#vaults-list")).ToBeVisibleAsync(new() { Timeout = 10000 });

        // Verify the vault card is visible
        var vaultCard = Page.Locator("#vaults-list .vault-card-responsive").Filter(new() { HasText = vaultDescription });
        await Expect(vaultCard).ToBeVisibleAsync(new() { Timeout = 5000 });

        // STEP 2: Unlock Vault
        // Click on the specific vault card by matching the description text we just created (unique GUID)
        await vaultCard.ClickAsync();

        // Verify unlock dialog is visible
        await Expect(Page.Locator(".modal").Filter(new() { HasText = "Unlock Vault" })).ToBeVisibleAsync();

        // Enter the passphrase
        await Page.Locator("#passphrase").FillAsync(passphrase);

        // Click unlock button
        await Page.Locator("#unlock-vault-button").ClickAsync();

        // Wait for unlock and navigation to credentials page
        await Expect(Page.Locator("h1, h2, h3").Filter(new() { HasText = "Credentials" }).Or(Page.Locator("h1, h2, h3").Filter(new() { HasText = "Vaults" }))).ToBeVisibleAsync(new() { Timeout = 15000 });

        // STEP 3: Delete Vault
        // Click Delete Vault button in navigation (should be available now that vault is unlocked)
        await Page.Locator("#nav-delete-vault-button").ClickAsync();

        // Verify delete confirmation dialog is visible
        await Expect(Page.Locator(".modal").Filter(new() { HasText = "Delete Vault" })).ToBeVisibleAsync();

        // Enter the vault name to confirm deletion
        await Page.Locator("#confirmName").FillAsync(vaultName);

        // Click the delete confirmation button
        await Page.Locator("#confirm-delete-vault-button").ClickAsync();

        // Wait for deletion to complete and return to vaults page
        await Expect(Page.Locator("h1, h2, h3").Filter(new() { HasText = "Vaults" })).ToBeVisibleAsync(new() { Timeout = 15000 });

        // Manually click the refresh button to ensure the vault list is updated
        await Page.Locator("#nav-refresh-button").ClickAsync();

        // Wait a moment for the refresh to complete
        await Task.Delay(2000, TestContext.CancellationTokenSource.Token);

        // Now check for the no vaults message
        var hasNoVaultsMessage = await Page.Locator("#no-vaults-found").IsVisibleAsync();
        Assert.IsTrue(hasNoVaultsMessage, "Should show no vaults message after deletion and refresh");
    }
}