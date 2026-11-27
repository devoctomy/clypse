using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Clypse.Portal.UITests;

/// <summary>
/// A vault's files, in the browser: the Files tab, folders, the image viewer and the text editor. The files are small,
/// because the UI tests' build is interpreted, where encrypting is slow.
/// </summary>
[TestClass]
public partial class VaultFilesTests : TestBase
{
    private const string ImageName = "passport-scan.png";
    private const string ImageBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

    // Searches IndexedDB, localStorage and sessionStorage for each needle (bytes, given as base64), and lists the file
    // keys and how many of the stored binary values are E2EE ciphertext.
    private const string ScanStorageScript =
        """
        async needles => {
            const wanted = Object.entries(needles).map(([name, b64]) => [name, Uint8Array.from(atob(b64), c => c.charCodeAt(0))]);
            const encoder = new TextEncoder();
            const found = new Set();
            const keys = new Set();
            const pending = [];
            let binaries = 0;
            let encrypted = 0;
            const contains = (hay, needle) => {
                outer: for (let i = 0; i + needle.length <= hay.length; i++) {
                    for (let j = 0; j < needle.length; j++) {
                        if (hay[i + j] !== needle[j]) { continue outer; }
                    }
                    return true;
                }
                return false;
            };
            const search = (where, bytes) => {
                for (const [name, needle] of wanted) {
                    if (contains(bytes, needle)) { found.add(name + ' in ' + where); }
                }
            };
            const walk = (where, value) => {
                if (value == null) { return; }
                if (typeof value === 'string') {
                    search(where, encoder.encode(value));
                    for (const match of value.matchAll(/files\/(?:meta|blobs)\/[^"\s\\]*/g)) { keys.add(match[0]); }
                    return;
                }
                if (value instanceof Blob) {
                    pending.push(value.arrayBuffer().then(buffer => walk(where, buffer)));
                    return;
                }
                if (value instanceof ArrayBuffer) { value = new Uint8Array(value); }
                if (ArrayBuffer.isView(value)) {
                    const bytes = new Uint8Array(value.buffer, value.byteOffset, value.byteLength);
                    binaries++;
                    if (bytes[0] === 0x45 && bytes[1] === 0x32 && bytes[2] === 0x45 && bytes[3] === 0x45) { encrypted++; }
                    search(where, bytes);
                    return;
                }
                if (typeof value === 'object') {
                    for (const [key, inner] of Object.entries(value)) {
                        walk(where, key);
                        walk(where, inner);
                    }
                }
            };
            const all = request => new Promise((resolve, reject) => {
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
            });
            for (const info of await indexedDB.databases()) {
                const db = await all(indexedDB.open(info.name));
                for (const name of db.objectStoreNames) {
                    const store = db.transaction(name).objectStore(name);
                    const [storeKeys, values] = await Promise.all([all(store.getAllKeys()), all(store.getAll())]);
                    storeKeys.forEach(key => walk('IndexedDB ' + info.name + '/' + name, key));
                    values.forEach(value => walk('IndexedDB ' + info.name + '/' + name, value));
                }
                db.close();
            }
            for (const [where, storage] of [['localStorage', localStorage], ['sessionStorage', sessionStorage]]) {
                for (let i = 0; i < storage.length; i++) {
                    const key = storage.key(i);
                    walk(where, key);
                    walk(where, storage.getItem(key));
                }
            }
            await Promise.all(pending);
            return JSON.stringify({ found: [...found], keys: [...keys], binaries, encrypted });
        }
        """;

    private static readonly byte[] Image = Convert.FromBase64String(ImageBase64);

    [TestMethod]
    public async Task GivenAVault_WhenOpened_ThenSecretsIsShownFirst_AndTheTabsSwitch()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");

        await Expect(Page.Locator("#vault-tab-secrets")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.Locator("#vault-tab-files")).ToHaveAttributeAsync("aria-selected", "false");
        await Expect(Page.Locator("#vault-page")).ToBeVisibleAsync();
        await Expect(Page.Locator("#vault-files")).ToHaveCountAsync(0);

