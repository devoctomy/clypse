# AGENTS.md

Instructions for AI coding agents working on clypse.

## Rules

1. **Never run git commands in this repository.** Not even read-only ones. Version control here is the
   maintainer's alone: they review, commit and push.
   - The same applies to `gh` and any other tool that talks to GitHub.
   - To delete or move files, use plain file operations.
   - If a step seems to need git, stop and ask.
2. **The blazwiz framework repository may be changed, on its current branch only.**
   - Read-only git commands there are fine: `git status`, `git log`, `git diff`, `git show`, `git ls-files`,
     `git branch --list`.
   - Never run git commands that change anything there: no `add`, `commit`, `checkout`/`switch`, `stash`, `reset`,
     `restore`, `fetch`, `pull`, `push`, and no branches or tags.
   - Leave changes uncommitted in its working tree. The maintainer reviews, commits and pushes.
   - Follow blazwiz's own `AGENTS.md` for anything done in that repository.
3. **The original clypse repository is read-only.** Copy from it; never change it.
4. **No AI attribution.** Never add `Co-Authored-By`, "Generated with…" or similar lines to files, commits or PRs.
5. **Never publish anything.** Do not push NuGet packages, create tags or releases, or deploy.
6. **Never run AWS commands or the deploy scripts** (`cloudformation/*.ps1`), unless the maintainer asks for that exact
   action. They create, change or delete real AWS resources.
7. If an action is blocked or fails, stop and report it. Don't substitute a different or more destructive action.
8. **Report honestly how things were checked.**
   - Say exactly what was run: which suites, which configuration.
   - Say what was skipped, and why.
   - A pass with rules or tests switched off is not a pass.
   - If something failed and you could not find out why, say so.
9. **When reviewing the maintainer's own edits, list the fixes.** Don't hand back a rewritten copy of their work.

## Project

clypse is a password manager, rewritten as an app on the blazwiz framework (`Devoctomy.Blazwiz.*` packages from
NuGet). It runs as a Blazor WebAssembly PWA and signs in with AWS Cognito. Data syncs offline-first to S3, encrypted
end to end.

- **A vault is a blazwiz protected sync profile.** It has its own passphrase and keys, its own lock, and syncs
  `profiles/<id>/**`. The profile holds:
  - its name and description, in an encrypted `.vault` file;
  - each **secret**, as one encrypted JSON file, `secrets/<secretId>.json`.
- **Users start with no vaults.** The app defines no sync profiles in code.
- **The plan:** `docs/plans/2026-11-06-blazwiz-rewrite-plan.md` holds the phases and decisions (D1 to D8). Read it
  before structural work, keep it up to date as work is done, and record changed decisions there.
- **The reference app:** the template's notes feature in blazwiz (`template/MyApp.Portal*`, `NotebookService`,
  `NoteService`) shows how to use protected profiles, file stores and `E2eUnlockPanel`. Use it as the reference.

Layout:

- `Clypse.Core`: the domain code ported from the old `clypse.core`:
  - the secret model (`ClypseObject`, `Secret`, `WebSecret`, `AwsCredentials`, `SecretFieldAttribute`);
  - password generation and complexity estimation;
  - embedded word lists;
  - CSV import.

  It has no references to AWS, JSInterop or cryptography libraries.
- `Clypse.Core.UnitTests`: its unit tests.
- `Clypse.Portal`: the WASM host (`Program.cs`, pages, `wwwroot`).
- `Clypse.Portal.Application`: services and view models.
- `Clypse.Portal.Models`: models.
- `Clypse.Portal.Application.UnitTests` and `Clypse.Portal.UITests`: tests.
- `cloudformation/`: the app's AWS stack, on top of the blazwiz base stack.
- `eng/`: build scripts, e.g. `use-local-blazwiz.ps1` (see "blazwiz packages").
- `Resources/Art`: icons and logos. **Use only the PNGs.** The SVGs render with the wrong font (Montserrat Black is
  not embedded); see the plan.

The project is in development and unreleased. Nobody uses it yet.

## Design rules

These follow blazwiz's rules, so the app and the framework read the same.

- **Only the old `clypse.core` C# and its unit tests are reused.** None of the old UI (views, view models, CSS, JS,
  portal services) is brought over: the UI is designed fresh on blazwiz.
