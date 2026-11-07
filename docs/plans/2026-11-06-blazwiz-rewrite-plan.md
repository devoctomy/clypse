# Plan: rewrite clypse on blazwiz

**Date:** 2026-11-06
**Status:** Phase 0 mostly done (deploy and spikes outstanding); Phases 1 and 2 done (Phase 2's two-browser check
needs a deployed stack); Phase 3 mostly done (settings sections and vault rename outstanding). See
[Progress](#progress).
**Source app:** `E:\Source\GitHub\clypse` (v1.2.0.0)
**Framework:** `E:\Source\Misc\blazwiz` (0.4.0, `Devoctomy.Blazwiz.*` on NuGet)
**New app:** `E:\clypse`

## Goal

Rebuild clypse as a new blazwiz app. Each clypse **vault** becomes a blazwiz **protected sync profile**: it has its own
passphrase and its own keys, is encrypted end to end, and syncs offline-first to S3. Each **secret** in a vault becomes
one encrypted file in that profile.

**Scope of reuse:** only the reusable C# in `clypse.core` and its unit tests come over. The domain code is the secret
model, password generation, the strength estimator and CSV import. Nothing of the existing UI comes over: no views,
view models, CSS, JavaScript or portal services. The UI is designed fresh on blazwiz.

blazwiz provides authentication, key management, encryption, storage, sync, the shell, themes and the AWS
infrastructure.

The template's notebooks feature is the model to follow, almost one to one:

| Notebooks (template) | clypse (new) |
|---|---|
| Notebook = protected profile (`NotebookService.CreateAsync(title, passphrase)`) | Vault = protected profile (`VaultService.CreateAsync(name, description, passphrase)`) |
| `profiles/<id>/.notebook`: encrypted `{Title}` | `profiles/<id>/.vault`: encrypted `{Name, Description}` |
| `SetLabelAsync` shows a locked notebook by name | The same for vaults |
| `NotebookLabelHandler` re-labels on unlock | `VaultLabelHandler` |
| Note = a markdown file in the profile's `ISyncFileStore` | Secret = a JSON file, `profiles/<id>/secrets/<secretId>.json` |
| `Files.razor` / `NoteEditor.razor` inside `<E2eUnlockPanel>` | The vault and secret pages inside `<E2eUnlockPanel>` |
| `NoteService` / `NoteServiceFactory` | `SecretService` / `SecretServiceFactory` |

## Progress

### Phase 0: scaffold and spikes

**Done:**
- **The app is generated** from the blazwiz 0.4.0 template: `--notes false --showcase false`, with `BlazwizVersion`
  pinned to 0.4.0. The template's `FilesSyncProfile` is removed (D8).
- **Template UI tests** that assumed a code-defined profile are replaced by
  `GivenNoProfiles_WhenSignedIn_ThenNothingAsksForAPassphraseAndNoneAreListed`.
- **Art.** `Resources/Art` is copied over. The template's favicon, Apple touch icon and PWA icons are replaced with the
  clypse PNGs, and `favicon.svg` is removed.
- **D8 checked.** Two framework gaps were found and fixed in blazwiz (see D8).

**Not done:**
- **Deploying the stacks.** This needs the maintainer (AGENTS.md rule 6).
- **The phone half of the spikes.** The desktop results are under "Spike results" below.

### Phase 1: `Clypse.Core`

**Done:**
- **The port.** 43 files, and 120 unit tests passing.
- **References.** No references to AWS, crypto, compression or JSInterop.
- **Fixes and changes:**
  - the `AwsCredentials` `SecretType` bug;
  - `Resorce` corrected to `Resource`;
  - the loader now uses `GZipStream` directly;
  - the list caches are per instance, with the loader registered as a singleton;
  - `AddClypseCoreServices` is now `AddClypseCore(configureTextLists)`, which also registers `ISsoProvidersService`.
- **Release-only full text list** (see the note under clypse.core).
- **Wiring.** `Clypse.Portal.Application` references `Clypse.Core`, and `Program.cs` calls `AddClypseCore`.

### Phase 2: vaults and secrets

**Done** (services and unit tests; the UI comes in Phase 3):
- **Models.** `Vault` (Id, Root, Name, Description, IsUnlocked) and `VaultDetails`, the content of the `.vault` file, in
  `Clypse.Portal.Models/Vaults`.
- **`VaultService`.** List, find, create (with Argon2id progress), update the name and description, delete and lock.
  - Every user-created protected profile is a vault.
  - A locked vault shows its device label, or its id.
  - The name is re-labelled whenever it is read while unlocked.
- **`SecretService`** and its factory:
  - Each secret is `profiles/{id}/secrets/{secretId}.json`, holding `Secret.Data` exactly as clypse stores it (D4).
  - Ids must be GUIDs, so a name can never reach a path.
  - Saving validates with `ClypseObjectValidator` and sets `LastUpdatedAt` from `TimeProvider`.
  - Import validates every secret before writing any.
  - Reading casts to the secret's own type.
- **`VaultSession`.** The open vault's secrets in memory (D3). It:
  - reads them one by one after unlock, notifying every 25;
  - counts unreadable files;
  - clears them on lock;
  - applies `FilesChanged` from sync;
  - discards a stale load when another vault is opened.
- **`VaultLabelHandler`.** Labels a vault whenever it is unlocked.
- **Errors.** `ClypseException`, a `BlazwizException` with codes from `ClypseErrorCodes`, with text in
  `Text/ClypseText.resx` registered through `AddErrorText<ClypseText>()`.
- **Wiring.** `AddClypse()` registers it all, and `Program.cs` calls it.
- **Fixes along the way:**
  - `ClypseObject` formatted its dates with the current culture; it now uses the invariant culture.
  - `ImportAsync` returned 0 when no progress reporter was passed: the count was incremented inside `progress?.Report(...)`.
    Fixed, with a regression test.
- **Tests.** 77 new unit tests, 97 in the application project in all.

**Not done:**
- **Two-browser sync and the S3 listing check.** They need a deployed stack.
- ~~Creating a vault and filling it from the browser.~~ Done: covered by the Phase 3 pages and their UI tests.

### Phase 3: new UI

**Done:**
- **Theme.** The accent is the exact orange of the PNG artwork, `#ff6600` (sampled from `icon-colour-1024.png`), in light
  and dark modes, through the `--bw-*` tokens in `wwwroot/css/app.css`. The range slider thumb is restyled to match.
- **Removed from the template.** The template's dashboard, greeting service and their tests are gone.
- **Vaults page,** the home page (`/` and `/vaults`, `VaultsViewModel`):
  - an empty state that offers to create the first vault;
  - vault cards, locked or unlocked, shown by device label or id;
  - a lock button on unlocked cards;
  - the new-vault dialog: name, description, passphrase twice, and the no-recovery acknowledgement.
- **Vault page** (`/vaults/{id}`, `VaultViewModel`), inside `E2eUnlockPanel`:
  - decrypting progress and an unreadable-secrets warning;
  - search across name, description, tags, user name, email and website;
  - tag filter chips, and the secret list with a type icon and summary line;
  - lock, import and new secret;
  - leaving the page closes the vault session.
- **Secret editor** (`SecretEditorDialog`, `SecretEditorViewModel`):
  - choosing the type for a new secret (login, AWS credentials, other);
  - fields from `GetOrderedSecretFields`, with required markers;
  - password fields with show/hide, the generator and a strength meter;
  - tags as comma-separated text;
  - delete with confirmation.

  It edits a copy, so nothing changes until saved. Passwords and multi-line text are stored exactly as typed.
- **Password generator** (`PasswordGeneratorDialog`, `PasswordGeneratorViewModel`):
  - random passwords (length 8–64, character groups, at least one of each);
  - memorable passwords from the templates in the `Clypse` section of `appsettings.json` (`ClypseSettings`).
- **Strength.** `PasswordStrengthService` uses the complexity estimator *without* the weak-password list, so typing stays
  fast. The list check was added in Phase 4.
- **CSV import** (`ImportSecretsDialog`, `ImportSecretsViewModel`):
  - KeePass and Cachy formats;
  - a file of the wrong format, or one with rows missing a name, is refused when read, not at import.
- **Text.** All text is in `ClypseText.resx` (generated from one table), and the Vaults navigation item uses
  `TextResource`.
- **Pseudo-localisation.** User data in the pages is marked `data-user-content`, so the pseudo-localisation UI test now
  covers clypse's own pages and dialogs too.
- **Checked by hand in the browser.** Create, add with generator, save, lock, unlock, and reload; the vault shows by name
  while locked. IndexedDB holds only `E2EE` ciphertext and GUID keys. Dark mode and phone width look right.
- **Fix in `Clypse.Core`.** `ClypseObject.SetData` stored an empty string for a new key, so an empty name passed
  validation. It now removes the key for any empty value, as its doc comment says.
- **Tests:**
  - unit tests for every new view model and the strength service: 169 application unit tests, 125 core;
  - new `VaultTests` UI tests: create, list, search and tag filter, edit and delete, validation, generator (random and
    memorable), lock and unlock, reload, KeePass import and a missing vault.

**Finished afterwards (the leftovers, before Phase 4):**
- **Copying:**
  - copy buttons on every single-line field in the editor;
  - quick copy on each secret row (a login's user name and password, an AWS key id and secret key);
  - `SecretCopyService` uses blazwiz's new clipboard service and toasts;
  - a secret value (a password, or a field marked `IsSensitive` in `SecretFieldAttribute` — the AWS secret access key)
    is cleared from the clipboard after `Clypse:ClipboardClearSeconds`, 30 by default, and masked in the editor until
    shown.
- **Vault settings** (a dialog on the vault page):
  - rename and describe (`VaultService.UpdateAsync`);
  - auto-lock after 1, 5, 15, 30 or 60 minutes or never, kept with the vault on every device through blazwiz's new
    per-profile idle timeout;
  - delete once the vault's name is typed exactly.

  clypse's vault defaults are set with `UseProfileDefaults`: auto-lock after 5 minutes, and passkey unlock allowed (each
  vault still opts in on each device, in Settings).
- **Password generator settings** (a Settings section, kept on the device by `PasswordGeneratorPreferencesService`):
  - what the generator starts with (mode, length, kinds of character, template);
  - the user's own memorable templates, each tried before it is added.
- **The framework's profile list in Settings is kept.** It is the place to change a vault's passphrase, turn on passkey
  unlock and delete a locked vault. Its wording is overridden to say "vaults" and "secrets" (`ClypseSyncText.resx`,
  checked against the framework by `ClypseSyncTextTests`).
- **The logo** is `images/logo.png` (the clypse PNG) through blazwiz's new `AppSettings.LogoUrl`. The SVG wrapper is
  gone.
- **Tests:** 214 application unit tests, 126 core, and 56 UI tests (new ones for copying, vault settings, deleting, the
  generator settings and the logo). All pass on the local blazwiz build.

### Phase 4: framework additions and hardening

**Done so far:**
- Items 1–3 were done with the Phase 3 leftovers: clipboard and toasts, a 5-minute auto-lock default, and passkey
  unlock allowed.
- **The weak known passwords list in the strength meter** (left over from Phase 3):
  - The editor shows the quick estimate as you type. Once typing pauses for 500 ms
    (`SecretEditorViewModel.KnownPasswordCheckDelay`, timed with `TimeProvider`), it checks the password against the
    list (`IPasswordStrengthService.EstimateCheckingKnownPasswordsAsync`).
  - A listed password shows as "Very weak: it is a commonly used password", however long it is.
  - The generator doesn't check its own passwords against the list.
  - **Changed decision:** `Clypse.Core` keeps the list as a sorted array of 64-bit XxHash3 hashes of each line
    (`HashedTextSet`, `LoadCompressedHashedTextSetAsync`, package `System.IO.Hashing`), not as a `HashSet<string>`.
    It is about 42 MB in memory. The string set was a few hundred MB by estimate, not measured. The original clypse
    loaded the string set in WebAssembly without trouble, so this saves memory; it doesn't fix a failure. The list
    loads once, however many ask at once.
  - **Which list.** Debug builds use the stand-in list. Release builds use the full list unless
    `TextLists:UseFullWeakKnownPasswordsList` is false. `appsettings.Development.json` sets it to false, so the UI
    tests (interpreted Release) use the stand-in list too.
- **CSP (item 4) already exists.** blazwiz's deploy adds it (`Add-ContentSecurityPolicy` in the base stack's
  `_common.ps1`):
  - scripts: self, `wasm-unsafe-eval` and the inline scripts by hash;
  - connections: only this region's Cognito, Secrets Manager and data bucket endpoints;
  - framing refused by CloudFront's security headers.

  Checked on 2026-11-07: the same policy, applied to a local AOT publish, ran the 1,000-secret flow, quick copy, the
  generator and Settings with no violations and no console errors.
