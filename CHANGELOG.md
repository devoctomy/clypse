# Changelog

What changed in each release of clypse. The version is `VersionPrefix` in `Directory.Build.props`;
`eng/bump-version.ps1` turns the Unreleased section into the new version's section, and the Release workflow uses that
section as the GitHub release's notes.

Changes are logged here from the release after 0.7.0. Each user-visible change goes in Unreleased, under Added,
Changed, Fixed, Removed or Security.

## Unreleased

## 0.9.8

### Added
- **Past versions** of secrets, markdown documents, text files and images. The secret editor, the text editor and the
  image viewer have a **History** dropdown, listing every version synced, newest first. Pick one to see it, read only,
  then go **Back to current** or **Restore this version**, which saves it as the current version; the version it
  replaces stays in the history. With unsaved changes, you are asked whether to save them first. History isn't
  available in offline mode.

### Changed
- Built on blazwiz 0.9.6.

## 0.9.7

### Added
- A vault's page has two tabs: **Secrets**, as before, and **Files**.
- **Files** in a vault: add important documents, such as a scan of a passport, with **Add files**. Each is encrypted
  with the vault's keys and synced like a secret, so it is there offline too. A file can be up to 20 MB.
- Open an image (JPEG, PNG, GIF or WebP) to view it. It is only decrypted then, in memory: nothing decrypted is kept
  on the device, and it is gone again when you close it or the vault locks. Tap the image to see it full screen,
  scaled to fit, and tap it again to go back to the file's details. Other kinds of file are stored and synced, and
  show their details.
- Write and edit text in a vault: **New > Markdown Document** and **New > Text File** make one, and opening a
  markdown or text file (up to 1 MB) shows it in an editor. A markdown document has formatting buttons and a preview
  beside the text; Ctrl+S saves, and Ctrl+B and Ctrl+I make text bold or italic. Closing the editor, changing tab or
  leaving the page with unsaved changes asks whether to save or discard them. Locking the vault drops unsaved text.
- Organise files in folders: **New > Folder** makes one, and a file's details move it. Folders can be renamed and
  deleted, and a file's name and notes changed. Search finds files in any folder by name or notes.

### Fixed
- The vaults page no longer says you have no vaults before it has looked. Straight after signing in, or when the app
  is opened again, it says it is loading your vaults until they have been read, and then shows them, or that there
  are none.
- Reloading the browser on a vault's page no longer says the vault does not exist. It waits for your vaults to be
  read, then asks for the vault's passphrase as usual.

## 0.9.6

