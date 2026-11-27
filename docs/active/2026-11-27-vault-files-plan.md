# Plan: files in a vault

**Date:** 2026-11-27
**Status:** not started. A plan only: nothing here is built or measured yet.
**blazwiz:** no changes needed for this plan (see "Does this need blazwiz?"). Checked against blazwiz 0.9.5, the
version clypse uses.

## Goal

1. **The vault page has two tabs:** Secrets (what it shows today) and Files.
2. **A file can be added to a vault**, e.g. a scan of a passport. It is encrypted with the vault's keys and synced like
   a secret.
3. **Files are synced to each device, so they are available offline.** What is kept on the device is the encrypted
   file, as with secrets.
4. **A file's binary is decrypted only when it is viewed, in the app.** The binary is the file's content: the blob in
   `files/blobs`. Decrypted, it is held in memory while it is shown and never written to the browser's storage.
5. **A file's metadata is stored apart from its binary, and is decrypted when the vault is unlocked, in the same way
   the secrets are.** So the Files tab can list, search and organise files without decrypting any binary.
6. **Files are organised in virtual folders**, held in the metadata. The real paths in S3 stay flat.
7. **Text files can be edited in the app.** Markdown has an editor and a preview, like blazwiz's notes.

**Scope:** a few important documents in a vault. It is not meant for gigabytes of files.

Everything stays inside the vault's existing protected profile:

| Key, under `profiles/<vaultId>/` | Holds |
|---|---|
| `secrets/<secretId>.json` | a secret (as today) |
| `files/meta/<id>.json` | the metadata of one file, or the record of one empty folder |
| `files/blobs/<id>.bin` | the content of one file |

## How things are today

Checked in the code, clypse and blazwiz 0.9.5:

- **A vault's file store already takes any bytes.** `ISyncFileStore.WriteAsync(key, byte[])` encrypts with the vault's
  keys, bound to the vault and the file's path, and tracks the file for sync. `SecretService` uses it for secrets.
- **The device only ever stores ciphertext.** The store app code gets is `EncryptingSyncFileStore` over
  `TrackingSyncFileStore` over the real storage (IndexedDB, OPFS or a local folder). Encryption happens before the
  storage is reached, and the sync engine uploads the stored bytes as they are. So plaintext exists only in memory.
- **A vault syncs `profiles/<id>/**/*`.** New folders under it sync with no configuration.
- **Listing files does not read their content.** The IndexedDB store leaves `content` out of a listing, so listing a
  vault with large files in it stays quick.
- **Secrets are found by prefix and GUID** (`SecretService.GetSecretId`), so files under `files/` are never taken for
  secrets.
