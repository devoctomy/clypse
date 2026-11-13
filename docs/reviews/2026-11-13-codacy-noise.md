# Codacy noise

Review of the 319 issues Codacy reported for `gh/devoctomy/clypse` on 2026-11-13, at the last analysed commit, and
what was done about each.

- **Fixed in code: 21 issues.** These are listed under "Fixed in code".
- **Everything else is noise.** It was turned off on Codacy on 2026-11-13, either as whole tools or patterns, or by
  ignoring single issues.

The repository uses no coding standard, so every change is per repository. Changes on Codacy show at the next
analysis.

## Already off

These tools were already disabled on Codacy. Their issues are left over from an older analysis and go at the next
one:

| Tool | Pattern | Issues | Notes |
|---|---|---|---|
| Checkov | `CKV_GHA_7` | 5 | The workflow_dispatch inputs are wanted. |
| Biome | `noUnusedFunctionParameters` | 2 | Fixed in code anyway. |
| ESLint (deprecated) | `ESLint_prettier-vue_prettier` | 47 | Prettier formatting for Vue. There is no Vue and no Prettier config. |
| ESLint (deprecated) | `ESLint_compat_compat` | 4 | Flags `URL` for Opera Mini. A service worker never runs there. |
| ESLint (deprecated) | `ESLint_node_no-unsupported-features_node-builtins` | 2 | Node version checks on browser code. |
| ESLint (deprecated) | `ESLint_no-unused-vars` | 2 | Fixed in code anyway. |
| JSHint (deprecated) | `unused` | 2 | Fixed in code anyway. |

## Tools turned off

| Tool | Issues | Why |
|---|---|---|
| Agentlinter | 25 | It lints `AGENTS.md` as a persona prompt: escape hatches, identity, tool docs, splitting files. `AGENTS.md` sets absolute rules on purpose. |

## Patterns turned off

| Tool | Pattern | Issues | Why |
|---|---|---|---|
| ESLint | `ESLint8_security-node_detect-unhandled-async-errors` | 2 | The install errors are meant to propagate to `event.waitUntil`, which fails the install so it is retried. |
| ESLint | `ESLint8_@typescript-eslint_no-empty-function` | 1 | The dev service worker's empty `fetch` handler is deliberate (blazwiz template). |
| ESLint | `ESLint8_@typescript-eslint_no-unused-vars` | 2 | Duplicates `ESLint8_no-unused-vars`, which is kept on. |
| SonarC# | `SonarCSharp_S2360` optional parameters | 39 | Nearly all are `CancellationToken ct = default`, the .NET convention. The rest are test helpers. |
| SonarC# | `SonarCSharp_S2339` public constants | 27 | Error codes, resource keys and settings section names: they have to be compile-time constants, e.g. in `switch` and in attributes. |
| SonarC# | `SonarCSharp_S2333` redundant `partial` | 17 | `partial` is required: the CommunityToolkit.Mvvm source generators and Razor code-behind fill in the other half. |
| PSScriptAnalyzer | `psscriptanalyzer_psavoidusingwritehost` | 7 | The `eng/*.ps1` scripts are run by hand and print progress for a person, as blazwiz's scripts do. |
| Stylelint | `Stylelint_color-no-hex` | 8 | The hex colours are the theme's tokens, defined once as CSS custom properties. |
| Stylelint | `Stylelint_color-hex-length` | 1 | Style choice: `#ff6600` next to the other six-digit tokens. |
| Stylelint | `Stylelint_media-feature-range-notation` | 1 | `(min-width: 768px)` matches Bootstrap's breakpoints. |
| markdownlint | `markdownlint_MD032` lists surrounded by blank lines | 35 | GitHub renders a list straight after a line of text correctly, and the docs are written that way throughout. |
| markdownlint | `markdownlint_MD029` ordered list prefix | 10 | The plan numbers a list on from an earlier one (4 to 13) on purpose. GitHub renders the start number. |
| markdownlint | `markdownlint_MD024` duplicate headings | 4 | The plan repeats phase headings under different sections. |
| markdownlint | `markdownlint_MD033` inline HTML | 3 | The README's centred logo needs HTML. |

