# Security checklist

**Date:** 2026-11-07
**Build checked:** clypse on blazwiz `0.4.0-local.20261107093211` (the `clypseupdates` working tree).
**Context:** the security checks for the first deployable build. Each item below says how it was checked. What needs a deployed
stack (Cognito, Secrets Manager, S3) is listed under "Not checked" at the end.

## How it was checked

- **The browser checks** used `.playwright-mcp/security.js` (git ignored), run through Playwright in Chromium.
  - **Target:** `dotnet run -c Release` in the Development environment, with placeholder sign-in and keys kept in the
    browser, because there is no Cognito locally.
  - **Data:** a new vault holding one login with marker values for its name, user name and password.
- **CSP:** a local AOT publish, with the policy that blazwiz's deploy adds (`Add-ContentSecurityPolicy`), copied by
  hand into `index.html`. Served statically.
- **Tests:** clypse's suite (131 core, 222 application, 57 UI unit and UI tests) and blazwiz's suite, all passing on
  the same day.

## Results

| # | Check | Result | How |
|---|---|---|---|
| 1 | No secret content in browser storage | **Pass** | Every localStorage and sessionStorage value, and every IndexedDB record in every database, was searched for the three markers, as UTF-8 and UTF-16 inside binary values too. None found. The secret files are `E2EE` ciphertext under GUID keys. |
| 2 | No plaintext vault details in browser storage | **Pass (fixed 2026-11-07)** | At first the vault's name was in localStorage in plain text, as its device label. Labels are now kept only encrypted (AES-GCM) with a key derived from the passkey at sign in (see below). Checked in Chromium with a virtual authenticator (PRF): the entry did not contain the name; after a reload the vault showed by id; after signing in with the passkey it showed by name again, locked. |
| 3 | Keys in browser storage are wrapped only | **Pass** | `e2e_browser_keys_*` (local stand-in for Secrets Manager) holds the Argon2id settings, and the master key and vault key, each wrapped (AES-GCM, nonce and ciphertext). `e2e_keys_*` (the device's cached copy) holds the same wrapped document; its device-unlock slot is empty while passkey unlock is off. No raw key material. |
| 4 | Passphrase key derivation | **Pass (by configuration)** | Release builds use the framework's Argon2id defaults: 512 MiB, 2 iterations (`Argon2idOptions`). Only `appsettings.Development.json` lightens them (19 MiB), and it is not published. A passphrase change rewraps the master key (`E2eProtectedProfileManager.ChangePassphraseAsync`). |
| 5 | Clipboard clearing | **Pass** | Quick copy of the password put it on the clipboard; 32 s later the clipboard was empty (`Clypse:ClipboardClearSeconds`, 30 by default). Non-secret fields are not cleared, by design. Covered by blazwiz's `ClipboardService` unit tests and clypse's copy tests too. |
| 6 | Auto-lock | **Pass** | The vault set to lock after 1 minute locked 59 s after the setting was saved, with no input in between. The unlock form replaced the page, no secret rows were left in the page, and the markers were gone from its HTML. |
| 7 | Lock on demand and on sign out | **Lock: pass (tests). Sign out: not re-checked** | Clypse's UI tests lock and unlock a vault (`VaultTests`). blazwiz locks and removes every protected profile at sign out (its 0.4.0 changelog); that was not checked again here. |
| 8 | Content Security Policy | **Pass** | Under the deploy's policy, the 1,000-secret flow, quick copy, the generator and Settings ran with no violations and no console errors. Scripts: self, `wasm-unsafe-eval` and the inline scripts by hash. Connections: this region's Cognito, Secrets Manager and data bucket only. Framing refused by CloudFront's security headers (not checked locally). |
| 9 | Weak passwords flagged | **Pass** | The strength meter checks the weak known passwords list after typing pauses; a UI test types one and sees "Very weak: it is a commonly used password." |
| 10 | File names reveal nothing | **Pass** | Secret files are `secrets/<guid>.json`. Ids are checked to be GUIDs, so a name can't reach a path (`SecretService`). |

## Vault names on the device (fixed)

- **What blazwiz does.** It labels a protected profile on each device so Settings, the unlock panel and the Sync
  page can show a name instead of an id.
  - The label is kept only on the device and never synced.
  - clypse sets it to the vault's name whenever the vault is unlocked (`VaultLabelHandler`).
- **The bug.** The label was kept in plain text. Encrypting the name was asked for, and that was missed when the
  vaults were built.
- **The fix.** `LocalStorageE2eProfileLabelStore` now keeps the labels only encrypted (AES-GCM) with
  `E2eKeyNames.LabelKey`. That key is derived from the passkey's PRF output at sign in, beside `rememberme` and
  `e2e-kek`, and is held in memory only.
- **What a locked vault shows:**
  - **Signed in with the passkey:** its name.
  - **Signed in with a password, or after a reload until the next passkey sign in:** its id. A name set then
    lasts only until the page is closed.

## Other observations (not failures)

- **Storage key names include the user name** (e.g. `clypse_e2e_index_<user>`), so the account name is visible in
  local storage. The signed-in session (`clypse_session`) holds the user name and email too. That's expected for a
  session record.
- **Chromium's verbose console notes** that a password form has no user name field: the vault unlock form. That's an
  accessibility hint, not a security issue.

## Not checked (needs the deployed stack)

- What S3 holds: that objects are ciphertext under GUID keys only, and that one user can't read another's prefix.
- Secrets Manager: per-user keys secrets, and that the IAM policy confines each user to `<sub>/keys/*`.
  (blazwiz's deploy tests check this, against its own deployment of the same base stack.)
- CloudFront's response headers (HSTS, `X-Frame-Options`) and the CSP on the deployed page.
- The same checks on a phone.