        await Page.Locator("#vault-tab-files").ClickAsync();

        await Expect(Page.Locator("#vault-tab-files")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.Locator("#vault-files")).ToBeVisibleAsync();
        await Expect(Page.Locator("#no-files")).ToBeVisibleAsync();
        await Expect(Page.Locator("#vault-page")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#add-files-button")).ToBeVisibleAsync();
        await Expect(Page.Locator("#new-file-button")).ToBeVisibleAsync();
        await Expect(Page.Locator("#new-secret-button")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#lock-vault-button")).ToBeVisibleAsync();
        await ScreenshotAsync("NoFiles");

        await Page.Locator("#vault-tab-secrets").ClickAsync();

        await Expect(Page.Locator("#vault-page")).ToBeVisibleAsync();
        await Expect(Page.Locator("#new-secret-button")).ToBeVisibleAsync();
        await Expect(Page.Locator("#new-file-button")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task GivenTheFilesTab_WhenNewIsOpened_ThenItOffersAFolderAMarkdownDocumentAndATextFile()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#vault-tab-files").ClickAsync();

        await Page.Locator("#new-file-button").ClickAsync();

        await Expect(Page.Locator("#new-file-menu .dropdown-item")).ToHaveTextAsync(["Folder", "Markdown Document", "Text File"]);
        await ScreenshotAsync("NewMenu");

        await Page.Locator(".files-new-backdrop").ClickAsync();

        await Expect(Page.Locator("#new-file-menu")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task GivenTheFilesTab_WhenAnImageIsAdded_ThenItIsListed_AndTheViewerShowsIt()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#vault-tab-files").ClickAsync();

        await AddImageAsync();

        var row = FileItem(ImageName);
        await Expect(row.Locator(".file-summary").First).ToContainTextAsync($"Image · {Image.Length} B · Added ");
        await Expect(Page.Locator("#files .file-item")).ToHaveCountAsync(1);
        await ScreenshotAsync("Files");

        await row.Locator(".file-row").ClickAsync();

        await Expect(Page.Locator("#file-viewer-name")).ToHaveTextAsync(ImageName);
        await Expect(Page.Locator("#file-viewer-type")).ToHaveTextAsync("Image");
        var shown = Page.Locator("#file-viewer-image");
        await Expect(shown).ToHaveAttributeAsync("src", $"data:image/png;base64,{ImageBase64}");
        Assert.AreEqual(1, await shown.EvaluateAsync<int>("image => image.naturalWidth"));
        await ScreenshotAsync("Viewer");

        var toggle = Page.Locator("#file-viewer-image-toggle");
        await toggle.ClickAsync();
        await Expect(toggle).ToHaveClassAsync(new Regex(@"\bis-fullscreen\b"));
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        await toggle.ClickAsync();
        await Expect(toggle).Not.ToHaveClassAsync(new Regex(@"\bis-fullscreen\b"));

        await Page.Locator("#file-viewer-close").ClickAsync();

        await Expect(Page.Locator("#file-viewer-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("img[src^='data:']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task GivenAFile_WhenMovedIntoAFolder_RenamedAndDeleted_ThenTheEmptiedFolderIsStillThereAfterAReload()
    {
        await LoginAsync();
        var id = await CreateVaultAsync("Personal");
        await Page.Locator("#vault-tab-files").ClickAsync();
        await AddImageAsync();

        // Make a folder.
        await Page.Locator("#new-file-button").ClickAsync();
        await Page.Locator("#new-folder").ClickAsync();
        await Expect(Page.Locator("#file-name-dialog .modal-title")).ToContainTextAsync("New folder");
        await Page.Locator("#file-name-input").FillAsync("Travel");
        await Page.Locator("#file-name-save").ClickAsync();
        await Expect(Page.Locator("#file-name-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#files .file-item[data-folder='Travel']")).ToBeVisibleAsync();

        // Move the file into it, with a new name and a note.
        await FileItem(ImageName).Locator(".file-details").ClickAsync();
        await Expect(Page.Locator("#file-details-name")).ToHaveValueAsync(ImageName);
        await Page.Locator("#file-details-name").FillAsync("Passport.png");
        await Page.Locator("#file-details-folder").SelectOptionAsync("Travel");
        await Page.Locator("#file-details-notes").FillAsync("Renew in 2031");
        await ScreenshotAsync("FileDetails");
        await Page.Locator("#file-details-save").ClickAsync();
        await Expect(Page.Locator("#file-details-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#files .file-item[data-file]")).ToHaveCountAsync(0);

        await Page.Locator("#files .file-item[data-folder='Travel'] .file-row").ClickAsync();
        await Expect(Page.Locator("#files-breadcrumb .files-crumb")).ToHaveTextAsync(["Personal", "Travel"]);
        await Expect(FileItem("Passport.png")).ToBeVisibleAsync();

        // Rename the folder: the file goes with it, and is found by its note from anywhere.
        await Page.Locator("#files-breadcrumb .files-crumb[data-folder='']").ClickAsync();
        await Page.Locator("#files .file-item[data-folder='Travel'] .file-rename").ClickAsync();
        await Expect(Page.Locator("#file-name-input")).ToHaveValueAsync("Travel");
        await Page.Locator("#file-name-input").FillAsync("Trips");
        await Page.Locator("#file-name-save").ClickAsync();
        await Expect(Page.Locator("#files .file-item[data-folder='Trips']")).ToBeVisibleAsync();
        await Expect(Page.Locator("#files .file-item[data-folder='Travel']")).ToHaveCountAsync(0);
        await Page.Locator("#file-search").FillAsync("renew");
        await Expect(FileItem("Passport.png").Locator(".file-in-folder")).ToHaveTextAsync("In Trips");
        await Page.Locator("#file-search").FillAsync(string.Empty);

        // Delete the file: its folder is empty again, and still there.
        await Page.Locator("#files .file-item[data-folder='Trips'] .file-row").ClickAsync();
        await FileItem("Passport.png").Locator(".file-delete").ClickAsync();
        await Expect(Page.Locator("#file-delete-question")).ToHaveTextAsync("Delete Passport.png? This cannot be undone.");
        await Page.Locator("#file-delete-yes").ClickAsync();
        await Expect(Page.Locator("#file-delete-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#no-file-matches")).ToHaveTextAsync("This folder is empty.");

        await GoToAsync("/");
        await Page.Locator($".vault-card[data-vault='{id}'] .vault-card-main").ClickAsync();
        await UnlockAsync();
        await Page.Locator("#vault-tab-files").ClickAsync();

        var folder = Page.Locator("#files .file-item[data-folder='Trips']");
        await Expect(folder).ToBeVisibleAsync();
        await Expect(Page.Locator("#files .file-item")).ToHaveCountAsync(1);

        await folder.Locator(".file-delete").ClickAsync();
        await Expect(Page.Locator("#file-delete-question")).ToHaveTextAsync("Delete the empty folder Trips?");
        await Page.Locator("#file-delete-yes").ClickAsync();

        await Expect(Page.Locator("#no-files")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task GivenTheViewerShowingAnImage_WhenTheVaultIsLocked_ThenTheViewerAndTheImageAreGone()
    {
        await LoginAsync();
        var id = await CreateVaultAsync("Personal");
        await Page.Locator("#vault-tab-files").ClickAsync();
        await AddImageAsync();
        await FileItem(ImageName).Locator(".file-row").ClickAsync();
        await Expect(Page.Locator("#file-viewer-image")).ToBeVisibleAsync();

        // The dialog covers the Lock button, so it is pressed without the mouse, as the idle lock would lock it.
        await Page.Locator("#lock-vault-button").DispatchEventAsync("click");

        await Expect(Page.Locator("#vaults-page")).ToBeVisibleAsync();
        await Expect(Page.Locator($".vault-card[data-vault='{id}'] .vault-card-state")).ToHaveTextAsync("Locked");
        await Expect(Page.Locator("#file-viewer-dialog")).ToHaveCountAsync(0);
        await Expect(Page.Locator("img[src^='data:']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task GivenANewMarkdownDocument_WhenWrittenFormattedAndSaved_ThenThePreviewFollows_AndTheTextIsThereWhenOpenedAgain()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#vault-tab-files").ClickAsync();

        await NewDocumentAsync("#new-markdown", "Trip notes");

        var editor = Page.Locator("#text-editor");
        await Expect(Page.Locator("#text-editor-name")).ToHaveTextAsync("Trip notes.md");
        await Expect(editor).ToHaveValueAsync(string.Empty);
        await Expect(Page.Locator(".editor-toolbar")).ToBeVisibleAsync();
        await Expect(Page.Locator("#editor-preview")).ToBeVisibleAsync();
        await Expect(Page.Locator("#new-file-button")).ToHaveCountAsync(0);

        await editor.FillAsync("# Lisbon trip\n\nRemember the passport.");

        await Expect(Page.Locator("#editor-preview h1")).ToHaveTextAsync("Lisbon trip");
        await Expect(Page.Locator("#editor-status")).ToHaveTextAsync("Unsaved changes");

        await editor.EvaluateAsync("e => { const start = e.value.indexOf('passport'); e.focus(); e.setSelectionRange(start, start + 8); }");
        await Page.Locator("#format-bold").ClickAsync();

        await Expect(editor).ToHaveValueAsync("# Lisbon trip\n\nRemember the **passport**.");
        await Expect(Page.Locator("#editor-preview strong")).ToHaveTextAsync("passport");
        await ScreenshotAsync("Editor");

        await Page.Locator("#toggle-preview-button").ClickAsync();
        await Expect(Page.Locator("#editor-preview")).ToHaveCountAsync(0);
        await Page.Locator("#toggle-preview-button").ClickAsync();
        await Expect(Page.Locator("#editor-preview")).ToBeVisibleAsync();

        await editor.FocusAsync();
        await Page.Keyboard.PressAsync("Control+s");

        await Expect(Page.Locator("#editor-status")).ToContainTextAsync("Saved at");
        await Expect(Page.Locator("#editor-save-button")).ToBeDisabledAsync();

        await Page.Locator("#editor-close-button").ClickAsync();

        await Expect(Page.Locator("#editor-leave-dialog")).ToHaveCountAsync(0);
        await Expect(FileItem("Trip notes.md").Locator(".file-summary").First).ToContainTextAsync("Markdown document");

        await FileItem("Trip notes.md").Locator(".file-row").ClickAsync();

        await Expect(Page.Locator("#text-editor")).ToHaveValueAsync("# Lisbon trip\n\nRemember the **passport**.");
        await Expect(Page.Locator("#editor-preview strong")).ToHaveTextAsync("passport");
    }

    [TestMethod]
    public async Task GivenANewTextFile_WhenOpened_ThenTheEditorHasNoToolbarOrPreview()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#vault-tab-files").ClickAsync();

        await NewDocumentAsync("#new-text", "Door codes");

        await Expect(Page.Locator("#text-editor-name")).ToHaveTextAsync("Door codes.txt");
        await Expect(Page.Locator("#text-editor")).ToBeVisibleAsync();
        await Expect(Page.Locator(".editor-toolbar")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#editor-preview")).ToHaveCountAsync(0);

        await Page.Locator("#text-editor").FillAsync("Garage: 4471");
        await Page.Locator("#editor-save-button").ClickAsync();

        await Expect(Page.Locator("#editor-status")).ToContainTextAsync("Saved at");
        await ScreenshotAsync("TextEditor");
        await Page.Locator("#editor-close-button").ClickAsync();
        await Expect(FileItem("Door codes.txt").Locator(".file-summary").First).ToContainTextAsync("Text file");
    }

    [TestMethod]
    public async Task GivenUnsavedChanges_WhenTheEditorIsClosedOrTheTabChanged_ThenItAsksFirst()
    {
        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#vault-tab-files").ClickAsync();
        await NewDocumentAsync("#new-text", "Codes");
        var editor = Page.Locator("#text-editor");
        await editor.FillAsync("1234");

        // Close: asked, and keeping on editing changes nothing.
        await Page.Locator("#editor-close-button").ClickAsync();
        await Expect(Page.Locator("#editor-leave-text")).ToHaveTextAsync("You have unsaved changes to Codes.txt. What would you like to do with them?");
        await ScreenshotAsync("UnsavedChanges");
        await Page.Locator("#editor-leave-stay").ClickAsync();
        await Expect(Page.Locator("#editor-leave-dialog")).ToHaveCountAsync(0);
        await Expect(editor).ToHaveValueAsync("1234");

        // Another tab: asked, and saving goes on to it.
        await Page.Locator("#vault-tab-secrets").ClickAsync();
        await Expect(Page.Locator("#editor-leave-dialog")).ToBeVisibleAsync();
        await Expect(Page.Locator("#vault-page")).ToHaveCountAsync(0);
        await Page.Locator("#editor-leave-save").ClickAsync();
        await Expect(Page.Locator("#vault-page")).ToBeVisibleAsync();

        await Page.Locator("#vault-tab-files").ClickAsync();
        await FileItem("Codes.txt").Locator(".file-row").ClickAsync();
        await Expect(editor).ToHaveValueAsync("1234");

        // Close, discarding: the saved text is kept.
        await editor.FillAsync("5678");
        await Page.Locator("#editor-close-button").ClickAsync();
        await Page.Locator("#editor-leave-discard").ClickAsync();
        await Expect(Page.Locator("#vault-files")).ToBeVisibleAsync();
        await FileItem("Codes.txt").Locator(".file-row").ClickAsync();

        await Expect(editor).ToHaveValueAsync("1234");
    }

    [TestMethod]
    public async Task GivenFilesBeingViewedAndEdited_WhenTheBrowsersStorageIsSearched_ThenTheyAreOnlyThereEncrypted_UnderGuidKeys()
    {
        // Check 1 of docs/reviews/2026-11-07-security-checklist.md, for files.
        const string savedText = "SAVED-TEXT-MARKER-4471";
        const string unsavedText = "UNSAVED-TEXT-MARKER-9902";
        var needles = new Dictionary<string, string>
        {
            ["the image's bytes"] = ImageBase64,
            ["the image as base64"] = Convert.ToBase64String(Encoding.ASCII.GetBytes(ImageBase64)),
            ["the saved text"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(savedText)),
            ["the unsaved text"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(unsavedText)),
            ["the image's name"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("passport-scan")),
            ["the text file's name"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("Door codes")),
            ["the folder's name"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("Tax returns")),
        };

        // What must be found, to show the search finds what is there: the start of a ciphertext, and of a file's key.
        var controls = new Dictionary<string, string>
        {
            ["control: ciphertext"] = Convert.ToBase64String(Encoding.ASCII.GetBytes("E2EE")),
            ["control: file key"] = Convert.ToBase64String(Encoding.ASCII.GetBytes("files/blobs/")),
        };
        foreach (var (name, value) in controls)
        {
            needles[name] = value;
        }

        await LoginAsync();
        await CreateVaultAsync("Personal");
        await Page.Locator("#vault-tab-files").ClickAsync();
        await AddImageAsync();
        await Page.Locator("#new-file-button").ClickAsync();
        await Page.Locator("#new-folder").ClickAsync();
        await Page.Locator("#file-name-input").FillAsync("Tax returns");
        await Page.Locator("#file-name-save").ClickAsync();
        await Expect(Page.Locator("#files .file-item[data-folder='Tax returns']")).ToBeVisibleAsync();

        // The editor open, with saved and unsaved text.
        await NewDocumentAsync("#new-text", "Door codes");
        await Page.Locator("#text-editor").FillAsync(savedText);
        await Page.Locator("#editor-save-button").ClickAsync();
        await Expect(Page.Locator("#editor-status")).ToContainTextAsync("Saved at");
        await Page.Locator("#text-editor").FillAsync($"{savedText}\n{unsavedText}");
        await Expect(Page.Locator("#editor-status")).ToHaveTextAsync("Unsaved changes");

        var whileEditing = await ScanStorageAsync(needles);

        // The viewer open, showing the decrypted image.
        await Page.Locator("#editor-close-button").ClickAsync();
        await Page.Locator("#editor-leave-discard").ClickAsync();
        await FileItem(ImageName).Locator(".file-row").ClickAsync();
        await Expect(Page.Locator("#file-viewer-image")).ToBeVisibleAsync();

        var whileViewing = await ScanStorageAsync(needles);

        foreach (var scan in new[] { whileEditing, whileViewing })
        {
            var all = scan["found"]!.AsArray().Select(f => f!.GetValue<string>()).ToList();
            foreach (var control in controls.Keys)
            {
                Assert.IsTrue(all.Any(f => f.StartsWith(control, StringComparison.Ordinal)), $"The search did not find '{control}', so it proves nothing.");
            }

            var found = all.Where(f => !f.StartsWith("control: ", StringComparison.Ordinal)).ToList();
            Assert.AreEqual(0, found.Count, $"Found unencrypted in the browser's storage:{Environment.NewLine}{string.Join(Environment.NewLine, found)}");
            Assert.AreEqual(scan["binaries"]!.GetValue<int>(), scan["encrypted"]!.GetValue<int>(), "Not every stored binary value is E2EE ciphertext.");
        }

        // Two files and a folder record, each with metadata; the two files with content. Named by GUID and nothing else.
        var keys = whileEditing["keys"]!.AsArray().Select(k => k!.GetValue<string>()).ToList();
        var unexpected = keys.Where(k => !FileKey().IsMatch(k)).ToList();
        Assert.AreEqual(0, unexpected.Count, $"File keys that are not a GUID:{Environment.NewLine}{string.Join(Environment.NewLine, unexpected)}");
        Assert.AreEqual(3, keys.Count(k => k.StartsWith("files/meta/", StringComparison.Ordinal)), string.Join(", ", keys));
        Assert.AreEqual(2, keys.Count(k => k.StartsWith("files/blobs/", StringComparison.Ordinal)), string.Join(", ", keys));
        Assert.IsTrue(whileEditing["encrypted"]!.GetValue<int>() >= 5, $"Only {whileEditing["encrypted"]} stored values are E2EE ciphertext.");
    }

    [GeneratedRegex(@"^files/(meta/[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}\.json|blobs/[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}\.bin)$")]
    private static partial Regex FileKey();

    private ILocator FileItem(string name)
    {
        return Page.Locator($"#files .file-item[data-file]:has(.file-name:text-is(\"{name}\"))");
    }

    private async Task AddImageAsync()
    {
        await Page.Locator("#add-files-input").SetInputFilesAsync(new FilePayload { Name = ImageName, MimeType = "image/png", Buffer = Image });
        await Expect(FileItem(ImageName)).ToBeVisibleAsync();
        await Expect(Page.Locator("#files-working")).ToHaveCountAsync(0);
    }

    private async Task NewDocumentAsync(string menuItem, string name)
    {
        await Page.Locator("#new-file-button").ClickAsync();
        await Page.Locator(menuItem).ClickAsync();
        await Page.Locator("#file-name-input").FillAsync(name);
        await Page.Locator("#file-name-save").ClickAsync();
        await Expect(Page.Locator("#text-editor")).ToBeVisibleAsync();
    }

    private async Task<JsonNode> ScanStorageAsync(Dictionary<string, string> needles)
    {
        return JsonNode.Parse(await Page.EvaluateAsync<string>(ScanStorageScript, needles))!;
    }
}