- **Fix:** the browser theme colour (`theme-color` in `index.html`, `theme_color` in the manifest) was still the
  template's purple. It is now #ff6600.
- **Item 5, the open blazwiz findings** (decided with the maintainer on 2026-11-07):
  - **MP1 fixed in blazwiz.** The sync status now covers every vault's last sync, not only the last one run (blazwiz
    change 11).
  - **MP4 fixed in blazwiz,** although clypse's vaults can't clash. Clashing profiles are refused (blazwiz change 12).
  - **Concurrent writes of the index secret:** left in blazwiz's backlog
    (`docs/backlog/protected-profile-concurrent-writes.md`). It now also covers a vault's auto-lock change racing
    another device's index write. Nothing is done about it for now.
  - **Secrets Manager deletes skip the recovery window:** moved to its own plan, with secret history (S3 versioning),
    in `docs/plans/2026-11-07-versioning-and-recovery-plan.md`. A recovery window on the keys alone would recover
    nothing, because the files are deleted too.
- **The security checklist** ("done when") is in `docs/reviews/2026-11-07-security-checklist.md`. Locally it passes:
  - no secret content in browser storage;
  - only wrapped keys stored;
  - clipboard clearing, auto-lock and the CSP all work.

  **Fixed after the first run:** vault names were in local storage in plain text, as blazwiz's device labels. They
  should have been encrypted from the start. Now they are kept only encrypted with a key derived from the passkey
  (blazwiz change 13). Locked vaults show by id when there is no passkey sign in.

  The S3, Secrets Manager and IAM checks need the deployed stack.

