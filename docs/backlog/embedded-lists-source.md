# Where the embedded lists come from

**Date:** 2026-11-13
**Status:** not started. Waiting on the maintainer to find where the lists came from.

## The lists

`Clypse.Core/Data/Lists/`, copied unchanged from the old clypse:

| File | Size | Used for |
|---|---|---|
| `weakknownpasswords.txt.gz` | 21 MB (over five million entries) | The strength check flags a password found in it (`WeakKnownPasswordsService`). |
| `adjective.txt`, `noun.txt`, `verb.txt` | 8 to 11 KB each | Memorable passwords (`{dict(...)}` tokens). |

Neither this repository nor the old clypse records where they came from, under what licence, or which version.
`ssoproviders.txt` is clypse's own list, not a copy.

## To do

1. **Find the source of each list** and its licence. Large weak-password lists usually come from breach dumps (RockYou
   and the like) or collections such as SecLists, whose licences and terms vary.
2. **Record it** the way blazwiz records vendored files in `THIRD-PARTY-NOTICES.md`: file, project and version, source
   URL, licence, SHA-256. Put the licence file beside the list if the licence asks for it. Check the licence allows
   shipping the list inside the app, which is MIT.
3. **Update the weak-passwords list** to the source's current version, or choose a better maintained source. Record the
   new version and hash. Keep the Debug stand-in (`TextListOptions.UseFullWeakKnownPasswordsList`) as it is.

Related: `pwa-update-size.md` proposes shipping the weak-passwords list as a static web asset instead of embedding it.
If both are done, do them together so the list is replaced once.
