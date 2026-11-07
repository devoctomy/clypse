# Backlog: a quicker test subset while developing and fixing

**Raised:** 2026-11-07, by the maintainer. Running the full suites after every change is slowing work down too much.
**Status:** not started. Until it is done, the full suites still run as AGENTS.md says.

## The problem

Every change runs everything, in clypse and blazwiz alike, often several times an hour.

Rough times on the development machine (2026-11-07):

| Suite | Time |
|---|---|
| clypse unit tests (core and application) | a few seconds |
| clypse UI tests (57) | about 3 to 3.5 minutes, plus a Release build |
| blazwiz unit and integration tests | well under a minute |
| blazwiz UI tests (57) | about 4 minutes |
| blazwiz template pack test (`test-template.ps1 -SkipRepositoryTests`) | several minutes |
| blazwiz upgrade test (`-UpgradeFrom latest`) | several minutes, plus another UI run |
| Repacking blazwiz for clypse (`use-local-blazwiz.ps1`) | about a minute |

A blazwiz change that clypse depends on runs most of that in sequence: about 15 to 20 minutes.

## What to decide

- **Two levels of checking, written into both AGENTS.md files:**
  - **While developing or fixing:** build, the unit tests of the changed projects, and only the UI tests that cover
    the change.
  - **Before handing work back** (end of a phase, or when asked): the full suites as now, including the template and
    upgrade tests.
- **How UI tests are picked.** MSTest categories (`[TestCategory("Vaults")]`, etc.) or class names, so a subset is one
  `--filter`. A short table in AGENTS.md would say which area each covers.
- **A script for each level** (e.g. `eng/test.ps1 -Quick` / `-Full`), so the same commands run every time and the
  report says which level was run.
- **The template and upgrade tests** run only for changes to public surface, template content or anything apps see,
  as blazwiz's AGENTS.md already says for the template test. Not for every blazwiz change.
- **Repacking blazwiz** only when clypse is about to be built against a blazwiz change, not after every blazwiz edit.
- **Running UI tests in parallel.** MSTest supports it, but each test signs in against one shared portal, so check
  that the tests are independent before turning it on.

## Done when

- Both AGENTS.md files say which level to run when, and the quick level takes a couple of minutes at most for a
  typical change.
- The reports from a quick run say clearly that it was a subset, not the full suite.
