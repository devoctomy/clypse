namespace Clypse.Portal.UITests;

// The secrets in a vault: importing them, the report on them, scrolling the list, and copying their fields.
public partial class VaultTests
{
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
    public async Task GivenReusedKnownWeakPasswords_WhenTheReportIsShown_ThenItsChecksSaySo_InClosedSections_ThatOpenTheirSecrets()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        var csv = Path.Combine(Path.GetTempPath(), $"clypse-import-{Guid.NewGuid():N}.csv");

        // password123 is on the Debug build's stand-in list of weak known passwords.
        await File.WriteAllTextAsync(csv, "\"Account\",\"Username\",\"Password\",\"Web site\",\"Comments\"\r\n\"Gmail\",\"bob\",\"password123\",\"\",\"\"\r\n\"Bank\",\"bob\",\"password123\",\"\",\"\"\r\n");
        try
        {
            await Page.Locator("#import-button").ClickAsync();
            await Page.Locator("#import-file").SetInputFilesAsync(csv);
            await Page.Locator("#import-confirm").ClickAsync();
            await Expect(Page.Locator("#import-done")).ToContainTextAsync("2 secrets imported.");
            await Page.Locator("#import-close").ClickAsync();
        }
        finally
        {
            File.Delete(csv);
        }

        await Expect(Page.Locator("#report-button #report-alert")).ToBeVisibleAsync();

        await Page.Locator("#report-button").ClickAsync();

        await Expect(Page.Locator("#vault-report-overview")).ToContainTextAsync("2 of 5 checks passed");
        await Expect(Page.Locator("#vault-report-overview")).ToContainTextAsync("2 secrets checked.");
        var sections = Page.Locator("#vault-report-sections .vault-report-section");
        await Expect(sections).ToHaveCountAsync(5);
        await Expect(Page.Locator(".vault-report-section-body")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".vault-report-section.failed")).ToHaveCountAsync(3);
        var known = Page.Locator(".vault-report-section[data-check='KnownWeakPasswords']");
        await Expect(known.Locator(".vault-report-section-summary")).ToHaveTextAsync("2 secrets use known weak passwords.");
        await Expect(Page.Locator(".vault-report-section[data-check='WeakPasswords'] .vault-report-section-summary")).ToHaveTextAsync("No weak passwords found.");
        await ScreenshotAsync("VaultReport");

        await known.Locator(".vault-report-section-header").ClickAsync();

        await Expect(known.Locator(".vault-report-section-header")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(known.Locator(".vault-report-secret")).ToHaveTextAsync(["Bank", "Gmail"]);
        await ScreenshotAsync("VaultReportOpen");

        await known.Locator(".vault-report-secret", new() { HasTextString = "Gmail" }).ClickAsync();

        await Expect(Page.Locator("#vault-report-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#secret-dialog")).ToBeVisibleAsync();
        var password = Page.Locator("#secret-field-Password-concerns .field-concern");
        await Expect(password).ToHaveTextAsync(["This password is on the list of known weak passwords.", "This password is also used by Bank."]);
        await Expect(Page.Locator("#secret-field-UserName-concerns .field-concern")).ToHaveTextAsync("This user name is also used by Bank.");
        await Expect(Page.Locator("label[for='secret-field-Password'] .field-alert-dot")).ToBeVisibleAsync();
        await Expect(Page.Locator("label[for='secret-field-Name'] .field-alert-dot")).ToHaveCountAsync(0);
        await ScreenshotAsync("SecretConcerns");

        await Page.Locator("#secret-field-Password").FillAsync("Rc3$mWx8!fGt5@kNz1q");

        await Expect(Page.Locator("#secret-field-Password-concerns")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#secret-field-UserName-concerns")).ToHaveCountAsync(1);
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

        await Expect(Page.Locator("#secrets-count")).ToHaveTextAsync($"Showing {count}/{count} secrets");
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

        await Expect(Page.Locator("#toasts .bw-toast")).ToContainTextAsync("Password copied.");
        await Expect(Page.Locator("#toasts .bw-toast")).Not.ToContainTextAsync("cleared");
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
        await Expect(Page.Locator("#clipboard-settings-after")).ToHaveCountAsync(0);
        await Page.Locator("#clipboard-settings-clear").CheckAsync();
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
}
