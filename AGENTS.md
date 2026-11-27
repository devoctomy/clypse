# AGENTS.md

Instructions for AI coding agents working on clypse.

## Rules

1. **Never run git commands in this repository.** Not even read-only ones. Version control here is the
   maintainer's alone: they review, commit and push.
   - The same applies to `gh` and any other tool that talks to GitHub.
   - To delete or move files, use plain file operations.
   - If a step seems to need git, stop and ask.
2. **NEVER touch blazwiz, or any other repository, unless the maintainer has discussed and agreed it beforehand.** This
   holds even for a change the design rules say belongs there.
   - "Touch" means search, read, build or change. Don't look for blazwiz's working tree or any other folder outside
     this repository: work from the blazwiz packages clypse uses (their API and docs). If the source is needed, ask the
     maintainer for permission and the path.
   - Agreement must be explicit. An answer to a different question (e.g. about scope) is not agreement.
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
  - each **secret**, as one encrypted JSON file, `secrets/<secretId>.json`;
  - each **file** (e.g. a scan of a passport), as two encrypted files: its details (name, folder, type, size, notes)
    in `files/meta/<fileId>.json`, and its content in `files/blobs/<fileId>.bin`. An empty folder is a details file
    with no content.

  File keys are GUIDs, like secret keys: nothing about a file is in its key. A file's details are decrypted when the
  vault is unlocked, as the secrets are. Its content is decrypted only when it is viewed or edited, in memory.
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
- **Codacy:** check it after every change, with Codacy's cloud CLI through npx. It reads the results Codacy Cloud
  already holds for `gh/devoctomy/clypse`, so it sees pushed code only; nothing is analysed on this machine.

  ```powershell
  npx -y @codacy/codacy-cloud-cli repository gh devoctomy clypse        # the repository: issues, coverage, open PRs
  npx -y @codacy/codacy-cloud-cli pull-request gh devoctomy clypse <n>  # one pull request's new issues and gate
  ```

  - Tools and patterns are turned on or off with the same CLI and `gh devoctomy clypse`, through `tools` and
    `pattern` with `--enable` / `--disable`. What was turned off, and why, is in
    `docs/reviews/2026-11-13-codacy-noise.md`.
  - Don't install the Codacy Analysis CLI or run `codacy-analysis`, and don't create a `.codacy/` folder here.
- **Deploy tests** (`Clypse.Portal.DeployTests`) run against a deployed environment, from the Deploy tests workflow.
  They test clypse's own UI only; what the blazwiz base stack does is tested by blazwiz. No CI filter runs them.
  - They are Reqnroll scenarios run by MSTest: features in `Features/*.feature`, steps in `Steps/`, and the
    per-scenario browser (`BrowserSession`) and its hooks in `Support/`.
  - Scenario titles are plain sentences, not `Given…_When…_Then…` names, and steps carry no Arrange/Act/Assert
    comments: the unit test naming rules don't apply to them.
  - Write steps in the user's words and reuse them across features, so new workflows are built from existing steps.
  - A scenario can end early and pass with `ScenarioFlow.End` (e.g. "…, or the scenario ends here"). Every step
    therefore returns at once when `ScenarioFlow.HasEnded` is true; new steps must check it first.
  - The scenarios share one test user and their one test vault, and later ones use what earlier ones left. They never
    run in parallel (`[assembly: DoNotParallelize]`, `Support/TestRunOrder.cs`). Keep it that way.
- **Never work around a build failure.**
  - If a build fails for a reason you don't understand, stop and show the maintainer the full error. Don't retry with
    something changed until it passes.
  - Never add a build flag, environment variable, clean-up step (deleting `bin` or `obj`) or build server shutdown to
    get past a failure: not in workflows, scripts, project files or test code, and not in your own shell.

## Running locally against the deployed Test environment

A Debug build of the portal can run on your machine against the deployed Test environment's user pool and bucket, to
check a change with real sign-in and sync before it is deployed.

- **Settings:** `Clypse.Portal/wwwroot/appsettings.Test.json` holds the Test stack's `AwsCognito` and `AwsS3`
  settings, over `appsettings.json`. It is local to the maintainer's machine and git ignored. If it is missing, ask
  the maintainer for it; never run AWS commands to find the values (rule 7).
- **Start the portal** (in the background, from the repository root):

  ```powershell
  dotnet run --project Clypse.Portal --launch-profile test -p:WasmApplicationEnvironmentName=Test
  ```

  - It serves `https://localhost:7090`.
  - The `test` launch profile only sets the host's environment. The WASM client's environment is fixed when it is
    built, so `-p:WasmApplicationEnvironmentName=Test` is needed too. Without it the client loads the Development
    settings. This selects the environment; it is not a way past a build failure.
  - Don't build while it runs. A rebuild leaves it serving stale fingerprinted files (e.g. `dotnet.*.js` 404s): stop
    it, build, and start it again.
  - Stop it when you are done, or when the maintainer says they no longer need it.
