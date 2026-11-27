using System.Text.Json.Nodes;

namespace Clypse.Portal.UITests;

/// <summary>
/// Runs the app with every text from resources pseudo-localised (<c>Text:PseudoLocalization</c>, e.g.
/// <c>[!! Śéţţîñĝš !!]</c>) and checks that all visible framework text is: anything else is hard-coded, so it cannot be
/// reworded or translated.
/// </summary>
[TestClass]
public class PseudoLocalizationTests : TestBase
{
    private const string Username = "bob.hoskins@example.com";

    // The signed-in user's name and initials, worked out from the email address: data, not text.
    private static readonly string[] UserText = [Username, "Bob Hoskins", "BH"];

    // What the user typed or made (vault and secret names, tags, a generated password), which clypse marks with
    // data-user-content: data, not text.
    private const string UserContent = "[data-user-content]";

    // Filled in as the settings files are served. They are fetched at the same time, so the handlers run on different
    // threads: lock while adding or reading.
    private readonly List<string> appSettingsText = [];

    [TestInitialize]
    public async Task UsePseudoLocalization()
    {
        // The app reads Text:PseudoLocalization from its settings (wwwroot/appsettings*.json): add it as they are served,
        // and note the app's own text in them (title, description, copyright), which is not the framework's.
        await Context.RouteAsync("**/appsettings*.json", async route =>
        {
            var response = await route.FetchAsync();
            var settings = JsonNode.Parse(await response.TextAsync())!.AsObject();
            settings["Text"] = new JsonObject { ["PseudoLocalization"] = true };
            if (settings["AppSettings"] is JsonObject app)
            {
                lock (appSettingsText)
                {
                    appSettingsText.AddRange(app.Select(p => p.Value?.ToString() ?? string.Empty).Where(v => v.Length > 0));
                }
            }

            await route.FulfillAsync(new() { Response = response, Body = settings.ToJsonString() });
        });
    }

    [TestMethod]
    public async Task GivenPseudoLocalization_WhenOpeningTheLoginPage_ThenAllTextIsFromResources()
    {
        // Act
        await GoToLoginAsync();

        // Assert
        await AssertNoHardCodedTextAsync("login page");
    }

