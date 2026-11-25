---
title: Fixes from using clypse (vault and vaults pages, biometrics, device name)
type: plan
status: not started
added: 2026-11-25
origin: the maintainer, using clypse
areas: [vault page, vaults page, VaultSession, blazwiz]
related: blazwiz docs/plans/active/passkey-key-and-device-rename-plan.md
---

# Fixes from using clypse

The maintainer reported ten issues while using clypse. Eight are fixed here. Two are fixed in blazwiz (agreed with
the maintainer), in its plan `docs/plans/active/passkey-key-and-device-rename-plan.md`. clypse picks those up from a
local blazwiz build (`eng/use-local-blazwiz.ps1`).

| # | Issue | Fixed in |
|---|---|---|
| 1 | The vault page shows the vault's id after its first unlock | clypse |
| 2 | An "Untagged" tag filter; "All" and "Untagged" in their own colour | clypse |
| 3 | Locking a vault leaves the passkey's key in memory, so biometric unlock doesn't prompt | **blazwiz** |
| 4 | A secret's tags in a row along the bottom of its card | clypse |
| 5 | "Showing x/y secrets" under the tags | clypse |
| 6 | Remove the vaults page subtitle | clypse |
| 7 | Remove "Change a vault's passphrase or delete it in Settings." | clypse |
| 8 | The id hint only when a vault is shown by id, with an (i) on that vault | clypse |
| 9 | The forget biometrics button looks like a delete | clypse |
| 10 | Renaming the device doesn't rename the remembered device | **blazwiz** |

## clypse

### 1. BUG: the vault page shows the id after the first unlock

- **Cause:** the page opens the vault while it is locked, so `VaultService.DescribeAsync` names it by its label or
  its id (the details file can't be read yet). When it unlocks, `VaultSession.OnLockStateChanged` only sets
  `Vault.IsUnlocked`. It never reads the name and description, so the header keeps the id until the page is opened
  again.
- [ ] On unlock, `VaultSession` reads the vault again (`IVaultService.FindAsync`) and takes its `Name` and
      `Description`, then tells the page (`Changed`).
- [ ] Unit test: given a vault opened locked and shown by id, when it is unlocked, then the session's vault has its
      name and description.
- [ ] Check in the running portal: a vault never unlocked on this device shows its name in the header as soon as it is
      unlocked.

### 2. FEAT: an "Untagged" filter, with "All" and "Untagged" set apart

- [ ] `VaultViewModel`: an "Untagged" selection that matches secrets with no tags (after trimming). It sits beside
      `SelectedTag` without clashing with a user tag of the same name (e.g. a separate flag, not a magic tag string).
- [ ] Show "Untagged" after "All" only when at least one secret has no tags.
- [ ] `VaultPage.razor` and CSS: "All" and "Untagged" get their own colour (one class for both, e.g.
      `tag-filter-builtin`), in light and dark themes, active and inactive.
- [ ] Resource `Vault.Tags.Untagged`.
- [ ] Unit tests: selecting "Untagged" shows only untagged secrets; selecting it again, or "All", clears it; it works
      with the search.

### 4. CHANGE: a secret's tags along the bottom of its card

- [ ] `VaultPage.razor`: move `secret-tags` out from the right of the row to a wrapping row under the name and summary,
      on mobile and desktop.
- [ ] CSS: tiles that wrap; they no longer hide on narrow screens.
- [ ] Rows with tags get taller: set `Virtualize`'s `ItemSize` to the new height, or measure it, so scrolling a large
      vault doesn't jump.
- [ ] Check at phone and desktop widths, with no tags, one tag and many tags.

### 5. CHANGE: "Showing x/y secrets" under the tags

- [ ] `VaultViewModel.CountText`: "Showing {0}/{1} secrets", where x is the secrets matching the search and tag
      (`Secrets.Count`) and y is all of them (`SecretCount`), with plural forms.
- [ ] `VaultPage.razor`: move `secrets-count` from beside the search box to under the tag filters (under the search
      box when the vault has no tags).
- [ ] Update the resources (`Vault.Count.*`), the unit tests and any UI or deploy test that reads `#secrets-count`.

### 6. CHANGE: remove the vaults page subtitle

- [ ] Remove `Vaults.Subtitle` ("Each vault has its own passphrase, …") and the `Subtitle` on `VaultsPage.razor`'s
      `PageHeader`.

### 7. CHANGE: remove "Change a vault's passphrase or delete it in Settings."

- [ ] Remove the sentence from `Vaults.IdHint`.

### 8. CHANGE: the id hint only when a vault is shown by id

- [ ] `VaultsViewModel`: `HasVaultShownById` (any vault where `IsShownById` is true).
- [ ] `VaultsPage.razor`: show the hint only then.
- [ ] Put a `bi-info-circle` next to the name of each vault shown by id, with the hint as its tooltip and as
      visually hidden text.
- [ ] Unit tests for `HasVaultShownById`; update any UI test that expects the hint.

### 9. CHANGE: the forget biometrics button looks like a delete

- [ ] `VaultsPage.razor`: replace `btn-outline-danger` and `bi-trash` with a neutral button (`btn-outline-secondary`)
      and an icon that doesn't read as a delete (e.g. `bi-eraser`; the maintainer chooses).

### Changelog

- [ ] Add every user-visible change above to `Unreleased` in `CHANGELOG.md` (Added, Changed, Fixed).

## blazwiz (issues 3 and 10)

Tracked in blazwiz's plan. Once it is done:

- [ ] Run `eng/use-local-blazwiz.ps1` and build clypse on the local blazwiz build.
- [ ] Check in the running portal: after a passkey sign-in, unlocking a vault with biometrics prompts; after a lock,
      it prompts again.
- [ ] Check in the running portal: renaming the device warns that it will be forgotten and that MFA is asked at the
      next sign-in.
- [ ] Add issues 3 and 10 to `Unreleased` in `CHANGELOG.md`.

## Checking

- [ ] Each change checked in the running portal first (unit tests of the changed projects only, between attempts).
- [ ] Hand the portal to the maintainer to try.
- [ ] Once they confirm: blazwiz's full suites, then clypse's unit and UI tests, once, on packages rebuilt from the
      current blazwiz tree.
