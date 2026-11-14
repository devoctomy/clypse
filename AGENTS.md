# AGENTS.md

Instructions for AI coding agents working on clypse.

## Rules

1. **Never run git commands in this repository.** Not even read-only ones. Version control here is the
   maintainer's alone: they review, commit and push.
   - The same applies to `gh` and any other tool that talks to GitHub.
   - To delete or move files, use plain file operations.
   - If a step seems to need git, stop and ask.
2. **NEVER change another repository unless the maintainer has discussed and agreed it beforehand.** This includes
   blazwiz, even for a change the design rules say belongs there.
   - When a fix or feature could go in another repository, stop and explain:
     - what the change is;
     - why it belongs there rather than in clypse;
     - what it would mean for clypse (e.g. a local blazwiz build).

     Then wait for the maintainer's agreement before editing that repository or running `eng/use-local-blazwiz.ps1`.
   - Agreement covers only the change discussed. A further change there needs agreeing again.
3. **Once agreed, blazwiz may be changed, on its current branch only.**
   - Read-only git commands there are fine: `git status`, `git log`, `git diff`, `git show`, `git ls-files`,
     `git branch --list`.
   - Never run git commands that change anything there: no `add`, `commit`, `checkout`/`switch`, `stash`, `reset`,
     `restore`, `fetch`, `pull`, `push`, and no branches or tags.
   - Leave changes uncommitted in its working tree. The maintainer reviews, commits and pushes.
   - Follow blazwiz's own `AGENTS.md` for anything done in that repository.
4. **The original clypse repository is read-only.** Copy from it; never change it.
5. **No AI attribution.** Never add `Co-Authored-By`, "Generated with…" or similar lines to files, commits or PRs.
6. **Never publish anything.** Do not push NuGet packages, create tags or releases, or deploy.
7. **Never run AWS commands or the deploy scripts** (`cloudformation/*.ps1`), unless the maintainer asks for that exact
   action. They create, change or delete real AWS resources.
8. If an action is blocked or fails, stop and report it. Don't substitute a different or more destructive action.
9. **Report honestly how things were checked.**
   - Say exactly what was run: which suites, which configuration.
   - Say what was skipped, and why.
   - A pass with rules or tests switched off is not a pass.
   - If something failed and you could not find out why, say so.
10. **When reviewing the maintainer's own edits, list the fixes.** Don't hand back a rewritten copy of their work.
11. **A question or an instruction for the maintainer always comes last.** Never ask a question, or give steps for them
    to follow, and then carry on with tool calls: their output pushes it off screen.
    - Either stop and wait for their answer,
    - or do the tool calls first, and ask or instruct at the end of the reply.

## Project

clypse is a password manager, rewritten as an app on the blazwiz framework (`Devoctomy.Blazwiz.*` packages from
NuGet). It runs as a Blazor WebAssembly PWA and signs in with AWS Cognito. Data syncs offline-first to S3, encrypted
end to end.

- **A vault is a blazwiz protected sync profile.** It has its own passphrase and keys, its own lock, and syncs
  `profiles/<id>/**`. The profile holds:
  - its name and description, in an encrypted `.vault` file;
  - each **secret**, as one encrypted JSON file, `secrets/<secretId>.json`.
- **Users start with no vaults.** The app defines no sync profiles in code.
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
- `Clypse.Portal.DeployTests`: UI tests of a deployed environment, signing in with the real user pool. Run by the
  Deploy tests workflow; skipped without their `CLYPSE_DEPLOYTEST_*` variables (`cloudformation/README.md`).
- `cloudformation/`: the app's AWS stack, on top of the blazwiz base stack.
- `eng/`: build scripts, e.g. `use-local-blazwiz.ps1` (see "blazwiz packages").
- `Resources/Art`: icons and logos. **Use only the PNGs.** The SVGs render with the wrong font (Montserrat Black is
  not embedded).

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
  - Propose them first and wait for the maintainer's agreement (rule 2). Only then make them in the blazwiz repository
    (rule 3), with tests there.
  - clypse picks them up from a local build of blazwiz (see "blazwiz packages" below), never by copying framework code
    into clypse.

## Build and test

Prerequisites: the .NET SDK pinned in `global.json`, the `wasm-tools` workload, and a blazwiz working tree only while
clypse uses a local blazwiz build (see "blazwiz packages").

```powershell
./eng/use-local-blazwiz.ps1 -BlazwizPath <blazwiz working tree>   # only on a local blazwiz build; or set BLAZWIZ_SOURCE
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
- **Deploy tests** (`Clypse.Portal.DeployTests`) run against a deployed environment, from the Deploy tests workflow.
  They test clypse's own UI only; what the blazwiz base stack does is tested by blazwiz. No CI filter runs them.
- **Never work around a build failure.**
  - If a build fails for a reason you don't understand, stop and show the maintainer the full error. Don't retry with
    something changed until it passes.
  - Never add a build flag, environment variable, clean-up step (deleting `bin` or `obj`) or build server shutdown to
    get past a failure: not in workflows, scripts, project files or test code, and not in your own shell.

## blazwiz packages

clypse uses either a released blazwiz from nuget.org, or blazwiz packed locally from the blazwiz working tree while a
blazwiz change is being made:

- **`eng/use-local-blazwiz.ps1`** switches to the local working tree. It:
  - packs every `Devoctomy.Blazwiz.*` package into `.blazwiz/packages`, with a version unique to the run
    (`<VersionPrefix>-local.<timestamp>`), so the NuGet cache never serves an older build;
  - sets `BlazwizVersion` in `Directory.Packages.props` to that version;
  - removes older local builds;
  - restores.

  `nuget.config` adds `.blazwiz/packages` as a package source. The folder is in git, empty (`.gitkeep`), because NuGet
  fails a restore when a local source folder is missing. The packages in it are git ignored.
- **When to run it:** after changing blazwiz, before building or testing clypse against the change.
- **`eng/use-released-blazwiz.ps1 -Version <version>`** switches back to a released version. It sets `BlazwizVersion`,
  removes the local builds and restores.
- **What can run on a local build:** the CI, deploy and upgrade workflows only run on a released version
  (`eng/check-blazwiz-version.ps1`). A commit with a `-local.` version fails CI.
- **Never edit `BlazwizVersion` by hand,** and never point clypse at blazwiz project references.
- **Before work that changed blazwiz is done,** rebuild the packages from the current blazwiz tree and run clypse's full
  test suite on them.

## Versions and releases

- **clypse's version is set in one place:** `VersionPrefix` in `Directory.Build.props`. Every project takes it from
  there, and Settings > About shows it.
- **Every user-visible change goes in the `Unreleased` section of `CHANGELOG.md`,** in the same piece of work, under
  Added, Changed, Fixed, Removed or Security. Write it for someone using clypse, not as a list of files.
- **`eng/bump-version.ps1`** sets the next version: the next minor by default, or `-Version <version>`. It turns
  `Unreleased` into that version's section. If the current version has no GitHub release yet, it offers to add the
  `Unreleased` changes to it instead. The maintainer runs it before a release.
- **The Release workflow** reads `VersionPrefix` and tags `v<version>` on main. Its notes are the blazwiz version and
  the version's `CHANGELOG.md` section. It refuses a version that is already released, is not above the latest release,
  or has no changelog section.
- **Never change `VersionPrefix` by hand, or run the release yourself** (rule 6).
