# Plan: secret history and recovering deleted vaults

**Date:** 2026-11-07
**Status:** not started. A plan only: nothing here is built yet, and the open questions need the maintainer's answers
first.
**Related:**
- `docs/plans/2026-11-06-blazwiz-rewrite-plan.md`, Phase 4 (where this was split off).
- blazwiz `docs/requests/2026-11-05-protected-profile-recovery.md`. That one is about a different recovery: getting
  back into a vault whose **passphrase** is forgotten. This plan is about getting back **data** that was changed or
  deleted.

## Goal

1. **Secret history.** See earlier versions of a secret, and roll a secret back to one.
2. **Recovering a deleted vault.** For a while after a vault is deleted, it can still be brought back.

## How things are today

- **Each secret is a file.** It lives at `profiles/<vaultId>/secrets/<secretId>.json` in the user's prefix of the
  data bucket, encrypted end to end and bound to its path. Saving a secret replaces the file.
- **The data bucket has no versioning.** An overwrite or delete is final.
- **Deleting a vault** (blazwiz `ISyncProtectedProfileService.DeleteAsync`):
  1. finds the vault's files in S3, and stops with nothing deleted if S3 cannot be reached;
  2. deletes the keys secret (`<sub>/keys/<vaultId>`) with `ForceDeleteWithoutRecovery`;
  3. removes the vault from the index secret;
  4. deletes its files.

  Once the keys are gone, the files cannot be decrypted even if they were kept. So recovery needs both the files and
  the keys back.
- **Each save stamps the secret.** `Secret.LastUpdatedAt` is set from `TimeProvider`.

## Approach (proposed)

### S3 versioning on the data bucket (blazwiz base stack)

- **Turn on versioning** for the data bucket in the base stack (`src/Devoctomy.Blazwiz.Aws/cloudformation/stack.yaml`).
  - An overwrite then keeps the old object as a noncurrent version.
  - A delete leaves a delete marker, and the object stays underneath.
- **A lifecycle rule** expires noncurrent versions after N days and removes expired delete markers, so storage
  doesn't grow without end.
- **The user's IAM policy** gains `s3:ListBucketVersions` and `s3:GetObjectVersion` on their own prefix. Rolling back
  is a normal `PutObject` of the old content, so it needs no new write permission.
- **This is a framework change.** Every blazwiz app gets versioning. It belongs in blazwiz, as a stack option with a
  default, not as clypse code.

### Reading history (blazwiz Sync, then clypse)

- **blazwiz:** an `ISyncFileHistory` service, or a method on the remote store, that:
  - lists a file's versions (version id, time, size);
  - reads one version, decrypted through the profile's keys.

  Old versions decrypt with the same keys, because they sit at the same path.
- **clypse:** a "History" view in the secret editor. It lists the versions by time, and shows a version's fields
  (secret values masked until shown).
- **Rolling back** writes the old content as the secret's new current version. That's an ordinary save, so it syncs
  to other devices as usual and the rollback itself has a history entry.
- **Offline:** history comes from S3 only. Devices keep just the current file, so the history view needs a
  connection.

### Recovering a deleted vault (blazwiz Encryption and Sync, then clypse)

- **Keys:**
  - delete the keys secret with a recovery window (`RecoveryWindowInDays`, 7–30), not `ForceDeleteWithoutRecovery`;
  - restoring calls `RestoreSecret`;
  - the user's IAM policy gains `secretsmanager:RestoreSecret` on `<sub>/keys/*`.
- **Files:** with versioning on, the vault's deleted files are delete markers. Restoring removes the markers, or
  copies the last version back. That needs `s3:DeleteObjectVersion` on the user's prefix.
- **Index:** the vault's entry is gone from the index secret.
  - **To make restoring possible,** keep a "deleted" entry instead, with the deletion time, until the recovery window
    ends.
  - **Then:** other devices forget the vault as they do now; restoring puts the entry back.
- **clypse:** a "Recently deleted" list on the Vaults page, with Restore and the date each vault goes for good.

## Open questions

1. **How long are versions and deleted vaults kept?** For example 30 days. The answer sets the lifecycle rule and the
   Secrets Manager recovery window (at most 30 days). Every kept version costs S3 storage, and a deleted vault's keys
   secret keeps costing about USD 0.40 a month until the window ends.
2. **Is versioning always on for blazwiz apps, or a stack option?** It changes cost and behaviour for every app.
3. **Can a user delete a vault for good at once?** (For example, to remove data deliberately.) If so, that needs
   `ForceDeleteWithoutRecovery` and version deletes, with a stronger confirmation.
4. **How much history to show:** every version within the window, or only the last N?
5. **Deleting a single secret.** Should it be recoverable too, as a "Recently deleted" list inside a vault, since
   versioning keeps it anyway?
6. **Sync conflicts.** Today the newest write wins (D6). Should the version that lost a conflict be shown, with a way
   to restore it? Versioning makes that possible.
7. **Clearing history.** A rollback creates a new version, so earlier ones are never lost within the window. Is that
   right, or should a user be able to clear a secret's history?

## Security notes

- **Old versions stay encrypted** with the vault's keys and are only readable by the user. A changed passphrase does
  not re-encrypt them, because the passphrase wraps the master key and the files stay encrypted with the same key.
- **Versioning keeps data longer.** Data that is changed or deleted still exists until it expires. The app should say
  so where it matters: deleting a vault, and anywhere it claims something is "deleted".
- **IAM stays narrow.** The new permissions are limited to the user's own prefix and keys, as now.

## Phases (once the questions are answered)

1. **blazwiz base stack:** versioning, the lifecycle rule and the IAM additions. `test-auth.ps1` checks the new
   permissions. Deployed by the maintainer.
2. **blazwiz Sync:** file history (list and read versions), with unit tests over a fake remote store and an
   integration scenario.
3. **clypse:** the secret history view and rollback.
4. **blazwiz Encryption and Sync:** vault deletion with a recovery window, a "deleted" index entry, and restore.
5. **clypse:** Recently deleted on the Vaults page.
