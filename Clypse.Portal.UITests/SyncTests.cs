using System.Text.RegularExpressions;

namespace Clypse.Portal.UITests;

/// <summary>
/// The UI tests use the placeholder sign-in with no AWS, so there is no cloud sync: these cover what the user sees
/// without it. Real syncing is covered by the Core integration tests against the real bucket.
/// </summary>
[TestClass]
public class SyncTests : TestBase
{
    [TestMethod]
    [Ignore("Needs the blazwiz fix that checks connectivity when there are no sync profiles (blazwiz CHANGELOG, Unreleased). Remove once clypse is on a release with it.")]
    public async Task GivenNoCloud_WhenSignedIn_ThenPillSaysOfflineNoCredentialsAndSyncNowIsDisabled()
    {
        await LoginAsync();

        var pill = Page.Locator("#sync-status-pill");
        await Expect(pill).ToContainTextAsync("Offline");
        await Expect(pill).ToHaveAttributeAsync("title", new Regex("Offline: no credentials"));

        await pill.ClickAsync();
        await Expect(Page.Locator("#sync-popover-reason")).ToContainTextAsync("Offline: no credentials");
        await Expect(Page.Locator("#sync-now-button")).ToBeDisabledAsync();
        await ScreenshotAsync("StatusPopover");
    }

