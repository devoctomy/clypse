# Clypse

A **Blazor WebAssembly static PWA** that signs in with **AWS Cognito**, built on the
[blazwiz](https://github.com/devoctomy/blazwiz) framework packages (`Devoctomy.Blazwiz.*`).

> **Status:** deployed builds sign in with AWS Cognito. Running locally without Cognito settings uses a
> placeholder login (any username / password). See [Authentication](#authentication).

## Solution structure

| Project | Purpose |
|---|---|
| `Clypse.Portal` | The Blazor WASM PWA: `Program.cs`, your pages and `wwwroot`. Views only. |
| `Clypse.Portal.Application` | Your view models (Blazing.Mvvm) and services. Unit testable. |
| `Clypse.Portal.Models` | Your DTOs and settings. No logic. |
| `Clypse.Portal.Application.UnitTests` | xUnit + Moq. Mirrors the `Portal.Application` folder structure. |
| `Clypse.Portal.UITests` | Playwright (MSTest) end-to-end tests. Starts the portal automatically. |

The framework (the app shell, sign in, settings, theme, sync and encryption) comes from NuGet packages. Your code
plugs into it, so it can be upgraded without touching your code:

| Package | What it gives you |
|---|---|
| `Devoctomy.Blazwiz.UI` | The app shell: layout, sign-in, profile and settings pages, theme, and the extension points below. |
| `Devoctomy.Blazwiz.Auth.Cognito` | Signing in with AWS Cognito, and the session's AWS credentials. |
| `Devoctomy.Blazwiz.UI.Sync` | The sync module: files kept in the browser or a folder, synced with S3, end to end encryption, the Sync page. |
| `Devoctomy.Blazwiz.Testing` | Test helpers: the framework's real text, and checks that your text overrides still match it. |

They bring in the rest (`Core`, `Cryptography`, `Encryption`, `Browser`, `Auth`, `Sync`). Package
versions are managed centrally in `Directory.Packages.props`.

### Upgrading the framework

Change `BlazwizVersion` in `Directory.Packages.props`, then build and run the tests. Every `Devoctomy.Blazwiz`
package has the same version. Minor and patch versions don't break your code; a major version lists what changed and
how to update in the framework's `UPGRADING.md`. Don't edit the framework's files (`_content/Devoctomy.Blazwiz.*`):
change things through the extension points instead, so upgrades replace them cleanly.

## Getting started

Prerequisites: .NET 10 SDK.

```powershell
dotnet run --project Clypse.Portal
```

Open <https://localhost:7090> (or <http://localhost:5090>) and sign in with any username and password.

To sign in against a real Cognito user pool locally instead, copy the deployed stack's ids into the
`AwsCognito` section of `wwwroot/appsettings.Development.json` (the data bucket already allows the local
origins for CORS).

## Running the tests

```powershell
dotnet test Clypse.Portal.Application.UnitTests
dotnet test Clypse.Portal.UITests
```

**UI tests** start the portal themselves on a random free port (its output goes to
`TestResults/portal.log`), and save screenshots to `TestResults/Screenshots`. The first time, install
the Playwright browser:

```powershell
dotnet build Clypse.Portal.UITests
pwsh Clypse.Portal.UITests/bin/Debug/net10.0/playwright.ps1 install chromium
```

To test a portal that is already running (or deployed), set `CLYPSE_UITEST_URL`, e.g.
`$env:CLYPSE_UITEST_URL = "http://localhost:5090"`. Timeouts are deliberately short (10s per step,
30s per test, see `TestBase.cs` and `.runsettings`).

## Building out

`Clypse.Portal/Program.cs` composes the app: `AddBlazwizApp` (its name and ID), `AddBlazwizUi` (the shell),
your own services in `AddAppServices`.

### Add a page

1. **View model**: `Clypse.Portal.Application/ViewModels/ThingsViewModel.cs`, deriving from `ViewModelBase`.
   It is registered automatically.
2. **Page**: `Clypse.Portal/Pages/Things.razor` with `@page "/things"` and
   `@inherits MvvmComponentBase<ThingsViewModel>`. Use `<PageHeader Title="Things" Icon="bi-box" />`
   for a consistent heading and browser tab title.
3. **Navigation**: add a `NavigationItem` with `services.AddNavigationItem(...)` in
   `AppServiceCollectionExtensions.AddAppServices`. Its `Order` sets its place in the sidebar, and it also appears as
   a quick link on the dashboard.
4. **Tests**: `Clypse.Portal.Application.UnitTests/ViewModels/ThingsViewModelTests.cs`.

The framework's own parts plug into the app the same way, so you can add to them or change them in `Program.cs`:
`AddShellComponent` (banners, top bar buttons, floating items, overlays), `AddSettingsSection`, `AddSignInOption`,
`AddSessionLifecycleHandler`, `AddPageAssembly`, and `ReplacePage` or `DisablePage` for the framework's pages
(`BlazwizPages`). Text comes from resources you can reword or translate with
`AddBlazwizText(text => text.AddOverrides<YourText>())`.

### End to end encryption

Every sync profile is encrypted end to end with its own keys and its own passphrase, so files on the device and in S3
can only be read once the user has entered it. A profile's id (`UseKeyId`, a GUID) names its keys, kept in the user's
own Secrets Manager secrets, or in the browser when no user pool is configured (local development). Until its
passphrase is set or entered the profile is locked: a banner points to Settings to set a first passphrase, and a
page wrapping the profile's content in `E2eUnlockPanel` asks for it there. There is no recovery: a forgotten
passphrase loses the profile's files. Syncing without encryption is not supported.

### Add a service

Put your services in `Clypse.Portal.Application/Services` (interface in `Services/Interfaces`) and register them in
`AppServiceCollectionExtensions.AddAppServices`.
Add a matching test file in `Clypse.Portal.Application.UnitTests`.

### Languages

The framework's text is English. To offer more languages, list them in `wwwroot/appsettings.json` (the first is the
default) and add their text as resources:

```json
"Text": { "SupportedCultures": [ "en-GB", "fr-FR" ] }
```

Settings then shows a Language choice, kept on the device; until the user picks one, the browser's language is used
when the app has it. Translate the framework's text with a resource of your own, e.g. `Text/AppText.fr.resx` with the
framework's keys (`Settings.Language.Title`, ...), added with `AddBlazwizText(text => text.AddOverrides<AppText>())`.
Dates and numbers follow the language without any work.

### Passwords

Signed-in users change their password on the Profile page; the sign-in page links to resetting a forgotten one
(`/forgot-password`, sent by email by Cognito). Replace or turn off that page like any framework page, e.g.
`builder.Services.DisablePage(BlazwizPages.ForgotPassword)`, which also hides the link.

### Theming

The app uses Bootstrap 5.3 colour modes (`data-bs-theme="light|dark"` on `<html>`). The framework's colours are CSS
variables (`--bw-*`); override them in `wwwroot/css/app.css` to re-brand, e.g. `--bw-accent` and `--bw-accent-2`.
The saved theme is applied by a small script in `index.html` before Blazor starts, so there is no flash of the wrong
theme.

### Configuration

`wwwroot/appsettings.json` holds `AppSettings` (title, description, copyright), `AwsCognito`
(region, user pool, client and identity pool ids) and `AwsS3` (region, data bucket).
`appsettings.Development.json` overrides values when running locally and is not published; it lightens the Argon2id
settings (`Argon2id`), so passphrases are quick to enter while developing and in the UI tests. When
deploying, `deploy.ps1` generates `appsettings.Production.json` with the stack's ids (git ignored).

Sync profiles (what is synced, where it is kept on the device, and the id of its keys) are classes deriving
from `SyncProfileDefinition`, found in the app's assemblies by `AddProfilesFromAssemblyContaining` in `Program.cs`.
Clypse defines none: every vault is a protected profile the user creates at runtime
(`ISyncProtectedProfileService`), so a new user starts with no profiles at all. The other sync settings (when to sync after a change, how long the sync log
is kept) are in `Clypse.Portal/sync.json`, embedded in the app and read by `UseEmbeddedConfiguration`. See the
`Devoctomy.Blazwiz.Sync` README for both.

## Authentication

`AddBlazwizCognito` signs in with Cognito when `AwsCognito` has a region, user pool id and app client id (deployed
builds get these from `appsettings.Production.json`), using SRP through AWS's
[`Amazon.Extensions.CognitoAuthentication`](https://github.com/aws/aws-sdk-net-extensions-cognito), so the password
is never sent. Otherwise it keeps the placeholder sign-in (any credentials), e.g. for local development and the UI
tests.

## Deploying to AWS

The scripts are in `cloudformation/`; see [`cloudformation/README.md`](cloudformation/README.md). The base stack
(CloudFront, S3, Cognito) comes with the framework (`Devoctomy.Blazwiz.Aws`), so upgrading the framework upgrades it;
add the app's own AWS resources to `cloudformation/app.yaml`. You need the AWS CLI v2, the .NET 10 SDK and credentials
for the target account.

```powershell
# Deploy (asks for confirmation, then for the test user's password)
./cloudformation/deploy.ps1 -Region eu-west-2 -TestUserEmail me@example.com

# Check it all works
./cloudformation/test-auth.ps1 -Region eu-west-2 -Email me@example.com

# Re-publish just the portal after code changes
./cloudformation/deploy.ps1 -Region eu-west-2 -SkipInfrastructure

# Tear it down
./cloudformation/remove.ps1 -Region eu-west-2
```

All scripts take `-AppName` (default `clypse`), `-Environment` (default `dev`), `-Region` and `-AwsProfile`. The stack
is named `<AppName>-<Environment>`, so several environments can share an account.
