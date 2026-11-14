# Manual tests: the AWS deployment

**For:** the first deployment of clypse to AWS (`clypse-test`, or the environment you deployed).
**Why:** these are the checks that couldn't run locally. They're the "Not checked" items in
`docs/reviews/2026-11-07-security-checklist.md`, and the checks that need a phone.

**You need:**
- the portal URL (the deploy prints it, or see the stack output);
- the test user's email and password;
- the AWS console, signed in to the account;
- a second browser or a private window, and a phone.

Note the result beside each test (pass or fail, and anything odd). Send me any failures with what you saw.

---

## 1. The deployed page is locked down

**Steps:**
1. Open the portal URL in Chrome with DevTools open on the Network tab.
2. Select the request for the page itself (the document) and look at its response headers.
3. Sign in as the test user. Watch the Console tab while you do the rest of these tests.

**Expected:**
- **Response headers:** `strict-transport-security` and `x-frame-options` are there, and so is a
  `content-security-policy` meta tag in the page source (View Source, near the top of `<head>`).
- **Console:** no "Refused to …" CSP errors and no other errors, through all the tests below.

strict-transport-security = max-age=31536000
x-frame-options = SAMEORIGIN
content-security-policy = where am I supposed to see this?

Console : dotnet.native.3mg5thhzjn.js:5173 [Violation] 'setTimeout' handler took 233ms

## 2. A vault's files in S3 are ciphertext under GUID names

**Steps:**
1. In the portal, create a vault named **Manual test vault**, with a passphrase you'll remember.
2. Add a login: name **MarkerName42**, user name **marker.user@example.com**, password **MarkerPass!42**. Save.
3. Wait until the sync pill says it's synced.
4. In the AWS console, open the data bucket (`clypse-test-data-…`) and go to your user's folder, then
   `profiles/<vault id>/`.
5. Download `.vault` and one file from `secrets/`, and open them in a text editor.

**Expected:**
- **Paths:** only GUIDs and fixed names (`.vault`, `secrets/<guid>.json`). No "Manual", "MarkerName" or anything you
  typed.
- **File contents:** binary (starting with the `E2EE` header). None of the vault name, the secret's name, user name or
  password.

yup looks encrypted

```
E2EEk_RC8MH6HZDRK0RKMG$03339e4a-9b09-4e83-9866-7c4cb655a6efx»}†³eâO Å·’tÊvbÀ-Øê½L¸nBî2yäËbØyül¿ˆh§nIhöräL&§@ø*0³ïå5ÞPìÑ¼E¡œMQ_¢æƒÕÂ´þ2´oÎ§#+êcOÙKOÕÁ©tNvHî¹lvVÒÜœúöüx;Å•Q–Èa Î-œ
fÓ2GJyƒJ/T[IYJ,H,ÈõûÈ,ga¨Ž§h~ž|¹¡éY‘&‹uÏHyÚ3Pïs±ûHÃŽ<ðþXSpæ.úIÒd°†Sƒµ£;BÌ—„8
d†£!ØÙ€Å|¶”voÃÝµK&í¯)÷È„sIjÝjªR:‡§!’ú®ô5¡´äŒÍÏFzÂÑŒ‹úpIf™éL7Jÿ÷²„‚¤ðá#ë'=‚¯ŠØeEÖœ Uz!Ò<¬RÐÇG”4QF:¡Eþ(îVÜf¤ã´àôIo_.ŠÄ©5:Cý¡Ô]H§ƒ›}Ùß
```

don't trust this encoding, it's just for reference

## 3. The keys in Secrets Manager are wrapped, and names aren't in them

**Steps:**
1. In the AWS console, open Secrets Manager (same region) and find the secrets named `<user sub>/keys/…`.
2. Open `<sub>/keys/index` and `<sub>/keys/<vault id>`, and choose "Retrieve secret value".

**Expected:**
- **The index:** profile ids, created dates and idle timeouts only. No vault names.
- **The vault's keys secret:** Argon2id settings plus a wrapped master key and wrapped file keys (base64). No raw
  keys and no names.
- **Other users' secrets:** you can't open or list them as the test user. blazwiz's deploy tests cover this;
  just note anything odd.

looks fine

