# Changelog

What changed in each release of clypse. The version is `VersionPrefix` in `Directory.Build.props`;
`eng/bump-version.ps1` turns the Unreleased section into the new version's section, and the Release workflow uses that
section as the GitHub release's notes.

Changes are logged here from the release after 0.7.0. Each user-visible change goes in Unreleased, under Added,
Changed, Fixed, Removed or Security.

## Unreleased

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