- **`ISyncService.FilesChanged`** gives the keys a sync pass changed. `VaultSession` uses it to re-read secrets.
- **The page has no tabs.** `VaultPage.razor` shows the secrets inside `E2eUnlockPanel`. Bootstrap's `nav-tabs` styles
  are already loaded (blazwiz's `E2ePassphraseForm` uses them).
- **clypse has no JavaScript of its own** and no `IJSRuntime` use.
- **blazwiz's notes editor is in its template app, not in a package.** `template/MyApp.Portal*` holds all of it, so
  clypse cannot reference it:
  - the editor page: a formatting toolbar, a text area and a live preview that can be hidden;
  - `MarkdownRenderer`: Markdig 1.4.0, with raw HTML turned off and links limited to `http`, `https` and `mailto`;
  - `MarkdownFormatService`: works out the edit each toolbar button makes;
  - `notes.js`: the text area's selection, edits that keep the browser's undo history, and Ctrl+S, B and I;
  - asking before leaving with unsaved changes, and asking which version to keep when a sync changes the open note.
- **A vault's idle timer restarts only when its files are read or written** (`E2eIdleLock`). Typing does not restart
  it. Clypse sets it to 5 minutes by default.
- **The deployed Content-Security-Policy** (added by blazwiz's deploy, `Add-ContentSecurityPolicy`) allows
  `img-src 'self' data:`. It has `object-src 'none'`, and frames fall under `default-src 'self'`. Local runs have no
  policy.

## Does this need blazwiz?

**No, not for what this plan builds.** Storage, encryption, sync and locking all come from the vault's existing file
store. Showing images needs no JavaScript and fits the deployed policy.

**The text editor is built in clypse for now**, and possibly backported to blazwiz later. blazwiz's
editor and preview are template code, so clypse gets its own, written with the template as the reference: a markdown
renderer and a format service, Markdig as a new package, and a small script for the text area. That script is
clypse's first JavaScript.

blazwiz does set limits this plan has to live within:

- **Each file is held whole in memory** to encrypt it, store it and upload it, in several copies. Uploads are one S3
  `PUT` each.
- **Encryption is managed AES-GCM (Bouncy Castle), run on the UI thread.** The page cannot respond while a file is
  encrypted or decrypted. How long that takes for a file of several megabytes is not known yet (see Phase 3).
- **Each app start hashes every local file** (the verify pass), content included.
- **A passphrase change encrypts and uploads every file again.**
- **A pass will not download a file over 100 MiB** (`SyncDefaults.MaxFileSize`).

So this plan caps the size of a file (see "Limits"). Things that would need a blazwiz change are listed under "Not in
this plan", each with what it would need. None is done here, and each would be proposed and agreed first.

## Design

### When things are decrypted

- **Metadata: when the vault is unlocked, in the same way the secrets are.** It is read and decrypted into the
  session, kept in memory while the vault is unlocked, and forgotten when it locks.
- **Binary: only when the file is viewed.** It is never read into the session.

### Keys

- **GUIDs only.** `files/meta/<id>.json` and `files/blobs/<id>.bin`, where `<id>` is a GUID in `D` format. Paths are
  not encrypted, so nothing about a file appears in its key: no name, no real extension, no folder.
- **A file's metadata and content share one id.** An empty folder's record has no content.
- **What S3 can still see:** how many files a vault has, roughly how large each is, and when each changed. The same is
  true of secrets today.

### Metadata

One JSON file per file, serialised with `System.Text.Json` like `VaultDetails`:

```json
{
  "Id": "2f0c6a1e-…",
  "Kind": "File",
  "Name": "Passport.jpg",
  "Folder": "Travel/Passports",
  "ContentType": "image/jpeg",
  "Size": 2481133,
  "Notes": "Expires 2031",
  "CreatedAt": "2026-11-27T10:15:00Z",
  "LastUpdatedAt": "2026-11-27T10:15:00Z"
}
```

And one per empty folder:

```json
{
  "Id": "8b1e44d0-…",
  "Kind": "Folder",
  "Folder": "Travel/Visas",
  "CreatedAt": "2026-11-27T10:20:00Z",
  "LastUpdatedAt": "2026-11-27T10:20:00Z"
}
```

- `Kind` is `File` or `Folder`.
- `Folder` is a folder's path, its names joined by `/`. For a file, it is the folder the file is in, and empty at the
  top level. For a folder record, it is the empty folder the record keeps.
- `Id` must match the file's name, as `SecretService` checks for secrets. Otherwise the entry is unreadable.
- Times come from `TimeProvider`.

### Folders

- **A file's metadata holds the path of the folder it is in.** A folder with a file in it needs no record: it exists
  because the file says so. The same goes for every folder further up the path.
- **An empty folder is kept by a record**, because no file names it.
  - Making a folder writes its record.
  - Putting a file into a folder removes the folder's record: it is not needed any more.
  - When the last thing in a folder is deleted or moved out, a record is written for it, so the folder stays.
  - A record left over for a folder that has files (e.g. after a sync) does no harm. The folders shown are those
    named by any file or any record.
- **A folder's name cannot contain `/`.**
- **Renaming or moving a folder rewrites the path in every file and record under it.** If that is cut short, both
  the old and the new folder show, each with some of the files. Nothing is lost, and doing it again finishes it.
- **File names need not be unique** in a folder. Two devices could make the same name anyway; ids tell them apart.
- **Deleting a folder deletes what is in it**, after a confirmation that says how many files that is.

### Adding, changing and deleting

- **Text files can be edited. Other files cannot be changed in place.** An image or a PDF can be renamed, moved,
  given notes and deleted. To change its content, delete it and add the new one.
- **Adding** writes the content first, then the metadata. **Deleting** removes the metadata first, then the content.
  If either is cut short, what is left is content with no metadata: unseen, and wasting space, but never a listed
  file that cannot be opened.
- **Saving a text file** writes the content, then the metadata (its size and when it changed).
- **Sync conflicts** are settled per file by blazwiz (newest wins), as for secrets:
  - Two devices edit the same metadata: the newest edit wins whole.
  - Two devices edit the same text file: its content and its metadata are settled separately. If they end up from
    different devices, only the size or time shown can be out of date. The text shown is always the content's.
  - One device deletes a file while another edits its metadata, and the edit is newer: the metadata comes back
    without its content. It is listed as having no content, and can be deleted.
- **Content not here yet.** During a sync the metadata can arrive before the content. The session knows which content
  is on the device from the file listing (no decrypting), so the row says the content is still on its way, and
  viewing waits.

### Viewing

- **Opening a file reads and decrypts its content into memory**, then shows it by type:
  - **Images** (`image/jpeg`, `image/png`, `image/gif`, `image/webp`): an `<img>` whose source is a `data:` URI.
  - **Text**: opens in the editor (see "Editing text").
  - **Anything else** (PDF, Office documents, SVG and so on): its details only, and a line saying it cannot be shown
    in the app yet. It is still stored and synced.
- **The type comes from the metadata**: what the browser reported when the file was added, or the name's extension
  when it reported nothing. Only the types above are ever rendered. Apart from the markdown preview, nothing is
  rendered as HTML.
- **`data:` URIs, not `blob:` URLs.** They need no JavaScript, the deployed policy already allows them for images, and
  a browser may keep large `blob:` data in temporary files on disk.
- **Nothing is cached.** Each view decrypts again. Closing the viewer drops the content, and the byte array is zeroed.
  The base64 text built for an image cannot be wiped: it goes when the runtime collects it. Secrets held in memory
  are the same.

### Editing text

- **Which files:** plain text (`text/plain`, `.txt`) and markdown (`text/markdown`, `.md`, `.markdown`), up to 1 MB.
- **Markdown has an editor and a preview, like blazwiz's notes:** a formatting toolbar, the text area, and a live
  preview beside it that can be hidden. Plain text has the text area only.
- **The editor opens in the Files tab, in place of the list.** Not on a page of its own, as the template's is:
  leaving the vault page forgets the decrypted secrets, and they would all be read again on the way back.
- **The text's content is decrypted when the file is opened**, like any other file's.
- **Saving** is a button and Ctrl+S.
- **Leaving with unsaved changes asks first:** save, discard or keep editing. That covers closing the editor, changing
  tab and leaving the page.
- **Changed by a sync while open:** with no unsaved changes, the editor reloads. With unsaved changes, it asks which
  version to keep. If the sync deleted the file, it asks whether to keep this version or let it go.
- **The preview is the one place HTML is rendered.** The renderer does what the template's does: raw HTML in the
  markdown is turned off, and links are limited to `http`, `https` and `mailto`. Under the deployed policy, images
  from other sites do not load in a preview.
- **The toolbar needs a small script** for the text area's selection, for edits that keep the browser's undo history,
  and for the shortcuts. It is one of the app's own files, so the deployed policy allows it.

### Locking

- **The session keeps metadata only**, and forgets it when the vault locks, as `VaultSession` forgets secrets.
- **Locking closes the viewer** and every other dialog. `VaultViewModel` already closes its dialogs when the vault
  locks; the viewer drops its content at the same time.
- **Reading a file counts as use**, so it restarts the idle timer (the store does this). Looking at an open image does
  not: the vault can lock while it is on screen, and the viewer closes.
- **Typing in the editor counts as use.** The editor restarts the idle timer as the user types
  (`IE2eIdleLock.Touch`), so the vault does not lock in the middle of writing.
- **Locking forgets everything decrypted.** Unsaved edits in the editor are discarded with the rest, whether the vault
  was locked by hand, from another tab, or by being left idle.

### Limits

- **A file can be at most N MB**, set in `ClypseSettings` and checked before the file is read. The working figure is
  20 MB; Phase 3 measures and sets it.
- **Text opens in the editor up to 1 MB.** A larger text file is stored and synced like any other file.
- **Several files can be added at once.** They are read, encrypted and stored one at a time, with progress.

## The page

- **Tabs** under the page header, inside the unlocked panel: Secrets and Files. Bootstrap `nav-tabs`, with the tab
  roles (`tablist`, `tab`, `tabpanel`).
  - Secrets is the tab shown first, so the page opens as it does today and the existing tests are unaffected.
  - The secrets markup keeps its ids.
- **Header buttons follow the tab.**
  - Always: Lock, Settings.
  - Secrets: Import, Report, New secret.
  - Files: Add files, and New.
- **New** is a button with a menu of three options: Folder, Markdown Document and Text File.
  - Each asks for a name, and makes it in the open folder.
  - A new markdown document or text file starts empty and opens in the editor.
  - The menu is Bootstrap's `dropdown-menu`, opened and closed by the view model, with no Bootstrap JavaScript, as
    blazwiz's `Argon2idEditor` does it.
- **Files tab:**
  - a breadcrumb: the vault, then each folder down to the open one;
  - search, across every folder, by name and notes. Results show which folder each file is in;
  - the open folder's contents: folders first, then files, by name. Each row has an icon for its type, its name, and
    a line with its type, size and when it was added;
  - row actions: open (the viewer for an image, the editor for text), details (name, folder, notes), delete;
  - reading progress and an "unreadable" warning, as the Secrets tab has;
  - an empty state with the Add files button.
- **Add files** is an `InputFile` with `multiple`, reached through a `<label>` styled as a button, so it needs no
  JavaScript. The code-behind reads each file, as `ImportSecretsDialog.razor.cs` does.
- **Dialogs:** the viewer (an image, its details, and delete), the details dialog (name, folder, notes), and the name
  dialog (a new folder, markdown document or text file, and renaming a folder). Deleting asks first, in the same way
  the secret editor does.
- **The text editor** takes the place of the list in the Files tab while a text file is open, with Save and Close.
- **All text from `ClypseText.resx`.** File and folder names and notes are user content (`data-user-content`).

## Code

### `Clypse.Portal.Models/Files`

- `VaultFileEntry`: the metadata above, for a file or an empty folder's record.
- `VaultFileKind`: `File`, `Folder`.
- `FileViewKind`: `Image`, `Text`, `Markdown`, `None`.
- `VaultFolderView`: one folder as shown: its breadcrumb, folders and files.
- `MarkdownFormat`, `MarkdownTextEdit`, `EditorSelection`: a toolbar button's format, the edit it makes, and the text
  area's selection, after the template's.

### `Clypse.Portal.Application/Services`

Each an interface and an implementation, registered in `AddClypse`:

- **`IVaultFileService`** (made per vault by **`IVaultFileServiceFactory`**, over the vault's `ISyncFileStore`, as
  `SecretService` is):
  - lists entry ids, and which have content on the device;
  - reads, adds, updates and deletes entries;
  - adds a file (content, then metadata) and deletes one (metadata, then content);
  - makes, renames, moves and deletes folders, keeping the records of empty folders as "Folders" says;
  - reads a file's content;
  - saves a text file's content (content, then metadata);
  - turns a key into an entry id or a content id, for `FilesChanged`.
- **`IVaultFileSession`** (scoped, like `IVaultSession`):
  - holds the open vault's entries (metadata only) and which have content here;
  - opened and closed by `VaultViewModel`, next to `IVaultSession`;
  - reads and decrypts the metadata when the vault is unlocked, in the same way `VaultSession` reads the secrets: in
    batches, with progress;
  - re-reads what `FilesChanged` names, and forgets everything on lock.
- **`IVaultFileTreeService`**: turns the entries into what the page shows. The folders are those named by any file's
  path or any folder record. It gives a folder's contents in order, its breadcrumb, everything under a folder (for
  renaming, moving and deleting it), and every folder's path (for the folder picker).
- **`IFileTypeService`**: a file's content type from its name when the browser gives none, how it is viewed
  (`FileViewKind`), its icon, and its size as text.
- **`IMarkdownRenderer`**: markdown to HTML for the preview, with Markdig: raw HTML off, unsafe links removed.
- **`IMarkdownFormatService`**: works out the edit a toolbar button makes.
- **`ITextEditorInteropService`**: the text area's selection and edits in the browser. Its implementation uses
  `IJSRuntime` and lives in `Clypse.Portal`, as the template's does.

New error codes in `ClypseErrorCodes`, each with its text: a file's id is not a GUID, an entry is unreadable, a name
is missing, a folder's name contains `/`, a file is too large, a file's content is not on this device, a text file is
too large to edit.

### `Clypse.Portal.Application/ViewModels`

- **`VaultViewModel`:** the active tab, a command to change it, and opening and closing the file session with the
  vault.
- **`VaultFilesViewModel`:** the open folder, search, the rows, adding files (with progress and per-file errors), the
  New menu and what it makes, folders, details and delete, which dialog is open, and whether the editor is open.
- **`FileViewerViewModel`:** reads the content when opened, exposes the image's `data:` URI, and drops it on close and
  on lock.
- **`TextFileEditorViewModel`:** reads the text when opened, the preview, the toolbar, saving, asking before leaving,
  what to do when a sync changes the open file, and dropping the text on close and on lock.

Before each encrypt or decrypt the view model shows that it is working and lets the page paint, because the page
cannot respond until it finishes.

### `Clypse.Portal`

- `VaultPage.razor`: the tabs, and the header buttons by tab.
- `Components/VaultFilesPanel.razor`, `FileViewerDialog.razor`, `FileDetailsDialog.razor`, `FileNameDialog.razor`:
  markup only, each inheriting `MvvmComponentBase<TViewModel>`.
- `Components/TextFileEditorPanel.razor` and its code-behind, which wires up the shortcuts and the check before
  leaving, as the template's `NoteEditor.razor.cs` does.
- `wwwroot/js/editor.js`, loaded from `index.html`, and `Services/TextEditorInteropService.cs`.
- `wwwroot/css/app.css`: the tabs, the file list (after the secret list's pattern), the breadcrumb, the viewer, the
  editor and the preview.
- `Directory.Packages.props`: Markdig.

## Phases

1. **Storage.** The models, `IVaultFileService`, its factory, `IVaultFileTreeService`, `IFileTypeService`, the error
   codes and their text, with unit tests. Nothing visible yet.
2. **The tab and the list.** The tabs, `IVaultFileSession`, the Files tab's list, the New menu with Folder, folders,
   and adding, moving, renaming and deleting files. Check it in the running portal.
3. **Measure, then set the limit.** Stop here and report before going on.
   - Time adding and reading back files of 1, 5, 10 and 20 MB in three builds: Debug, the UI tests' build (Release,
     interpreted) and an AOT publish. The AOT publish is the one that matters, and needs the maintainer to ask for
     it (AGENTS.md).
   - Watch the tab's memory while doing it.
   - Set the size limit from the numbers.
   - **If a 10 MB file takes more than about 5 seconds to encrypt or decrypt in the AOT build, stop.** That cannot be
     fixed in clypse: it would need faster or chunked encryption in blazwiz, proposed and agreed first.
4. **The viewer.** Images, and dropping the content on close and on lock.
5. **The text editor.** The renderer, the format service, the script, the editor and preview, saving, asking before
   leaving, a sync changing the open file, and Markdown Document and Text File in the New menu. Check it in the
   running portal.
6. **Tests, checks and documents** (below).

## Tests and checks

- **Unit tests** (`Clypse.Portal.Application.UnitTests`), for each new service and view model. Among them:
  - keys hold a GUID and nothing else;
  - content is written before metadata, and metadata deleted before content;
  - an unreadable or mismatched entry is counted, not shown;
  - a file over the limit is refused before it is read;
  - the session never holds content, and forgets on lock;
  - a folder with a file in it shows without a record, and so does every folder above it;
  - making a folder writes its record, putting a file in removes it, and emptying the folder writes it again;
  - renaming or moving a folder rewrites the path in everything under it;
  - deleting a folder deletes the files and records under it;
  - locking discards the editor's unsaved text;
  - only the allowed types get a `data:` URI or open in the editor;
  - the viewer drops its content on close and on lock;
  - the renderer leaves raw HTML and unsafe links out of the preview;
  - each toolbar button makes the right edit;
  - saving a text file writes the content, then the metadata;
  - the editor asks before leaving with unsaved changes, and when a sync changes or deletes the open file;
  - `SecretService` ignores keys under `files/`.
- **UI tests** (`Clypse.Portal.UITests`, a new `VaultFilesTests`), with files of a few kilobytes because that build
  runs interpreted:
  - the tabs switch, and Secrets is shown first;
  - add an image: it is listed, and the viewer shows it;
  - the New menu offers Folder, Markdown Document and Text File;
  - make a folder, move a file into it, rename it, delete it; an emptied folder is still there after a reload;
  - locking with the viewer open closes it;
  - make a markdown document: the editor opens; type, and the preview follows; a toolbar button formats the
    selection; save, close and open it again, and the text is there;
  - make a text file: the editor opens with no toolbar or preview;
  - closing the editor with unsaved changes asks first;
  - the browser's storage holds the content only as `E2EE` ciphertext under a GUID key, and the image's bytes, the
    text and the names appear nowhere in localStorage, sessionStorage or IndexedDB. This is check 1 of
    `docs/reviews/2026-11-07-security-checklist.md`, done there by hand for secrets, as a test for files;
  - the pseudo-localisation test covers the Files tab, its dialogs and the editor.
- **Deploy tests** (`Clypse.Portal.DeployTests`, a new `Files.feature`), in the shared "Deploy tests" vault: add a
  small image, see it listed, view it, delete it; and add a small text file, edit it, see the change on opening it
  again, delete it. Each first removes a file of that name left by a failed run. Added to `TestRunOrder`.
- **Under the Content-Security-Policy.** Local runs have no policy, so check the viewer and the editor once with the
  deploy's policy in place: no violations in the console, the image shows, and the toolbar and preview work. Either
  the way
  `docs/reviews/2026-11-07-security-checklist.md` did it (a local AOT publish with the policy copied into
  `index.html`), or on the Test environment.
- **By hand, against the Test environment:** add a file on one device and view it on another, view a synced file
  while offline, and add one offline and see it sync.
- **The full suites** once, at the end, then Codacy on the pushed branch.

## Documents

- `CHANGELOG.md`, `Unreleased`, Added: files in a vault, editing text files, and the tabs.
- `AGENTS.md`, "Project": what a vault's profile holds (`files/meta`, `files/blobs`), and that file keys are GUIDs
  like secret keys.

## Not in this plan

Each of these is a later piece of work, proposed first.

| Later | Needs |
|---|---|
| Viewing PDFs in the app | The browser's own PDF viewer cannot be used: it needs a frame or `<object>`, which the deployed policy refuses, and phone browsers mostly do not show PDFs inline anyway. Either clypse bundles PDF.js and draws pages on a canvas (clypse's first JavaScript, to be checked under the policy), or blazwiz gains a generic document viewer. |
| Saving a copy to the device | Deliberately puts plaintext on disk, so it needs the maintainer's decision. Small files could use a link with a `data:` URI; larger ones need a `blob:` download, which is a generic browser service and so a blazwiz candidate. |
| Replacing the content of a file that is not text, or keeping versions | A design for it. |
| Tags and thumbnails | Tags are a metadata field. A thumbnail is a third encrypted object, or a field in the metadata. |
| Removing content left with no metadata | Care: content can arrive before its metadata during a sync. |
| Files larger than the limit | blazwiz: chunked or streamed encryption, multipart upload, and a verify pass that does not hash everything. |
| Backporting the editor and preview to blazwiz | A package for them in blazwiz, used by the template and clypse. |