    [TestMethod]
    [Ignore("Needs the blazwiz fix that checks connectivity when there are no sync profiles (blazwiz CHANGELOG, Unreleased). Remove once clypse is on a release with it.")]
    public async Task GivenNoCloud_WhenSyncSwitchedOff_ThenPillSaysSyncOffAndItSurvivesAReload()
    {
        await LoginAsync();
        var pill = Page.Locator("#sync-status-pill");
        await pill.ClickAsync();

        await Page.Locator("#sync-enabled-switch").UncheckAsync();
        await Expect(pill).ToHaveAttributeAsync("title", new Regex("Sync is turned off"));
        await Expect(pill).ToContainTextAsync("Sync off");

        await ReloadAsync();
        await Expect(Page.Locator("#sync-status-pill")).ToHaveAttributeAsync("title", new Regex("Sync is turned off"));
        await Page.Locator("#sync-status-pill").ClickAsync();
        await Expect(Page.Locator("#sync-enabled-switch")).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task GivenSyncOff_WhenSigningOutAndBackIn_ThenAdvancedOptionsShowItAndUntickingTurnsSyncOn()
    {
        await LoginAsync();
        await Page.Locator("#sync-status-pill").ClickAsync();
        await Page.Locator("#sync-enabled-switch").UncheckAsync();
        await Expect(Page.Locator("#sync-status-pill")).ToHaveAttributeAsync("title", new Regex("Sync is turned off"));
        await Page.Locator(".sync-popover-backdrop").ClickAsync();

        await Page.Locator("#logout-button").ClickAsync();
        await ExpectLoginPageAsync();
        await Expect(Page.Locator("#advanced-options")).ToBeVisibleAsync();
        await Expect(Page.Locator("#start-sync-off")).ToBeCheckedAsync();
        await ScreenshotAsync("AdvancedOptions");

        await Page.Locator("#start-sync-off").UncheckAsync();
        await Page.Locator("#username").FillAsync("bob.hoskins@example.com");
        await Page.Locator("#password").FillAsync("password123");
        await Page.Locator("#login-button").ClickAsync();
        await Expect(Page.Locator("#dashboard-greeting")).ToBeVisibleAsync();

        await Expect(Page.Locator("#sync-status-pill")).ToHaveAttributeAsync("title", new Regex("Offline: no credentials"));
    }

    [TestMethod]
    public async Task GivenSyncOn_WhenOpeningLogin_ThenAdvancedOptionsCollapsed()
    {
        await GoToLoginAsync();

        await Expect(Page.Locator("#advanced-options-toggle")).ToBeVisibleAsync();
        await Expect(Page.Locator("#advanced-options")).Not.ToBeVisibleAsync();

        await Page.Locator("#advanced-options-toggle").ClickAsync();
        await Expect(Page.Locator("#start-sync-off")).Not.ToBeCheckedAsync();
    }

    [TestMethod]
    public async Task GivenSignedIn_WhenOfflineModeTurnedOnInSettings_ThenBannerOfflineCountdownAndNoSync()
    {
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();

        await Page.Locator("#offline-mode-switch").CheckAsync();

        await Expect(Page.Locator("#offline-banner")).ToBeVisibleAsync();
        await Expect(Page.Locator("#offline-banner-text")).ToContainTextAsync("You're in offline mode");
        await Expect(Page.Locator("#session-timer")).ToHaveTextAsync(new Regex(@"^Offline 1[45]:\d{2}$"));
        await Expect(Page.Locator("#sync-status-pill")).ToHaveAttributeAsync("title", new Regex("Offline: offline mode"));
        await Expect(Page.Locator("#e2e-offline")).ToContainTextAsync("You're offline");
        await ScreenshotAsync("OfflineMode");
    }

    [TestMethod]
    public async Task GivenOfflineMode_WhenTurnedOff_ThenLoginPageIsShown()
    {
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();
        await Page.Locator("#offline-mode-switch").CheckAsync();
        await Expect(Page.Locator("#offline-banner")).ToBeVisibleAsync();

        // A click, not UncheckAsync: the page leaves for the login page before the switch could be seen unticked.
        await Page.Locator("#offline-mode-switch").ClickAsync();

        await ExpectLoginPageAsync();
    }

    [TestMethod]
    public async Task GivenOfflineMode_WhenSignInOnlineClicked_ThenLoginPageIsShown()
    {
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();
        await Page.Locator("#offline-mode-switch").CheckAsync();

        await Page.Locator("#offline-banner-sign-in").ClickAsync();

        await ExpectLoginPageAsync();
    }

    [TestMethod]
    public async Task GivenOfflineMode_WhenPageReloaded_ThenOfflineSessionIsGoneAndLoginPageIsShown()
    {
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();
        await Page.Locator("#offline-mode-switch").CheckAsync();
        await Expect(Page.Locator("#offline-banner")).ToBeVisibleAsync();

        await ReloadAsync();

        await ExpectLoginPageAsync();
    }

    [TestMethod]
    [Ignore("Needs the blazwiz fix that checks connectivity when there are no sync profiles (blazwiz CHANGELOG, Unreleased). Remove once clypse is on a release with it.")]
    public async Task GivenSyncPage_WhenOpened_ThenConnectivityEventsCanBeSearchedFilteredAndSurviveAReload()
    {
        await LoginAsync();
        await Page.Locator("#sync-status-pill").ClickAsync();
        await Page.Locator("#sync-enabled-switch").UncheckAsync();
        await Page.Locator(".sync-popover-backdrop").ClickAsync();

        await Page.Locator("#nav-sync").ClickAsync();
        await Expect(Page.Locator("h1.page-title")).ToContainTextAsync("Sync");

        var log = Page.Locator("#sync-log");
        await Expect(log).ToContainTextAsync("Offline: no credentials");
        await Expect(log).ToContainTextAsync("Sync turned off");
        await Expect(log.Locator(".bi-wifi-off").First).ToBeVisibleAsync();
        await Expect(log.Locator(".bi-airplane").First).ToBeVisibleAsync();
        await ScreenshotAsync("SyncLog");

        await Page.Locator("#sync-search").FillAsync("credentials");
        await Expect(log).ToContainTextAsync("Offline: no credentials");
        await Expect(log).Not.ToContainTextAsync("Sync turned off");
        await Page.Locator("#sync-search").FillAsync(string.Empty);

        await Page.Locator(".sync-filter[data-group='Connectivity']").ClickAsync();
        await Expect(log).ToContainTextAsync("Sync turned off");
        await Page.Locator(".sync-filter[data-group='Uploads']").ClickAsync();
        await Expect(Page.Locator("#sync-log-empty")).ToBeVisibleAsync();

        await ReloadAsync();
        await Expect(Page.Locator("#sync-log")).ToContainTextAsync("Sync turned off");
    }

    [TestMethod]
    [Ignore("Needs the blazwiz fix that loads the storage choice again when a profile is created (blazwiz CHANGELOG, Unreleased). Remove once clypse is on a release with it.")]
    public async Task GivenStorageChoiceOpen_WhenCancelled_ThenNothingMoves()
    {
        await LoginAsync();
        await Page.Locator("#nav-settings").ClickAsync();
        await CreateProtectedProfileAsync();
        await Page.Locator("#storage-option-opfs").ClickAsync();
        await Expect(Page.Locator("#storage-dialog")).ToBeVisibleAsync();

        await Page.Locator("#storage-dialog .btn-secondary").ClickAsync();

        await Expect(Page.Locator("#storage-dialog")).ToBeHiddenAsync();
        await Expect(Page.Locator("#storage-option-indexeddb")).ToHaveClassAsync(new Regex("active"));
        Assert.AreEqual(0, (await ListOpfsFilesAsync()).Count);
    }


    [TestMethod]
    [Ignore("Needs the blazwiz fix that loads the storage choice again when a profile is created (blazwiz CHANGELOG, Unreleased). Remove once clypse is on a release with it.")]
    public async Task GivenTwoTabs_WhenOneMovesTheFiles_ThenOnlyTheOtherReloads()
    {
        await LoginAsync();
        var other = await Context.NewPageAsync();
        await GoToAsync(other);
        await Expect(other.Locator("#dashboard-greeting")).ToBeVisibleAsync();
        await other.EvaluateAsync("() => window.notReloaded = true");
        await Page.EvaluateAsync("() => window.notReloaded = true");

        await Page.Locator("#nav-settings").ClickAsync();
        await CreateProtectedProfileAsync();
        await Page.Locator("#storage-option-opfs").ClickAsync();
        await Page.Locator("#storage-confirm").ClickAsync();
        await Expect(Page.Locator("#storage-option-opfs")).ToHaveClassAsync(new Regex("active"));

        await other.WaitForFunctionAsync("() => window.notReloaded === undefined");
        await Expect(other.Locator("#dashboard-greeting")).ToBeVisibleAsync();
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.notReloaded === true"));
    }

    private ILocator Row(string name) => Page.Locator($"#file-tree .file-row[data-name='{name}']");

    // Playwright cannot drive the real folder picker, so it hands back a real directory handle (an OPFS folder called
    // 'picked-folder'). Permission is granted unless the test sets 'test-folder-revoked' in localStorage, which asking
    // for permission again clears.
    private Task StubFolderPickerAsync()
    {
        return Page.AddInitScriptAsync(@"
            window.showDirectoryPicker = async () =>
                (await navigator.storage.getDirectory()).getDirectoryHandle('picked-folder', { create: true });
            FileSystemHandle.prototype.queryPermission = async () =>
                localStorage.getItem('test-folder-revoked') ? 'prompt' : 'granted';
            FileSystemHandle.prototype.requestPermission = async () => {
                localStorage.removeItem('test-folder-revoked');
                return 'granted';
            };");
    }

    private async Task<List<string>> ListPickedFolderAsync()
    {
        var files = await Page.EvaluateAsync<string[]>(@"async () => {
            const found = [];
            async function walk(directory, prefix) {
                for await (const [name, handle] of directory.entries()) {
                    if (handle.kind === 'directory') {
                        await walk(handle, prefix + name + '/');
                    } else {
                        found.push(prefix + name);
                    }
                }
            }

            await walk(await (await navigator.storage.getDirectory()).getDirectoryHandle('picked-folder', { create: true }), '');
            return found;
        }");
        return [.. files];
    }

    // Every file under the notes OPFS root, as "<key>" relative to the user's directory.
    private async Task<List<string>> ListOpfsFilesAsync()
    {
        var files = await Page.EvaluateAsync<string[]>(@"async () => {
            const found = [];
            async function walk(directory, prefix) {
                for await (const [name, handle] of directory.entries()) {
                    if (handle.kind === 'directory') {
                        await walk(handle, prefix + name + '/');
                    } else {
                        found.push(prefix + name);
                    }
                }
            }

            const top = await navigator.storage.getDirectory();
            let root;
            try {
                root = await top.getDirectoryHandle('clypse_notes');
            } catch {
                return found;
            }

            for await (const [, owner] of root.entries()) {
                await walk(owner, '');
            }

            return found;
        }");
        return [.. files];
    }
}