**Phase 4 is not finished** until the deployed-stack checks are done.

### Changes made in blazwiz

All on branch `clypseupdates`, with unit tests and CHANGELOG entries:

1. **`SyncScheduler` checks connectivity when there are no sync profiles.** Before, the status said "Checking"
   forever.
2. **`SyncSettingsViewModel` reloads the storage choice when a profile is added or removed.** Before, the choice stayed
   hidden after the first protected profile was created. The constructor gains an `ISyncProfileRegistry` (recorded in
   `PublicAPI.Unshipped.txt`).
3. **The template's `MyApp.slnx` lists the files actually in `template/cloudformation`.** Before, it listed the base
   stack's files, which generated apps don't have.

**No blazwiz changes are released until this rewrite is finished.** clypse builds against blazwiz packed locally from
source: `eng/use-local-blazwiz.ps1` packs every package with a unique `-local.<timestamp>` version into
`.blazwiz/packages` (git ignored), sets `BlazwizVersion`, and `nuget.config` adds the folder as a source (see
AGENTS.md). The 5 UI tests that were `[Ignore]`d waiting for fixes 1 and 2 run again and pass.

4. **Clipboard** (`IClipboardService`, Browser): copy, and clear again later unless something else was copied since;
   waits for focus if it has to.
5. **Toasts** (`IToastService`, `ToastHost` in the shell's overlays).
6. **`AppSettings.LogoUrl`**, defaulting to `images/icon.svg`.
7. **A storage for the device:** `ISyncStorageResolver.Get/SetDeviceDefaultKindAsync`. Profiles without a storage of
   their own use it. Settings offers the choice with no profiles yet, and a move also records it. This replaces the old
   "no default storage before the first profile exists" gap.
8. **A protected profile's own idle timeout**, kept in the profile index (`idleTimeoutSeconds`) and applied on every
   device: `ISyncProtectedProfileService.SetIdleTimeoutAsync`, `IE2eIdleLock.Refresh`.
9. **CSS fixes:**
   - a dialog over another dims the one beneath;
   - the theme preview and range sliders use the app's accent.
10. **Text lookups are cached** in `OverridingStringLocalizer`, per culture and name (see "Spike results").
11. **MP1: the sync status covers every profile.**
    - `SyncService` keeps each profile's last pass.
    - The status is Error if any profile failed. "Last synced" is when the least recently synced profile last synced.
      Conflicts are added up.
    - A profile held back (a locked vault) or removed no longer counts.
12. **MP4: clashing profiles are refused.** `SyncBuilder.AddProfile` and `SyncProfileRegistry.Add` refuse two
    profiles that:
    - have overlapping remote prefixes;
    - or share an IndexedDB store or OPFS root.

    A profile set up in code may not cover `profiles/`, where the profiles users create are kept.
13. **Labels are encrypted on the device.**
    - Protected profiles' labels (a vault's name) are kept only encrypted (AES-GCM) with `E2eKeyNames.LabelKey`.
      That key is derived from the passkey's PRF output at sign in.
    - Without the key (password sign in, or after a reload), labels are not read or kept, so profiles show by id.