```
{
  "version": 1,
  "profiles": [
    {
      "id": "03339e4a-9b09-4e83-9866-7c4cb655a6ef",
      "created": "2026-11-07T11:01:50.79Z"
    }
  ]
}

{
    "version": 1,
    "kdf": {
        "alg": "argon2id",
        "salt": "IWmTf71HoA19lGO+l2CPWw==",
        "memory_kib": 524288,
        "iterations": 2,
        "parallelism": 1,
        "key_len": 32
    },
    "master_key": {
        "alg": "aes-256-gcm",
        "nonce": "y4hhSL0j92B0Y2eA",
        "wrapped": "xi4ghf58dws8lURUBBX/noDSTBT9IrSR27s8yOsRmgTLaB0Ty0YMWv7KaiUTTEjW"
    },
    "keys": {
        "k_RC8MH6HZDRK0RKMG": {
            "created": "2026-11-07T11:01:50.608Z",
            "status": "active",
            "alg": "aes-256-gcm",
            "nonce": "ufWYX+7T0knHSjvj",
            "wrapped": "pqCWJCoYkdXqyFSUVdudNkPudlPOuDEOx33leepc4dnLEUVVnOoT9Gz7KByD2aoT"
        }
    }
}
```

## 4. A second device gets the vault, and changes sync both ways

**Steps:**
1. In a private window (or another browser), sign in as the test user.
2. Open Vaults.
3. Open the vault, unlock it with its passphrase, and edit the login's password to **MarkerPass!43**. Save.
4. In the first window, wait for sync (or press Sync now), then open the vault and the login.

**Expected:**
- **Locked, in the second window:** the vault shows by its **id**, not its name. It has never been unlocked there,
  and this is a password sign-in.
- **Unlocked:** the vault shows by name, and the login is there.
- **Back in the first window:** the password is **MarkerPass!43**.

Yes it works on secondary device.

## 5. With a passkey, a locked vault shows its name; without one, its id

**Steps:**
1. Sign out. Sign in again with your password, tick **Remember me**, and set up the passkey when asked.
2. Unlock **Manual test vault** once, then lock it.
3. Reload the page and look at the Vaults page.
4. Sign out, and sign in again with the passkey (pick your remembered user).
5. In DevTools, open Application, then Local storage, and find the key containing `e2e_labels_`.

**Expected:**
- **After the reload:** the locked vault shows its **id**.
- **After the passkey sign-in:** the locked vault shows **Manual test vault**.
- **The `e2e_labels_` value:** a base64 blob, with no readable vault name in it.

yes vault name is id until unlocked

## 6. Auto-lock and clipboard clearing on the deployed build

**Steps:**
1. Open **Manual test vault**, then Settings (on the vault page), set Auto-lock to **1 minute**, and save.
2. Copy the login's password with its quick-copy button, and paste it somewhere to check.
3. Wait about 35 seconds and paste again.
4. Leave the vault page alone for just over a minute.

**Expected:**
- **The clipboard:** the first paste is the password; the second is empty.
- **After a minute:** the vault locks itself. The page shows the passphrase form and no secrets.

yes both features worked

## 7. Phone: install, sign in, and the 1,000-secret timings

**Steps:**
1. On the phone, open the portal URL. Add it to the home screen, then open it from there.
2. Sign in, create a vault **Phone spike**, and time how long creating it takes (it runs Argon2id at 512 MiB).
3. Import `spike-1000.csv` (KeePass format; it's in `.playwright-mcp/` in the clypse folder) into it. Copy it to the
   phone first, or do the import on the desktop and let it sync. Time the import.
4. Lock the vault. Unlock it, and time both of these:
   - until the page shows;
   - until the count says **1000 secrets** and the loading note has gone.
5. Search for **Site 0999**.

**Expected:**
- **Install:** the app installs and opens like an app.
- **No failures:** no crash, and no "out of memory" or reload, during any step.
- **Timings:** write down create, import, unlock, all 1,000 read, and search. Desktop AOT for comparison: create
  2.0 s, import 1.9 s, unlock 1.7 s, all read 2.3 s, search 7 ms. Anything over about 10 s for create or unlock is
  worth telling me.
- **Search:** finds exactly one secret.

yes it worked fine, i am not providing timings, they were fine for my use

## 8. Deleting a vault removes it everywhere

**Steps:**
1. On the desktop, open **Phone spike**, then Settings. Type its name to confirm, and delete it.
2. Check the data bucket for `profiles/<its id>/`, and Secrets Manager for `<sub>/keys/<its id>`.
3. On the phone (or the second window), reload and open Vaults.

**Expected:**
- **On the desktop:** the vault is gone from Vaults.
- **In AWS:** the S3 folder is empty or gone, and the keys secret is gone. It's deleted without a recovery window
  for now; see `docs/plans/2026-11-07-versioning-and-recovery-plan.md`.
- **On the other device:** it's gone there too after the reload.

vault deleted, not accessible anymore but still listed in vaults on both desktop and mobile. it was deleted on mobile. deletion took me back to the vault unlock page (even though the vault was just deleted and can no longer be unlocked). i'd expect vaults page with the deleted vault gone.

---

**Afterwards:**
- Delete the test vaults, or remove the stack (`./cloudformation/remove.ps1 -Region <region> -Environment test`).
- Delete the deploy user's access key when you've finished.
