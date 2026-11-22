# Changelog

What changed in each release of clypse. The version is `VersionPrefix` in `Directory.Build.props`;
`eng/bump-version.ps1` turns the Unreleased section into the new version's section, and the Release workflow uses that
section as the GitHub release's notes.

Changes are logged here from the release after 0.7.0. Each user-visible change goes in Unreleased, under Added,
Changed, Fixed, Removed or Security.

## Unreleased

## 0.9.2

### Changed
- Built on blazwiz 0.9.2.

### Fixed
- An update to clypse no longer fails to download after a new version is deployed. Deploys run from GitHub did not
  clear the old copies of the app's files from CloudFront, so the app could be given an old file with the new version
  and reject it (fixed in blazwiz 0.9.2).

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

