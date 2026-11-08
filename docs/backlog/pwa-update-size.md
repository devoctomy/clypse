# Updates download far more than they need to

**Date:** 2026-11-08
**Status:** not started. Found while deploying blazwiz 0.6.0-beta.15; not a security item.

## What was measured

The published `wwwroot` of `clypse-test` (AOT, Release) is 140 MB on disk, 70 MB without the pre-compressed copies.
What the browser downloads on an update:

| File | Raw | Brotli | Why |
|---|---|---|---|
| `_framework/dotnet.native.*.wasm` | 37 MB | 7.5 MB | The ahead-of-time compiled runtime and app code. Changes with every build. |
| `_framework/Clypse.Core.*.wasm` | 21 MB | 21 MB | 20 MB of it is the embedded `Data/Lists/weakknownpasswords.txt.gz`, already gzipped, so brotli gains nothing. Its fingerprint changes with every build, so the list is downloaded again each release although it never changes. |
| Everything else | 12 MB | 2.7 MB | BouncyCastle (4.9 MB raw), the framework assemblies, ICU data (1.5 MB, `BlazorWebAssemblyLoadAllGlobalizationData`). |

## Why updates are slow

1. **The two big files are served uncompressed.** blazwiz's `deploy.ps1` excludes `*.br` and `*.gz` from the upload
   ("CloudFront compresses on the fly"), but CloudFront only compresses objects between 1,000 bytes and 10 MB. The
   runtime and `Clypse.Core` are above that, so about 58 MB of the roughly 61 MB an update downloads goes over the
   wire as is.
2. **The password list travels with the code.** Embedded in `Clypse.Core`, it is re-downloaded with every release.

## Proposed changes

1. **blazwiz, `deploy.ps1`:** upload the pre-compressed `.br` file as the object itself, under the original name, with
   `Content-Encoding: br` and the right `Content-Type`, at least for anything over CloudFront's limit (simplest: for
   every file Blazor pre-compressed). Every browser that runs Blazor WebAssembly accepts brotli over HTTPS. The service
   worker's integrity check is on the decoded body, so the hashes in `service-worker-assets.js` still match; confirm
   that on a deployed environment before relying on it. Expected update size: about 31 MB.
2. **clypse, the weak-passwords list:** stop embedding it. Ship `weakknownpasswords.txt.gz` as a static web asset
   (fingerprinted, so cached until it changes, which is never) and have the loader fetch it with `HttpClient` the
   first time the strength meter needs it, through the existing cache-once service. `Clypse.Core.wasm` drops to
   under 1 MB, and the 20 MB downloads once. The Debug stand-in list and `UseFullWeakKnownPasswordsList` stay as they
   are. Expected update size with both changes: about 10 MB.
3. **Maybe:** `BlazorWebAssemblyLoadAllGlobalizationData` loads every ICU locale (1.5 MB raw). Loading only the
   app's supported cultures would save most of it; worth checking what `Text:SupportedCultures` needs.

Keep AOT: Argon2id at 512 MiB in interpreted WebAssembly would be far slower, and the compiled runtime is 7.5 MB
compressed.
