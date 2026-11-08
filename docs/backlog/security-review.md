# Security review of clypse

**Date:** 2026-11-08
**Status:** not started. blazwiz had a full review on 2026-11-08 (`docs/reviews/security-review-2026-11-08-01-Fable-5.1.md`
there, fixed in 0.7.0); clypse's own code and workflows have not been reviewed yet.

## Noticed while upgrading to blazwiz 0.7.0

- The Deploy, Upgrade and Destroy workflows use `actions/checkout@v4` and `actions/setup-dotnet@v4` by tag. blazwiz pins
  its actions to commit SHAs (a tag can be moved); `release.yml` here already does.
- `deploy.yml` says the custom domain and certificate must be passed on every run "as the stack drops the address
  when they are not". From blazwiz 0.7.0 the deploy script keeps a stack's current values, so the comment is out of
  date; passing them is still correct.
