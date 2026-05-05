using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace clypse.portal.UITests;

/// <summary>
/// UI tests that verify the login page behaves correctly when the Cognito auth service
/// fails to initialise on startup.
///
/// Each test in this class requires <c>appsettings.json</c> to contain empty/invalid
/// Cognito configuration so that <c>CognitoAuth.initialize</c> throws in the browser.
/// <see cref="ClassInitialize"/> temporarily replaces the file with a bad-config copy and
/// <see cref="ClassCleanup"/> restores it afterwards, ensuring the change is invisible to
/// all other test classes regardless of execution order.
/// </summary>
[TestClass]
public class LoginPageInitFailureTests : TestBase
{
    private static string? _originalAppSettings;
    private static string? _appSettingsPath;

    // appsettings.json content with deliberately empty Cognito values so that the
    // Amazon Cognito Identity JS SDK throws "Both UserPoolId and ClientId are required."
    // during CognitoAuth.initialize, triggering the InitializationFailed path.
    private const string BadCognitoAppSettings = """
        {
          "AwsS3": {
            "BucketName": "",
            "Region": ""
          },
          "AwsCognito": {
            "UserPoolId": "",
            "UserPoolClientId": "",
            "Region": "",
            "IdentityPoolId": ""
          },
          "AppSettings": {
            "EnablePortalLoginAuthn": true,
            "ApplicationTitle": "Clypse Testing Portal",
            "CopyrightMessage": "\u00A9 2025 - 2026 Clypse Testing Portal. All rights reserved.",
            "ShowLogoInTitleBar": true,
            "MemorablePasswordTemplates": [
              {
                "Name": "Default",
                "Template": "{dict(adjective):random}{randstr(0123456789,2):random}{dict(verb):random}{randstr(!-=_,1)}{dict(verb):random}"
              }
            ],
            "Deployment": {
              "DeployedBy": "NickDevoctomy",
              "DeployedAt": "Unknown",
              "DeploymentActionUrl": "https://github.com/devoctomy/clypse"
            }
          }
        }
        """;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        _appSettingsPath = GetAppsettingsJsonPath();
        _originalAppSettings = await File.ReadAllTextAsync(_appSettingsPath);
        await File.WriteAllTextAsync(_appSettingsPath, BadCognitoAppSettings);

        // Give the static-file server a moment to pick up the modified file before
        // the first test navigates to the page.
        await Task.Delay(500);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        if (_appSettingsPath != null && _originalAppSettings != null)
        {
            await File.WriteAllTextAsync(_appSettingsPath, _originalAppSettings);
        }
    }

    [TestMethod]
    public async Task ShouldShowInitialisationErrorAndHideLoginFormWhenCognitoInitFails()
    {
        // Navigate to root — the app redirects to login when not authenticated
        await Page.GotoAsync(ServerUrl);

        // Wait for the Blazor WASM app to finish loading
        await Expect(Page.Locator(".card-body")).ToBeVisibleAsync();
        await ScreenshotAfterNavigationAsync("LoginPageLoaded");

        // Verify the initialisation error is displayed
        var errorBox = Page.Locator(".init-failure-error");
        await Expect(errorBox).ToBeVisibleAsync();
        var errorText = await errorBox.InnerTextAsync();
        Assert.IsTrue(
            errorText.Contains("Cognito"),
            $"Expected error message to mention 'Cognito', but got: '{errorText}'");
        await ScreenshotAfterActionAsync("InitFailureErrorVisible");

        // Verify the login form fields are hidden
        await Expect(Page.Locator("input[placeholder='Enter your username']")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("input[type='password'][placeholder='Enter your password']")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("button[type='submit']").Filter(new() { HasText = "Login" })).Not.ToBeVisibleAsync();

        await ScreenshotEndOfScenarioAsync();
    }
}
