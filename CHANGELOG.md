# Changelog

What changed in each release of clypse. The version is `VersionPrefix` in `Directory.Build.props`;
`eng/bump-version.ps1` turns the Unreleased section into the new version's section, and the Release workflow uses that
section as the GitHub release's notes.

Changes are logged here from the release after 0.7.0. Each user-visible change goes in Unreleased, under Added,
Changed, Fixed, Removed or Security.

## Unreleased

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

