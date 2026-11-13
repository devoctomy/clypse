# Security review of clypse

**Date:** 2026-11-08
**Status:** not started. blazwiz had a full review on 2026-11-08 (`docs/reviews/security-review-2026-11-08-01-Fable-5.1.md`
there, fixed in 0.7.0); clypse's own code and workflows have not been reviewed yet.

## Noticed while upgrading to blazwiz 0.7.0

- **Done 2026-11-13:** every workflow's actions are pinned to the commit SHAs blazwiz uses, checkouts set
  `persist-credentials: false`, and Dependabot (`.github/dependabot.yml`) keeps the pins up to date. Before, the Deploy,
  Upgrade and Destroy workflows used `actions/checkout@v4` and `actions/setup-dotnet@v4` by tag.
- `deploy.yml` says the custom domain and certificate must be passed on every run "as the stack drops the address
  when they are not". From blazwiz 0.7.0 the deploy script keeps a stack's current values, so the comment is out of
  date; passing them is still correct.
