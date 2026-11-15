using System.Text.RegularExpressions;
using Clypse.Portal.DeployTests.Support;
using static Microsoft.Playwright.Assertions;

namespace Clypse.Portal.DeployTests.Steps;

/// <summary>
/// Steps that wait for sync, through the sync status pill and its popover.
/// </summary>
/// <remarks>
/// The pill counts changes still to upload only at the end of a sync pass, so it can say "Up to date" while a new file
/// waits. "Sync is complete" therefore runs a pass of its own with Sync now and waits for it to end with nothing left.
/// </remarks>
/// <param name="session">The scenario's browser.</param>
/// <param name="flow">Says whether the scenario has ended early.</param>
[Binding]
public sealed partial class SyncSteps(BrowserSession session, ScenarioFlow flow)
{
    private readonly BrowserSession session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly ScenarioFlow flow = flow ?? throw new ArgumentNullException(nameof(flow));

    private IPage Page => session.Page;

    private ILocator Pill => Page.Locator("#sync-status-pill");

    /// <summary>
    /// Waits for the first sync pass since sign in to finish with nothing left to sync. It runs once the user's vaults
    /// have been read from the cloud, so the vault list is complete from then on.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Given("the first sync has finished")]
    public async Task GivenTheFirstSyncHasFinished()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await OpenPopoverAsync();
        await Expect(Page.Locator("#sync-last-synced")).Not.ToHaveTextAsync("Not yet", new() { Timeout = BrowserSession.SyncTimeoutMs });
        await ExpectUpToDateAsync();
        await ClosePopoverAsync();
    }

    /// <summary>
    /// Runs a sync pass with Sync now, once any pass already running has finished, and waits for it to end with
    /// nothing left to upload: everything changed before it is in the cloud.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("sync is complete")]
    public async Task ThenSyncIsComplete()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await OpenPopoverAsync();
        var syncNow = Page.Locator("#sync-now-button");
        await Expect(syncNow).ToBeEnabledAsync(new() { Timeout = BrowserSession.SyncTimeoutMs });
        await syncNow.ClickAsync();
        try
        {
            // The pass can finish before this looks: then it is already up to date below.
            await Expect(Pill.Locator(".sync-spin")).ToHaveCountAsync(1, new() { Timeout = 5000 });
        }
        catch (PlaywrightException)
        {
        }

        await ExpectUpToDateAsync();
        await Expect(syncNow).ToBeEnabledAsync();
        await ClosePopoverAsync();
    }

    [GeneratedRegex(@"\bsync-tone-success\b")]
    private static partial Regex UpToDate();

    // Up to date: no pass running, online, no error and nothing pending (the pill's only "success" state).
    private Task ExpectUpToDateAsync()
    {
        return Expect(Pill).ToHaveClassAsync(UpToDate(), new() { Timeout = BrowserSession.SyncTimeoutMs });
    }

    private async Task OpenPopoverAsync()
    {
        if (await Page.Locator("#sync-popover").CountAsync() == 0)
        {
            await Pill.ClickAsync();
        }

        await Expect(Page.Locator("#sync-popover")).ToBeVisibleAsync();
    }

    private async Task ClosePopoverAsync()
    {
        await Page.Locator(".sync-popover-backdrop").ClickAsync();
        await Expect(Page.Locator("#sync-popover")).ToHaveCountAsync(0);
    }
}