`ESLint8_no-unused-vars` stays on, so unused JavaScript variables are still reported.

## Deleted

`.github/instructions/codacy.instructions.md` (50 issues: MD005, MD007, MD022, MD032, MD034) was added by Codacy's IDE
extension without asking, and has been deleted. Its issues go at the next analysis.

## Single issues ignored (false positives)

The patterns are worth keeping. These issues are ignored on Codacy as false positives, each with the reason as its
comment:

| Pattern | Where | Why it's wrong |
|---|---|---|
| SonarC# `S1172` unused `sender` (3) | `VaultLabelHandler.cs:56`, `VaultSession.cs:290`, `VaultsViewModel.cs:378` | Event handlers: the signature is fixed by `EventHandler<E2eKeyScope>`. |
| SonarC# `S3453` private constructor (2) | `ClypseText.cs:7`, `ClypseSyncText.cs:7` | Marker types for `IStringLocalizer<T>`. They are never meant to be created. |
| SonarC# `S3264` event never invoked | `VaultSession.cs:51` | `Changed` is invoked by `Notify()` (line 288). |
| SonarC# `S3241` return value unused | `VaultServiceTests.cs:441` | `AddVault()` returns `AddProfile`'s value. |
| SonarC# `S2737` catch only rethrows | `VaultService.cs:203` | It stops the `catch (Exception)` below from swallowing cancellation. |
| SonarC# `S1121` assignment in expression | `HashedTextSet.cs:60` | The usual `stackalloc` or rented-array pattern; the rented array is returned in `finally`. |
| markdownlint `MD034` bare URL | `docs/manualtests/2026-11-07-aws-deployment.md:39` | It is an example e-mail address in bold, not a link. |

## Fixed in code

| Pattern | Where | Fix |
|---|---|---|
| Unused `event` parameter (10, from five linters) | `service-worker.published.js:29,68` | Removed the parameter from `onInstall` and `onActivate`. The same change was made to the blazwiz template's copy, so the two files stay identical. |
| SonarC# `S1481` unused local | `DictionaryTokenProcessorTests.cs:122` | Removed `expectedWord`. |
| SonarC# `S3052` initialising to the default | `RandomGeneratorService.cs:12` | `private bool disposed;` |
| SonarC# `S1450` field only used as a local | `StandardWesternPasswordGeneratorServiceTests.cs:12` | It is now a local in the constructor. The service under test disposes it. |
| SonarC# `S3257` unused lambda parameters (3) | `RandomStringTokenProcessorTests.cs:86`, `StandardWesternPasswordGeneratorServiceTests.cs:56`, `PasswordGeneratorViewModelTests.cs:38` | Discards. |
| SonarC# `S1117` local hides a field | `VaultServiceTests.cs:357` | Renamed the local `unlocked` to `result`. |
| Opengrep: third-party action not pinned to a commit SHA (4) | `deploy-tests.yml:68`, `deploy.yml:110`, `destroy.yml:75`, `upgrade.yml:115` | `aws-actions/configure-aws-credentials@e1253824e5c10ff9df46874f81ed3ec929e19cfd # v6.3.0`, the pin blazwiz's workflows use, with the same inputs. |

Codacy only flags third-party actions, but blazwiz pins its first-party `actions/*` too, so clypse now does the same.
Every `uses:` of `actions/*` is pinned to the commit SHA blazwiz uses:

- `actions/checkout` v7.0.1. Every checkout now sets `persist-credentials: false`, as blazwiz does. No step runs git
  afterwards: `gh` takes its token from `GH_TOKEN`.
- `actions/setup-dotnet` v6.1.0.
- `actions/upload-artifact` v7.0.2.