**Last full run, 2026-11-07, after change 13:**
- **Pass:** blazwiz's unit, integration (`@aws` skipped), UI (57) and template pack tests.
- **Upgrade test from the latest release** (`test-template.ps1 -UpgradeFrom latest`): **one failure, expected.**
  - The app created from 0.4.0 carries the old notebook reload test, which expects a name after a password sign in.
    That is the behaviour change 13 removes.
  - The changelog tells apps to update that test.
  - The same run after changes 11 and 12 passed in full.
- The template scripts ran under Windows PowerShell 5.1, because `pwsh` is not installed here.

### Framework gaps found, not fixed

None outstanding from Phases 0–3.

### Spike results

Desktop only (Chromium through Playwright, on the development machine). Same flow each time:
- sign in with the placeholder sign-in;
- create a vault;
- import 1,000 logins from a CSV;
- lock, then unlock and wait until all 1,000 are read;
- search for one.

Both runs use the release Argon2id costs (512 MiB, 2 iterations, the framework's defaults).

| Step | Interpreted (`dotnet run -c Release`) | AOT (`dotnet publish -c Release`, served statically) |
|------|------|------|
| Create a vault (Argon2id plus the first write) | 2.4 s | 2.0 s |
| Import 1,000 secrets | 4.1 s | 1.9 s |
| Unlock (until the page shows) | 2.0 s | 1.7 s |
| Unlock until all 1,000 are read | 6.0 s | 2.3 s |
| Search 1,000 | 27 ms | 7 ms |

No errors in the console in either run.

**What made the difference.** Before the fixes below, an interpreted build had read only 650 of 1,000 after 5
minutes. The cost was not decryption or storage: it was rendering. Every row looked its text up through the localizer,
and every refresh re-sorted the list with culture-aware comparisons. The fixes:
- **blazwiz:** `OverridingStringLocalizer` keeps each lookup per culture and name (see "Changes made in blazwiz").
- **clypse:**
  - the vault page virtualizes the list (`<Virtualize>`);
  - `VaultViewModel` keeps the filtered list and tags until the secrets or filter change, and keeps each secret's copy
    labels;
  - comparisons are ordinal and ignore case;
  - `VaultSession` reads secrets in parallel batches of 100 and tells the page once per batch.

**AOT.** `Clypse.Portal.csproj` turns on `RunAOTCompilation` and `WasmEnableSIMD` for Release builds, as the original
clypse did. It needs the `wasm-tools` workload.
- The SDK only AOT compiles on publish, so it applies to the plain `dotnet publish -c Release` that deploy runs. Its
  native wasm is 37 MB, against 2.9 MB without AOT; it is compressed when served.
- A Release `dotnet run` (and so the UI tests) stays interpreted, but relinks the native runtime. After changing
  these settings, build clean: stale output failed at runtime with "function signature mismatch".
- The original also set `InvariantGlobalization`. clypse does not: blazwiz's language choice and plural forms need
  ICU.

**Still to do:** the same flow on a phone, once the stacks are deployed.

## What changes conceptually

| Concern | clypse today | clypse on blazwiz |
|---|---|---|
| Sign in | Cognito through `amazon-cognito-identity-js` | `Devoctomy.Blazwiz.Auth.Cognito` (SRP in .NET), the framework's `LoginPage` |
| Remember me / passkey | SimpleWebAuthn PRF; the Cognito password is encrypted with the raw PRF output | The framework's PRF `rememberme` key (HKDF of the PRF output) and saved users |
| Vault key | Passphrase → Argon2id → key used directly; KDF parameters in a plaintext `manifest.json` | Passphrase → Argon2id, which wraps a master key, which wraps the file keys. Kept in a keys document in Secrets Manager (`<sub>/keys/<scope>`); costs are set per vault and stored in the document |
| Unlock without the passphrase | Not possible | Optional per vault and per device (`AllowDeviceUnlock`, PRF `e2e-kek`) |
| Locking | Manual; the key is held until sign out | Per vault, lock all, idle timeout per vault, lock in every tab |
| Storage | Online only: S3 through the JS AWS SDK | Offline first: IndexedDB, OPFS or a folder, synced to S3 by `SyncEngine` |
| File format | `nonce‖ct‖tag`, GZip then encrypt, no AAD | `EncryptedFileFormat` v1, bound to scope and path |
| Vault listing | S3 listing plus `clypse_vaults_{user}` in localStorage | `ISyncProtectedProfileService.Profiles` (the index secret) plus device labels |
| Index of secrets | Encrypted `index.json`, rewritten on every save | Built in memory after unlock (D3) |
| Offline | No | Offline sign-in and offline unlock from the cached keys document |
| Text | Inline English | `.resx` throughout, enforced by the pseudo-localisation test |
| Infrastructure | `clypse.portal.setup` or `Cloudformation/clypse-aws-setup.yaml` | The blazwiz base stack plus the app's `cloudformation/app.yaml` |

## Decisions

- **D1. One protected profile per vault.**
  Not one per secret: each profile costs an Argon2id derivation and a Secrets Manager secret.
- **D2. One file per secret, named by the secret's GUID.**
  A conflict then only affects one secret. GUID names are needed because paths are not encrypted (blazwiz D4), so a
  name such as `gmail.json` would be visible in S3 and IndexedDB.
- **D3. No stored index.**
  - After a vault is unlocked, a background task reads, decrypts and deserialises every `secrets/*.json` into `Secret`
    instances.
  - The in-memory list fills in as each secret is ready. The UI shows the list growing and a progress count; search
    works over whatever has loaded so far.
  - The task is cancelled if the vault locks.
  - Later, consider caching the index on the device, encrypted with a PRF-derived key. This is out of scope for now.
- **D4. Secret files use clypse's JSON format:** the `Secret.Data` dictionary, `ClypseObject`'s camelCase enums and its
  date format. Clypse is the standard. No GZip.
- **D5. No migration.** clypse is unfinished and has no users. Nothing of the legacy read path (vault manager, KDF,
  crypto, S3 providers) is kept.
- **D6. Conflicts: newest wins for now,** which is the framework's default. A better conflict resolution system will be
  designed in blazwiz later, so clypse gets it automatically. clypse does not add its own resolver.
- **D7. Gaps go to the framework where they are generic.**
  - Clipboard with auto-clear: into `Devoctomy.Blazwiz.Browser`.
  - Toasts: into `Devoctomy.Blazwiz.UI`.
  - The password generator stays in clypse as domain code.
  - MFA waits for the framework backlog item (`docs/backlog/mfa-totp.md`).
- **D8. No default profiles.** A user starts with no vaults and creates their own. Generate the app with
  `--notes false` and remove the template's `FilesSyncProfile`.
  **Checked in Phase 0.** Protected profiles can be created, listed, reloaded and deleted with zero code-defined
  profiles. Two framework gaps showed up:
  - **Connectivity was never checked**, because the sync scheduler only checked it inside a profile's pass. The sync
    pill said "Checking" forever and the Sync page logged no connectivity events. **Fixed in blazwiz** (branch
    `clypseupdates`, uncommitted): `SyncScheduler` now checks connectivity when there are no profiles. Unit tests and a
    CHANGELOG entry are added. clypse's three connectivity UI tests are `[Ignore]`d until a blazwiz release has the fix.
  - **The Settings storage choice only appears once a profile exists.** The choice is per profile (MP2), so a new user
    cannot pick IndexedDB, OPFS or a folder until they create their first vault. Not fixed. It needs a framework
    decision: a device-wide default storage for profiles created later. The storage UI tests now create a protected
    profile first.

## What is brought over

Only `clypse.core` and `clypse.core.UnitTests`.

### clypse.core (6,691 lines) → `Clypse.Core`

| Area | Files | Action | Notes |
|---|---|---|---|
| Base | `Base/ClypseObject`, `ClypseObjectValidator`, `RequiredDataAttribute`, `ClypseObjectValidatorException` | Keep | The secret file format (D4) depends on it |
| Secrets | `Secrets/Secret`, `WebSecret`, `AwsCredentials`, `SecretFieldAttribute` | Keep | Fix the bug where the `AwsCredentials` constructor sets `SecretType.Web` |
| Enums | `SecretType`, `SecretFieldType`, `CharacterGroup`, `CsvImportDataFormat`, `DictionaryType`, `PasswordComplexityEstimation` | Keep | |
| Extensions | `SecretExtensions` (`GetOrderedSecretFields`, `CastSecretToCorrectType`), `StringExtensions`, `MatchExtensions`, `CharacterGroupExtensions` | Keep | `GetOrderedSecretFields` will drive the new secret editor |
| Extensions | `ServiceCollectionExtensions.AddClypseCoreServices` | Adapt | Register only the services that are kept |
| Password | `StandardWesternPasswordGeneratorService`, `DictionaryTokenProcessor`, `RandomStringTokenProcessor`, `CharacterGroups`, `StandardWesternPasswordComplexityEstimatorService` | Keep | Remove the stray `using Amazon.S3.Model` in `RandomStringTokenProcessor` |
| Data | `EmbeddedResorceLoaderService`, `SsoProvidersService`, `ResourceKeys`, `Data/Lists/*` (word lists, `ssoproviders.txt`, `weakknownpasswords.txt.gz`) | Keep | Correct the "Resorce" spelling while moving it |
| Cryptography | `RandomGeneratorService` | Keep | Needed by the password generator. It wraps `RandomNumberGenerator`, so it does not count as hand-rolled crypto under the blazwiz rules |
| Import | `CsvSecretsImporterService`, `FieldMappings` (KeePass and Cachy CSV), import exceptions | Keep | |
| Vault | `Vault`, `VaultInfo`, `VaultIndex(Entry)`, `VaultManifest`, `VaultManager`, bootstrapper, results, exceptions | Drop | Replaced by a protected profile plus `SecretService`. One idea carries forward: all-or-nothing `AddRawSecrets` becomes "import validates everything before writing anything" |
| Cryptography | AES-GCM/CBC crypto services, `KeyDerivationService` (+ options, benchmarks), `CryptoHelpers`, `KeyDerivationAlgorithm` | Drop | `Devoctomy.Blazwiz.Cryptography` and `.Encryption` replace them |
| Compression | `GZipCompressionService` | Drop | |
| Cloud | all of `Cloud/` (storage providers, `JavaScriptS3Client`, `AmazonS3ClientWrapper`, …) | Drop | `S3SyncRemoteStore` replaces them |
| Json, Blazor | `JElementToPrimativesConverter`, `JavaScriptInteropUtility` | Drop | Only the manifest and JS S3 path needed them |

> **Note: load the full text lists only in Release WASM builds.**
>
> Loading the large embedded lists is far too slow in a Debug WASM build, so the full lists must load only in Release
> builds. The old clypse did this for the weak-known-passwords list, in
> `StandardWesternPasswordComplexityEstimatorService.IsWeakKnownPasswordAsync`. Under `#if DEBUG` it used a one-entry
> stand-in (`"password123"`); otherwise it loaded `weakknownpasswords.txt.gz` through `LoadCompressedHashSetAsync`.
>
> **Done in Phase 1:**
> - **Only the weak-known-passwords list needs it.** That list is 21 MB compressed, with 5.19 million lines. The
>   dictionary word lists are 8–10 KB each, so they always load in full.
> - **The choice is made through DI, not `#if DEBUG` in the library.** `TextListOptions.UseFullWeakKnownPasswordsList`
>   (default on) is read by a new `IWeakKnownPasswordsService`, which the complexity estimator now uses. The portal's
>   `Program.cs` turns the option off in Debug builds; that is the only `#if DEBUG`. Unit tests cover both paths.
> - **Caching.** The old loader already cached lists, in a static dictionary. The cache is now per instance (thread-safe),
>   and the loader is registered as a singleton.

**Result:** about 2,500 of the 6,691 lines carry over. `Clypse.Core` then has no references to AWS, JSInterop,
Konscious or BouncyCastle. Check whether `TextFieldParser` (Microsoft.VisualBasic) is still the right CSV parser.

### clypse.core.UnitTests (7,230 lines) → `Clypse.Core.UnitTests`

| Files | Action |
|---|---|
| Tests for Base, Secrets, Extensions, Password, Data, Import and `RandomGeneratorService` | Keep. They already follow the blazwiz convention (`Given_When_Then` names, Arrange/Act/Assert comments), so no renaming was needed |
| The fixtures: `Data/Lists/fruit.txt(.gz)` and the CSV samples | Keep |
| Tests for Cloud, Cryptography, Vault, Compression and Json | Drop |
| `clypse.core.IntTests` (Reqnroll, real S3) | Drop; keep `Data/secrets.csv` as an import fixture |

### Not brought over

Every other project is replaced by blazwiz or written new.

| Project | Replaced by |
|---|---|
| `clypse.portal` (views, layouts, CSS, wwwroot) | New UI on the blazwiz shell |
| `clypse.portal.Application` (view models, services) | New view models; blazwiz auth, storage, navigation, settings and theming |
| `clypse.portal.Models` | New models, plus the framework's `SavedUser`, `AppSettings` and `NavigationItem` |
| `clypse.portal.ClientScripts` (Cognito, S3, WebAuthn, PWA update, viewport JS) | blazwiz's packages, which bring their own JS |
| `clypse.portal.setup`, `Cloudformation/` | The blazwiz base stack and the template's app stack and deploy scripts |
| `clypse.portal.Application.UnitTests`, `clypse.portal.setup.UnitTests`, `clypse.portal.UITests` | New tests, from the template's projects |

`Documentation/portal-views-and-viewmodels.md` is still useful as a list of the features the old UI had, for checking
the new UI against. The README can be kept as a starting point.

### Art (`Resources/Art`) → `Resources/Art`

Bring every file over unchanged:

| Kind | Files |
|---|---|
| PNG icons | `icon-colour-{16,32,44,48,71,72,96,144,150,152,160,180,192,512,1024}.png` |
| PNG logos | `logo-colour-1024x637.png`, `logo-colour-161x100.png`, `logo-white-invert363-72.png`, `logo-white-invert645-128.png` |
| SVG | `icon-colour.svg`, `logo-colour.svg`, `logo-white-invert.svg`, `logo.svg` |
| Source | `logo1.afphoto` |

**Use only the PNG files** in the app: the PWA manifest icons, the favicon, the Apple touch icon, the logo on the login
page and title bar, and the README.

> **Note: the SVGs have the wrong font.**
>
> Each SVG draws its lettering as live `<text>` in `font-family: Montserrat`, weight 900. The font is not embedded,
> so wherever Montserrat Black is not installed the text falls back to another font and the art looks wrong. The PNGs
> were rendered with the right font and are correct.
>
> **To do: restore the proper font in the SVGs.** Options, best first:
> 1. Re-export from `logo1.afphoto` (Affinity Photo) with the text converted to curves.
> 2. Install Montserrat Black (SIL Open Font License, from Google Fonts), open each SVG in Inkscape and use
>    *Path → Object to Path* on the text.
> 3. Embed a subset of Montserrat as a `@font-face` data URI. This is the least preferred option, because some renderers
>    ignore fonts embedded in SVG.
>
> Check the result against the PNGs. The SVGs can be used in the app only once the font is fixed.

Also kept: `Resources/Screenshots`, until they are replaced by screenshots of the new UI.

## Target solution layout

Generate the app with `dotnet new blazwiz -n Clypse --title "Clypse" --notes false --showcase false`. Generate a
second copy with `--notes true` in a scratch folder to use as the reference implementation.

```
E:\clypse
  Clypse.Core/                         ported domain code
  Clypse.Core.UnitTests/               ported tests
  Clypse.Portal/                       WASM host: Program.cs, Pages/, wwwroot/
  Clypse.Portal.Application/           VaultService, SecretService, view models, Text/*.resx
  Clypse.Portal.Models/
  Clypse.Portal.Application.UnitTests/
  Clypse.Portal.UITests/
  cloudformation/
  docs/
```

`Program.cs` follows the template's order: `AddBlazwizApp` → `AddBlazwizUi` → `AddBlazwizCognito` →
`AddBlazwizSyncModule(...)` → `AddClypseCore()` → `AddClypse()` → `AddMvvm(...)`. On top of that it registers:

- `AddNavigationItem` for "Vaults";
- `AddSettingsSection<PasswordGeneratorSettings>()`;
- `AddSessionLifecycleHandler<VaultLabelHandler>()`.

## Phases

### Phase 0: scaffold and spikes

1. Generate the app with the layout above and pin `BlazwizVersion` exactly. Copy `Resources/Art` across and replace the
   template's icons and logo with the clypse PNGs. Do not use the SVGs (see the art note). Deploy the base and app stacks to a test
   account and sign in.
2. **Confirm D8:** with zero code-defined profiles, create, unlock, lock and delete a protected profile in the
   generated app. Check the Sync page, the sync pill and the Settings storage choice. Fix anything that fails in
   blazwiz.
3. **Spike D3:** time reading and decrypting 1,000 small files from one profile, from IndexedDB and from OPFS, on
   desktop and on a phone.
4. **Spike Argon2id costs:** pick per-vault defaults that are strong but don't freeze a phone for long. Argon2id runs on
   the WASM UI thread.

**Done when:** the empty app is deployed and the spike results are recorded in this file.

### Phase 1: port `Clypse.Core`

1. Copy the "Keep" rows and fix the `AwsCredentials` `SecretType` bug.
2. Remove the AWS, Konscious, BouncyCastle and JSInterop references. Apply the blazwiz build settings
   (TreatWarningsAsErrors, StyleCop with `analyzers.globalconfig`) and fix what they find.
3. Keep full text lists for Release WASM builds only, and cache each list once loaded (see the note under clypse.core).
   Check that a Debug build starts quickly and that a Release build uses the full lists.
4. Port the matching unit tests, rename them to `Given_When_Then`, and get them green.

**Done when:** `Clypse.Core` builds without warnings, has no cloud or crypto dependencies, and its tests pass.

### Phase 2: vaults and secrets on sync profiles

1. **Vaults.** A `Vault` model (Id, Name, Description, IsUnlocked, HasPassphrase) and `IVaultService` / `VaultService`.
   It mirrors `NotebookService`: list, find, create (name, description, passphrase, progress), edit the name and
   description (rewrites `.vault`), and delete through `ISyncProtectedProfileService.DeleteAsync`.
2. **Secrets.** `ISecretService` / `SecretService` over `registry.GetFileStore(vault.Id)`:
   - `ListIdsAsync()` lists the secret ids. The vault session then reads them one by one, so the list fills in as they
     are decrypted (D3);
   - `GetAsync(id)`;
   - `SaveAsync(secret)` sets `LastUpdatedAt` and validates with `ClypseObjectValidator`;
   - `DeleteAsync(id)`;
   - `ImportAsync(secrets)` validates every secret first, then writes them.
3. **Vault session.** `IVaultSession` holds the open vault's in-memory secret list. It:
   - starts the background load on unlock;
   - cancels and clears the list on lock (`IE2eProtectedProfileManager.LockStateChanged`);
   - applies `FilesChanged` from sync, so a secret saved on another device appears without a reload.
4. **Labels.** `VaultLabelHandler` re-reads `.vault` on unlock and updates the label on this device.
5. **Tests.** Unit tests for each service against the framework's in-memory stores, following the template's notes
   tests.

**Done when:** a vault can be created, unlocked, filled, and synced between two browsers, and the S3 listing shows only
GUIDs and ciphertext.

### Phase 3: new UI

Design it fresh on the blazwiz shell, with all text in `Text/ClypseText.resx`.

1. **Vaults page.** One card per vault, shown by its label while locked and by name and description once unlocked, with
   create, lock and delete. Creating a vault states clearly that a forgotten passphrase cannot be recovered.
2. **Vault page,** inside `<E2eUnlockPanel>`:
   - the secret list, which fills in progressively (D3);
   - search over name, description and tags, and a tag filter;
   - an add menu per `SecretType`.
3. **Secret editor.** Fields are rendered from `GetOrderedSecretFields()` by `SecretFieldType`: single-line text,
   multi-line text, tag list and password. The password field has reveal, generate, copy and a strength meter (the
   complexity estimator and the weak-password list).
4. **Password generator.** Random passwords by character groups, and memorable passwords from templates, with a
   settings section for the templates and defaults.
5. **CSV import** (KeePass and Cachy formats) into a vault.
6. **Vault security settings.** Per vault: idle timeout, opting in to device unlock, and Argon2 costs (reuse
   `Argon2idPanel`). Decide whether the framework's `E2eProfilesSection` stays or is hidden in favour of the Vaults
   page.

**Done when:** the new UI covers the features listed in the old `portal-views-and-viewmodels.md`, or each one left out
is a deliberate choice, and the view model tests pass.

### Phase 4: framework additions and hardening

1. In blazwiz: clipboard copy with auto-clear (`Browser`) and toasts (`UI`), per D7. Then use them in clypse with a
   default clear time of about 30 s.
2. Idle lock defaults suited to a password manager, for example 5 minutes.
3. Turn on `AllowDeviceUnlock` in the app. It stays off for each vault until the user opts in.
4. A Content Security Policy for the portal. (Already added by blazwiz's deploy; see Progress.)
5. Review the open blazwiz findings that affect clypse, and fix them upstream or document them (done; see Progress):
   - MP1: the status shows only the last profile's pass;
   - MP4: nothing prevents store clashes between profiles;
   - concurrent writes of the profile index secret, for example creating vaults on two devices at once;
   - Secrets Manager deletes skip the recovery window.
6. **Backlog, not required for Phase 4:** make the blazwiz packages trimmable.
   - **Today.** Blazor publishes with `TrimMode=partial`, which trims only assemblies marked trimmable. The blazwiz
     assemblies are not marked, so they ship whole. That is safe, just larger.
   - **What it needs.** `IsTrimmable` on the browser packages (not `Testing` or `Aws`). Every trim analyzer warning must
     then really be fixed, because the assemblies would then be trimmed:
     - Tried on 2026-11-06: 12 errors (IL2026, reflection-based `System.Text.Json`) in `Browser` and `Encryption`
       alone. About 20 more files in `Sync`, `UI` and `Auth.Cognito` call reflection-sensitive APIs and are not
       checked yet.
     - The fix is source-generated JSON contexts in each package.
     - It also breaks public API: `ILocalStorageService`'s generic get and set would take a `JsonTypeInfo<T>`, so
       apps (clypse's password generator preferences included) pass their own context.
   - **The AOT analyzer is not needed.** WebAssembly AOT runs on Mono, which falls back to its interpreter, so
     `IsAotCompatible` (aimed at NativeAOT) does not apply.
   - **Check:** a published AOT build runs the 1,000-secret spike with no errors.

**Done when:** a security checklist passes: key handling, clipboard clearing, CSP, lock behaviour, and no plaintext in
local storage or S3.

### Phase 5: UI tests, CI and deployment

1. Playwright UI tests, starting from the template's (login, E2E encryption, sync, pseudo-localisation). Add:
   - vault create, unlock, lock and delete;
   - secret CRUD;
   - import and the generator;
   - sync between two browser contexts;
   - offline unlock;
   - a mobile device profile.
2. CI from the template: build, unit tests, UI tests against a test stack, and coverage. Add a release workflow that
   deploys the app stack.
3. Rewrite the README and diagrams for the new architecture.

**Done when:** CI is green and a fresh AWS account can deploy from the README alone.

## Later

- Cache the secret index on the device, encrypted with a PRF-derived key, so large vaults open instantly (D3).
- A better conflict resolution system in blazwiz, for example keeping both versions (D6).
- MFA/TOTP once blazwiz supports it.
- A clypse JSON export and import, for backup.
- Restore the Montserrat Black lettering in the SVG art, then switch to SVG where it helps, for example the title-bar
  logo.

## Rough size

| Area | Lines today | Carried over |
|---|---|---|
| clypse.core | 6,691 | about 2,500 |
| clypse.core.UnitTests | 7,230 | about 1,700 (120 tests after the port) |
| Everything else | about 30,000 | none |

The new code is mainly the vault and secret services, the vault session, the pages, the secret editor and the
settings sections, plus their tests. A good part of it follows the template's notes code.

## Risks

- **The weak-known-passwords list is very large.** It adds 21 MB to the app's download, as an embedded resource of
  `Clypse.Core`. Loaded, it becomes a `HashSet` of 5.19 million strings, likely several hundred MB of WASM memory, which
  is probably why loading it is slow even outside Debug. Measure it in a Release build on a phone. Alternatives:
  - a much smaller list (the top 100k);
  - a Bloom filter shipped as a compact binary file;
  - a k-anonymity range check against Have I Been Pwned (online only).
- **blazwiz is pre-1.0.** Its API and stored data can change in any release with no migration. Pin the version
  exactly and check each release for changes to the keys document before upgrading.
- **No passphrase recovery** (blazwiz D6). Say so clearly when a vault is created.
- **Cost.** Each vault has its own Secrets Manager secret, plus one for the index, at about $0.40 per secret per month.
  State this in the README.
- **Argon2id on the UI thread** in WASM. Strong costs on a phone may freeze it for seconds.
- **Paths are visible** in S3 and IndexedDB. GUID names guard against this (D2); review that no secret name ever
  reaches a file key.
- **Whole-file, newest-wins conflicts** until blazwiz has a better system (D6). Editing the same secret on two devices
  while offline loses one of the edits.
