using System.Text.RegularExpressions;
using Clypse.Portal.DeployTests.Support;
using static Microsoft.Playwright.Assertions;

namespace Clypse.Portal.DeployTests.Steps;

/// <summary>
/// Steps for the files in the open vault: its Files tab, adding a file from TestData, viewing an image, editing text,
/// and deleting a file.
/// </summary>
/// <param name="session">The scenario's browser.</param>
/// <param name="flow">Says whether the scenario has ended early.</param>
[Binding]
public sealed class FileSteps(BrowserSession session, ScenarioFlow flow)
{
    private readonly BrowserSession session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly ScenarioFlow flow = flow ?? throw new ArgumentNullException(nameof(flow));

    private IPage Page => session.Page;

    /// <summary>
    /// Opens the open vault's Files tab, and waits for it to finish reading the details of its files.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I open the vault's files")]
    public async Task WhenIOpenTheVaultsFiles()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#vault-tab-files").ClickAsync();
        await Expect(Page.Locator("#vault-files")).ToBeVisibleAsync();
        await Expect(Page.Locator("#files-loading")).ToHaveCountAsync(0, new() { Timeout = BrowserSession.SyncTimeoutMs });
        await Expect(Page.Locator("#files, #no-files, #no-file-matches").First).ToBeVisibleAsync();
    }

    /// <summary>
    /// Deletes every file with that name in the folder shown, if there is one: a run that failed part way through
    /// leaves its file behind.
    /// </summary>
    /// <param name="name">The file's name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I delete any file named {string}")]
    public async Task WhenIDeleteAnyFileNamed(string name)
    {
        if (flow.HasEnded)
        {
            return;
        }

        var left = await Files(name).CountAsync();
        while (left > 0)
        {
            await DeleteFirstAsync(name);
            await Expect(Files(name)).ToHaveCountAsync(left - 1);
            left--;
        }
    }

    /// <summary>
    /// Adds a file from TestData to the folder shown, and waits for it to be encrypted and stored.
    /// </summary>
    /// <param name="fileName">The file's name in TestData.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I add the file {string}")]
    public async Task WhenIAddTheFile(string fileName)
    {
        if (flow.HasEnded)
        {
            return;
        }

        var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"There is no test file '{fileName}' in TestData.", path);
        }

        await Page.Locator("#add-files-input").SetInputFilesAsync(path);
        await Expect(Files(fileName).First).ToBeVisibleAsync(new() { Timeout = BrowserSession.SyncTimeoutMs });
        await Expect(Page.Locator("#files-working")).ToHaveCountAsync(0, new() { Timeout = BrowserSession.SyncTimeoutMs });
        await Expect(Page.Locator("#files-error")).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Checks the folder shown lists exactly one file with that name.
    /// </summary>
    /// <param name="name">The file's name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the file {string} is listed")]
    public async Task ThenTheFileIsListed(string name)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Expect(Files(name)).ToHaveCountAsync(1);
    }

    /// <summary>
    /// Checks the folder shown lists no file with that name.
    /// </summary>
    /// <param name="name">The file's name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the file {string} is not listed")]
    public async Task ThenTheFileIsNotListed(string name)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Expect(Files(name)).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Opens a file from the list: an image in the viewer, text in the editor.
    /// </summary>
    /// <param name="name">The file's name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I open the file {string}")]
    public async Task WhenIOpenTheFile(string name)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Files(name).First.Locator(".file-row").ClickAsync();
    }

    /// <summary>
    /// Checks the viewer decrypted the open file and shows it as an image the browser could draw.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the viewer shows its image")]
    public async Task ThenTheViewerShowsItsImage()
    {
        if (flow.HasEnded)
        {
            return;
        }

        var image = Page.Locator("#file-viewer-image");
        await Expect(image).ToBeVisibleAsync();
        await Expect(Page.Locator("#file-viewer-error")).ToHaveCountAsync(0);
        await Expect(image).ToHaveJSPropertyAsync("complete", true);
        var width = await image.EvaluateAsync<int>("image => image.naturalWidth");
        if (width == 0)
        {
            throw new InvalidOperationException("The viewer's image could not be drawn by the browser.");
        }
    }

    /// <summary>
    /// Closes the viewer.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I close the viewer")]
    public async Task WhenICloseTheViewer()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#file-viewer-close").ClickAsync();
        await Expect(Page.Locator("#file-viewer-dialog")).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Deletes a file from the list, answering yes when asked.
    /// </summary>
    /// <param name="name">The file's name.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I delete the file {string}")]
    public async Task WhenIDeleteTheFile(string name)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await DeleteFirstAsync(name);
    }

    /// <summary>
    /// Checks the editor has decrypted the open file and shows its text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Then("the editor shows the text {string}")]
    public async Task ThenTheEditorShowsTheText(string text)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Expect(Page.Locator("#editor-error")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#text-editor")).ToHaveValueAsync(text);
    }

    /// <summary>
    /// Replaces the text in the editor.
    /// </summary>
    /// <param name="text">The new text.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I change the text to {string}")]
    public async Task WhenIChangeTheTextTo(string text)
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#text-editor").FillAsync(text);
        await Expect(Page.Locator("#editor-save-button")).ToBeEnabledAsync();
    }

    /// <summary>
    /// Saves the editor's text, and waits for it to be encrypted and stored.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I save the text")]
    public async Task WhenISaveTheText()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#editor-save-button").ClickAsync();
        await Expect(Page.Locator("#editor-save-button")).ToBeDisabledAsync();
        await Expect(Page.Locator("#editor-error")).ToHaveCountAsync(0);
    }

    /// <summary>
    /// Closes the editor, which goes back to the list of files.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [When("I close the editor")]
    public async Task WhenICloseTheEditor()
    {
        if (flow.HasEnded)
        {
            return;
        }

        await Page.Locator("#editor-close-button").ClickAsync();
        await Expect(Page.Locator("#text-editor-panel")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#vault-files")).ToBeVisibleAsync();
    }

    // The files in the folder shown with exactly that name.
    private ILocator Files(string name)
    {
        var exactName = new Regex($@"^\s*{Regex.Escape(name)}\s*$");
        return Page.Locator("#files .file-item[data-file]").Filter(new()
        {
            Has = Page.Locator(".file-name", new() { HasTextRegex = exactName }),
        });
    }

    private async Task DeleteFirstAsync(string name)
    {
        await Files(name).First.Locator(".file-delete").ClickAsync();
        await Expect(Page.Locator("#file-delete-dialog")).ToBeVisibleAsync();
        await Page.Locator("#file-delete-yes").ClickAsync();
        await Expect(Page.Locator("#file-delete-dialog")).ToHaveCountAsync(0);
    }
}