### Added
- A vault's **Report** checks its secrets for known weak passwords, weak passwords, reused passwords, reused user
  names (email addresses aren't counted) and passwords not changed for a while. It shows what passed as well as what
  didn't. Each check opens to list the secrets it found, and choosing one opens it. A red dot on the Report button
  shows when there's something to look at. The report updates as you add, change, delete or sync secrets.
- When you open a secret the report has concerns about, its password and user name show a red dot by their title,
  with what the concern is underneath (e.g. "This password is also used by Bank."). The concern goes as soon as you
  change that field.
- Settings > Vault report: choose how long a password can go unchanged before the report counts it as old (3 months
  unless you change it).
- Clypse now records when each password was last changed. Editing a secret's other details doesn't count as a change.
  For secrets saved before this, the last time the secret was saved stands in.
- Import can read clypse's own JSON format. Each secret keeps its type, its notes (line breaks included), and when it
  and its password were last updated.

## 0.9.5

### Added
- An **Untagged** filter on a vault's page shows the secrets that have no tags. It and **All** have their own colour,
  so they stand apart from your tags.

### Changed
- Built on blazwiz 0.9.5.
- A secret's tags show in a row along the bottom of its entry, on phones as well as on larger screens.
- A vault's page says how many secrets are shown out of how many it holds ("Showing 3/40 secrets"), under the tags.
- The vaults page is tidier: the line under its title is gone, and the note about vaults shown by their id appears
  only when one is, with an (i) on that vault.
- The button that forgets a vault's biometric unlock is grey with an eraser, so it no longer looks like a delete.
- Copied passwords and secret keys are no longer cleared from the clipboard unless you turn it on in Settings >
  Clipboard. Clearing writes to the clipboard again, which caused problems on some devices. If you already saved your
  clipboard settings on a device, they're kept there.

- Renaming this device in Settings, when it's remembered, now warns first: its remembered name can't be changed, so
  renaming it forgets it, and your next sign in on it asks for a code from your authenticator app, then remembers it
  under the new name.

### Fixed
- A vault unlocked for the first time on a device shows its name at the top of its page, not its id.

### Security
- Signing in with your passkey no longer leaves the key that unlocks your vaults with biometrics in memory. Every
  unlock with biometrics asks for your fingerprint, face or PIN, including after you lock a vault. Changing a vault's
  passphrase asks for it first when biometrics are on for that vault, so they stay on.

## 0.9.4

### Added
- **Two-step verification, if you want it.** You can turn on a code from an authenticator app (such as Google
  Authenticator, Microsoft Authenticator or 1Password) in your Profile: open the app from the link, scan the QR code
  or copy the key. Signing in then asks for the app's code after your password. It is off until you turn it on, and
  you can turn it off again.
- **Remembered devices.** A device you sign in on with a passkey is not asked for a code again. Settings lists your
  remembered devices, with a Forget button on each; signing out everywhere forgets them all.

### Changed
- Built on blazwiz 0.9.4.

## 0.9.3

### Changed
- Built on blazwiz 0.9.3.
- Opening a dialog no longer selects the text of its first field. The dialog itself takes focus, so Esc still closes
  it.

## 0.9.2

### Added
- The sign-in page shows clypse's version at the bottom, before the copyright message.

### Changed
- Built on blazwiz 0.9.2.
- The sign-in page and the top of the sidebar show the app's name in capitals, in the font of the clypse logo
  (Montserrat ExtraBold). The font comes with the app, so it also shows offline.
- The clypse icon on the sign-in page is larger.

### Fixed
- An update to clypse no longer fails to download after a new version is deployed. Deploys run from GitHub did not
  clear the old copies of the app's files from CloudFront, so the app could be given an old file with the new version
  and reject it (fixed in blazwiz 0.9.2).
- With the sidebar collapsed, the clypse icon at the top lines up with the icons below it. It sat slightly to the
  left.

## 0.9.0

### Added
- The new vault dialog has an **Advanced** tab with the Argon2id settings its passphrase key is made with: memory,
  iterations and parallelism, starting at the app's defaults. You can enter any values. If you change them, they must
  be benchmarked on your device first: Create vault stays disabled until the benchmark has run with those values.
- **Change passphrase** in a vault's settings. It gives the vault new keys and encrypts every secret again, shows how
  long that will take before it starts, and signs you out on every device when it is done. Its Advanced tab starts at
  the vault's current Argon2id settings.

### Removed
- The End to end encryption section in Settings, which listed vaults by id. Everything it did is on clypse's own pages:
  vaults are created and locked on the vaults page, and renamed, deleted and given a new passphrase in a vault's
  settings.

### Changed
- Built on blazwiz 0.9.0.
- The Argon2id panel in Settings is now **Argon2id Benchmarking**, and you can choose what to benchmark. Memory,
  iterations, parallelism and key length can be edited, starting at the values new vaults use; memory can be typed in
  or picked from common sizes (16 to 512 MiB) with the MiB button. Changing them only affects the benchmark, never new
  vaults. A note explains that the app can use at most 2 GiB of memory in the browser, so very large values can run
  out and the page then needs reloading (from blazwiz 0.8.4).

## 0.8.3

### Changed
- Built on blazwiz 0.8.3.

### Fixed
- When more than one vault syncs at the same time, each shows its own progress in its own pill, instead of one pill
  jumping between their counts. The sync status names the vault being synced ("Syncing Work 12 of 201"), or shows only
  the progress for a vault whose name isn't known on this device yet (fixed in blazwiz 0.8.3).

## 0.8.2

### Changed
- Built on blazwiz 0.8.2.
- Importing secrets from a CSV file shows how far it has got ("Importing 120 of 1000…"), instead of only a spinner.
- The sync status shows how far a sync has got ("Syncing 120 of 1000"), from blazwiz.
- Deleting a vault goes straight back to the vaults list, instead of leaving you on the deleted vault's unlock page
  until it is gone. The vault shows "Deleting…" there and cannot be opened, the sync status counts its files as they
  are deleted, and a message says when it is gone (or why it could not be deleted).

## 0.8.1

### Added
- A name to remember you by. Ticking "Remember me" when signing in offers an optional name, shown in the list of
  remembered users instead of your email address (which still shows when you hover over it).

### Changed
- Built on blazwiz 0.8.1.
- Unlocking a vault with biometrics from the vault list now uses blazwiz's biometric unlock, which came from clypse.
  It works as before: the vault is synced first when online, then opens straight onto its secrets.

## 0.8.0

### Changed
- Built on blazwiz 0.8.0.
- Settings > About shows clypse's version. It is set in one place, `VersionPrefix` in `Directory.Build.props`, with
  `eng/bump-version.ps1`, and the Release workflow tags that version instead of asking for one.

### Fixed
- A vault with many secrets can be scrolled through. One turn of the mouse wheel used to run the list to its end, or
  back to its start, unloading everything else on the way (fixed in blazwiz 0.8.0).

## 0.7.0

### Changed
- Built on blazwiz 0.7.0.