- **An AOT build** (when the maintainer asks for one): `dotnet run` never compiles ahead of time; only `dotnet publish`
  of a Release build does (`Clypse.Portal.csproj`). Publish outside the repository, then serve the output with the dev
  server `dotnet run` uses (the `Microsoft.AspNetCore.Components.WebAssembly.DevServer` version in
  `Directory.Packages.props`), in the background:

  ```powershell
  dotnet publish Clypse.Portal -c Release -p:WasmApplicationEnvironmentName=Test -p:ClypseEnvironment=test -o <folder>
  $env:ASPNETCORE_ENVIRONMENT = 'Test'
  dotnet "$env:USERPROFILE\.nuget\packages\microsoft.aspnetcore.components.webassembly.devserver\<version>\tools\blazor-devserver.dll" `
      --applicationpath <folder>\Clypse.Portal.dll --contentroot <folder> --urls https://localhost:7090
  ```

  - Publishing with AOT takes several minutes. `-p:ClypseEnvironment=test` gives it the Test environment's look.
  - A published build has the PWA service worker, which answers every page navigation with the app. Typing a file's
    URL in the browser (e.g. `/appsettings.Test.json`) therefore shows the app's "Nothing here" page even though the
    file is served. Check files with `Invoke-WebRequest`, or in the browser's network requests.
- **Signing in** uses the deploy tests' user, through the Playwright MCP server in `.mcp.json` (Edge, with
  `--secrets playwright-secrets.env`).
  - Only a Claude session started in this repository has that server. A session started elsewhere (e.g. in blazwiz)
    gets a different Playwright server without the secrets: typing the secret names there sends them literally and
    Cognito refuses them. Don't try; say so, and ask the maintainer to sign in or to use a session started here.
  - `playwright-secrets.env` is in the repository root, git ignored (`*.env`). It holds `CLYPSE_TEST_USERNAME` and
    `CLYPSE_TEST_PASSWORD`, which the maintainer sets.
  - Type the secret names into the sign-in form; Playwright fills in the values. Never print, echo or copy the file's
    values.
- **The data is real:** the Test environment's bucket, shared with the deploy tests.
  - Never delete the maintainer's vaults, or the deploy tests' "Deploy tests" vault.
  - Use one passphrase for every vault you make, so it can be cleaned up afterwards.
- **Never give the maintainer a URL to test until it works.** Before handing over a URL, check that the server is
  running, the page loads with no console errors, and the change to be tested works there. While something is still
  building or restarting, say so, and give no URL.
- **Once the portal is handed to the maintainer,** leave it and the Playwright browser alone: don't rebuild, restart
  or drive it while they test. File pickers (e.g. the import dialog's) don't open in the Playwright window; the
  maintainer uses their own browser at `https://localhost:7090` for those.
- **Deploy tests against it:** build first, then point the tests at the local portal, taking the user from
  `playwright-secrets.env` without printing it:

  ```powershell
  $s = @{}
  Get-Content playwright-secrets.env | Where-Object { $_ -match '^\s*([A-Z_]+)\s*=\s*(.*)$' } |
      ForEach-Object { $s[$Matches[1]] = $Matches[2].Trim().Trim('"') }
  $env:CLYPSE_DEPLOYTEST_PORTAL_URL = 'https://localhost:7090'
  $env:CLYPSE_DEPLOYTEST_EMAIL = $s.CLYPSE_TEST_USERNAME
  $env:CLYPSE_DEPLOYTEST_PASSWORD = $s.CLYPSE_TEST_PASSWORD
  dotnet test Clypse.Portal.DeployTests --no-build
  ```

  They use the same user as the deployed runs, so the rules in "Build and test" apply: one run at a time, and a failed
  run of the import scenario leaves its vault behind.

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
- **Get the change working before running any full suite.**
  - While working on a blazwiz change, run only the unit tests of the projects you changed, and check the change in
    the running clypse portal.
  - Hand the portal to the maintainer to try. Run the full suites only after they have confirmed the fix is right.
  - Never run blazwiz's full suites (unit, integration, UI, `test-template.ps1`) or clypse's between attempts. They
    take many minutes and are the final gate, run once.
- **Before work that changed blazwiz is done,** rebuild the packages from the current blazwiz tree. Then run blazwiz's
  full suites and clypse's full test suite on them, once.

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