- **Secret files use clypse's JSON format:** the `Secret.Data` dictionary, as serialised by `ClypseObject`.
- **Name secret files by GUID.** File paths are not encrypted, so a secret's name, or anything else about it, must
  never appear in a file key.
- **No backward compatibility before 1.0.** No migrations, legacy keys or shims. Nothing is migrated from the old
  clypse.
- **No static helper classes.** Logic goes in injectable services: an interface plus an implementation, registered
  through an `Add*` extension method.
- **No hand-rolled cryptography.**
  - Encryption, keys and key derivation come from blazwiz (`Devoctomy.Blazwiz.Cryptography` / `.Encryption`).
  - Random values come from `RandomNumberGenerator`.
- **MVVM.**
  - Razor files are markup only; each view inherits `MvvmComponentBase<TViewModel>` (Blazing.Mvvm).
  - The logic lives in view models (CommunityToolkit.Mvvm).
- **User-facing text comes from `.resx` resources, never literals.** The pseudo-localisation UI test fails on visible
  text that does not.
- **Browser storage uses short names only.** The framework's storage clients add the app's prefix (`IAppStorageKeys`).
- **Use `TimeProvider`** for time, and `FakeTimeProvider` in tests.
- **Large text lists load in full only in Release builds.**
  - Loading the embedded lists (the weak-known-passwords list, possibly the dictionary word lists) is far too slow in
    a Debug WASM build, so Debug uses a small stand-in.
  - Choose the full or stand-in list through a service or option, not `#if DEBUG` scattered through the code.
  - Cache each list once it is loaded.
- **Unit tests:**
  - Name them `Given<State>_When<Action>_Then<Outcome>`.
  - Mark the parts of every test method with `// Arrange`, `// Act` and `// Assert` comments (`// Act & Assert` where
    the assert does the acting, e.g. `Assert.Throws`).
  - Test projects end in `.UnitTests`, and their namespaces must contain that suffix.
- **Generic gaps go into blazwiz, not into clypse.** Clipboard with auto-clear, toasts and conflict resolution belong
  in the framework.
  - Make those changes in the blazwiz repository (rule 2), with tests there.
  - clypse picks them up from a local build of blazwiz (see "blazwiz packages" below), never by copying framework code
    into clypse.

## Build and test

Prerequisites: the .NET SDK pinned in `global.json`, and a blazwiz working tree (see "blazwiz packages").

```powershell
./eng/use-local-blazwiz.ps1 -BlazwizPath <blazwiz working tree>   # or set BLAZWIZ_SOURCE
dotnet build Clypse.slnx
dotnet test Clypse.slnx --no-build --filter "FullyQualifiedName~UnitTests"
dotnet test Clypse.slnx --no-build --filter "FullyQualifiedName~UITests"
```

- **Warnings are errors**, and StyleCop runs on the projects. Rules turned off for the whole solution are in
  `analyzers.globalconfig`. Fix warnings; don't suppress them without the maintainer's agreement.
- **UI tests:**
  - They start the portal themselves and need the Playwright browser installed once:

    ```powershell
    pwsh Clypse.Portal.UITests/bin/Debug/net10.0/playwright.ps1 install chromium
    ```

  - Don't build while they run.
- **Static web assets compression failures.** If a build fails in that step ("An item with the same key has already
  been added"), build again with `MSBUILDDISABLENODEREUSE=1`.

## blazwiz packages

Until this rewrite is finished, no blazwiz changes are released to nuget.org. clypse uses blazwiz packages packed
locally from the blazwiz working tree:

- `eng/use-local-blazwiz.ps1` does all of it:
  - packs every `Devoctomy.Blazwiz.*` package into `.blazwiz/packages` (git ignored), with a version unique to the run
    (`<VersionPrefix>-local.<timestamp>`), so the NuGet cache never serves an older build;
  - sets `BlazwizVersion` in `Directory.Packages.props` to that version;
  - removes older local builds;
  - restores.

  `nuget.config` adds `.blazwiz/packages` as a package source.
- Run it after changing blazwiz, before building or testing clypse against the change, and in a fresh clone before the
  first restore.
- Never edit `BlazwizVersion` by hand, and never point clypse at blazwiz project references.
- Before work is done, rebuild the packages from the current blazwiz tree and run clypse's full test suite on them.