    [TestMethod]
    public async Task GivenPseudoLocalization_WhenResettingAForgottenPassword_ThenAllTextIsFromResources()
    {
        // Arrange
        await GoToAsync("/forgot-password");
        await Expect(Page.Locator("#forgot-password-title")).ToContainTextAsync("[!!");
        await AssertNoHardCodedTextAsync("forgot password page");

        // Act
        await Page.Locator("#reset-email").FillAsync(Username);
        await Page.Locator("#send-code-button").ClickAsync();

        // Assert
        await Expect(Page.Locator("#reset-intro")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("reset password form");
    }

    [TestMethod]
    public async Task GivenPseudoLocalization_WhenOpeningEachFrameworkPage_ThenAllTextIsFromResources()
    {
        // Arrange
        await LoginAsync(Username);

        // Act & Assert: the shell (sidebar, top bar, banners) is checked on every framework page.
        await Page.Locator("#nav-settings").ClickAsync();
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("[!!");
        await AssertNoHardCodedTextAsync("settings page");

        await GoToAsync("/profile");
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("[!!");
        await AssertNoHardCodedTextAsync("profile page");

        await Page.Locator("#nav-sync").ClickAsync();
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("[!!");
        await AssertNoHardCodedTextAsync("sync page");
    }

    [TestMethod]
    public async Task GivenPseudoLocalization_WhenUsingAVault_ThenAllTextIsFromResources()
    {
        // Arrange
        await LoginAsync(Username);

        // Act & Assert: clypse's own pages and dialogs.
        await Expect(Page.Locator("#no-vaults")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("vaults page");

        await Page.Locator("#create-first-vault-button").ClickAsync();
        await Expect(Page.Locator("#vault-dialog")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("new vault dialog");

        await Page.Locator("#vault-name").FillAsync("Personal");
        await Page.Locator("#vault-passphrase").FillAsync(Passphrase);
        await Page.Locator("#vault-confirm-passphrase").FillAsync(Passphrase);
        await Page.Locator("#vault-acknowledge").CheckAsync();
        await Page.Locator("#vault-create").ClickAsync();
        await Expect(Page.Locator("#no-secrets")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("empty vault page", "Personal");

        await Page.Locator("#new-secret-button").ClickAsync();
        await Page.Locator("#secret-field-Password").FillAsync("hunter2hunter2");
        await AssertNoHardCodedTextAsync("secret editor", "Personal");

        await Page.Locator(".secret-generate").ClickAsync();
        await Expect(Page.Locator("#generator-dialog")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("password generator", "Personal");
        await Page.Locator("#generator-mode-memorable").ClickAsync();
        await AssertNoHardCodedTextAsync("memorable password generator", "Personal");
        await Page.Locator("#generator-use").ClickAsync();

        await Page.Locator("#secret-field-Name").FillAsync("Gmail");
        await Page.Locator("#secret-field-Tags").FillAsync("email");
        await Page.Locator("#secret-save").ClickAsync();
        await Expect(Page.Locator("#secrets .secret-row")).ToHaveCountAsync(1);
        await AssertNoHardCodedTextAsync("vault page", "Personal");

        await Page.Locator("#import-button").ClickAsync();
        await Expect(Page.Locator("#import-dialog")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("import dialog", "Personal");
    }

    [TestMethod]
    public async Task GivenPseudoLocalization_WhenUsingAVaultsFiles_ThenAllTextIsFromResources()
    {
        // Arrange
        await LoginAsync(Username);
        await CreateVaultAsync("Personal");

        // Act & Assert: the Files tab, its dialogs and the editor.
        await Page.Locator("#vault-tab-files").ClickAsync();
        await Expect(Page.Locator("#no-files")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("empty files tab", "Personal");

        await Page.Locator("#new-file-button").ClickAsync();
        await Expect(Page.Locator("#new-file-menu")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("new menu", "Personal");

        await Page.Locator("#new-folder").ClickAsync();
        await Expect(Page.Locator("#file-name-dialog")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("new folder dialog", "Personal");
        await Page.Locator("#file-name-save").ClickAsync();
        await Expect(Page.Locator("#file-name-error")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("new folder dialog without a name", "Personal");
        await Page.Locator("#file-name-input").FillAsync("Travel");
        await Page.Locator("#file-name-save").ClickAsync();
        await Expect(Page.Locator("#files .file-item[data-folder='Travel']")).ToBeVisibleAsync();

        var image = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
        await Page.Locator("#add-files-input").SetInputFilesAsync(new FilePayload { Name = "passport.png", MimeType = "image/png", Buffer = image });
        var file = Page.Locator("#files .file-item[data-file]");
        await Expect(file).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("files tab", "Personal");

        await Page.Locator("#file-search").FillAsync("nothing like it");
        await Expect(Page.Locator("#no-file-matches")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("files search with no matches", "Personal");
        await Page.Locator("#file-search").FillAsync(string.Empty);

        await file.Locator(".file-row").ClickAsync();
        await Expect(Page.Locator("#file-viewer-image")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("file viewer", "Personal");

        await Page.Locator("#file-viewer-details").ClickAsync();
        await Expect(Page.Locator("#file-details-dialog")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("file details dialog", "Personal");
        await Page.Locator("#file-details-dialog .btn-secondary").ClickAsync();

        await file.Locator(".file-delete").ClickAsync();
        await Expect(Page.Locator("#file-delete-dialog")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("delete file dialog", "Personal");
        await Page.Locator("#file-delete-no").ClickAsync();

        await Page.Locator("#new-file-button").ClickAsync();
        await Page.Locator("#new-markdown").ClickAsync();
        await Expect(Page.Locator("#file-name-dialog")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("new markdown document dialog", "Personal");
        await Page.Locator("#file-name-input").FillAsync("Notes");
        await Page.Locator("#file-name-save").ClickAsync();
        await Expect(Page.Locator("#text-editor")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("markdown editor", "Personal");

        await Page.Locator("#text-editor").FillAsync("# Title");
        await Expect(Page.Locator("#editor-preview h1")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("markdown editor with unsaved changes", "Personal");
        await Page.Locator("#editor-save-button").ClickAsync();
        await Expect(Page.Locator("#editor-save-button")).ToBeDisabledAsync();
        await AssertNoHardCodedTextAsync("markdown editor once saved", "Personal");

        await Page.Locator("#text-editor").FillAsync("# Title\n\nMore.");
        await Page.Locator("#editor-close-button").ClickAsync();
        await Expect(Page.Locator("#editor-leave-dialog")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("unsaved changes dialog", "Personal");
        await Page.Locator("#editor-leave-discard").ClickAsync();
        await Expect(Page.Locator("#vault-files")).ToBeVisibleAsync();

        await Page.Locator("#new-file-button").ClickAsync();
        await Page.Locator("#new-text").ClickAsync();
        await Expect(Page.Locator("#file-name-dialog")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("new text file dialog", "Personal");
        await Page.Locator("#file-name-input").FillAsync("Codes");
        await Page.Locator("#file-name-save").ClickAsync();
        await Expect(Page.Locator("#text-editor")).ToBeVisibleAsync();
        await AssertNoHardCodedTextAsync("text editor", "Personal");
    }

    // Version numbers, such as the framework's 0.1.0-preview.1: data, not text.
    private static bool IsVersion(string text) =>
        System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+\.\d+\.\d+(\.\d+)?(-[0-9A-Za-z.]+)?$");

    // Dates and times are formatted by the culture, so they are already translated: data, not text.
    private static bool IsDateOrTime(string text) =>
        DateTime.TryParse(text, System.Globalization.CultureInfo.GetCultureInfo("en-GB"), System.Globalization.DateTimeStyles.AllowWhiteSpaces, out _) ||
        DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out _);

    private async Task AssertNoHardCodedTextAsync(string where, params string[] data)
    {
        // Let the page settle: some text is filled in after the first render.
        await Page.WaitForTimeoutAsync(500);
        var texts = await Page.EvaluateAsync<string[]>(
            @"exempt => {
                const found = [];
                const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
                while (walker.nextNode()) {
                    const node = walker.currentNode;
                    const text = node.textContent.trim();
                    const element = node.parentElement;
                    if (!text || !element || element.closest('script, style, noscript, ' + exempt)) {
                        continue;
                    }

                    if (element.checkVisibility && !element.checkVisibility({ checkOpacity: true, checkVisibilityCSS: true })) {
                        continue;
                    }

                    found.push(text);
                }

                return found;
            }",
            UserContent);

        HashSet<string> allowed;
        lock (appSettingsText)
        {
            allowed = new HashSet<string>(appSettingsText.Concat(UserText).Concat(data), StringComparer.Ordinal);
        }

        var hardCoded = texts
            .Where(t => t.Any(char.IsLetter))
            .Where(t => !t.Contains("[!!", StringComparison.Ordinal) && !t.Contains("!!]", StringComparison.Ordinal))
            .Where(t => !allowed.Contains(t) && !allowed.Any(a => t.Contains(a, StringComparison.Ordinal)))
            .Where(t => !IsDateOrTime(t) && !IsVersion(t))
            .Distinct()
            .ToList();

        if (hardCoded.Count > 0)
        {
            await ScreenshotAsync(where.Replace(' ', '-'));
        }

        Assert.AreEqual(0, hardCoded.Count, $"Hard-coded text on the {where}:{Environment.NewLine}{string.Join(Environment.NewLine, hardCoded.Select(t => $"  \"{t}\""))}");
    }
}
