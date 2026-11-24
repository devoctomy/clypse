# Backlog: limit the GitHub deploy role to the deploy workflows' environments

**Raised:** 2026-11-07, while setting up the deploy workflows.
**Status:** done in part. The role's trust policy was limited to the environments on 2026-11-24. The GitHub
environment protection rules are still to do. Both are settings (the AWS console, GitHub): no code changes.

## The problem

The `clypse-github-deploy` role's trust policy allowed any workflow in the repository. GitHub's token names the
repository with numeric ids (shown by the workflows' "Show the OIDC token's claims" step), so the condition was:

```json
"StringLike": { "token.actions.githubusercontent.com:sub": "repo:devoctomy@14939242/clypse@1448521013:*" }
```

That let any workflow run in the repository assume the role: any workflow, on any branch, including one added on a
branch later. The role has the full `clypse-deploy` policy, so such a run could change or delete the environments.

## The trust policy (done 2026-11-24)

On the role's **Trust relationships** tab, the condition now names the GitHub environments the Deploy, Upgrade,
Destroy and Deploy tests workflows run in. Their jobs set `environment:`, so GitHub's token says
`repo:devoctomy@14939242/clypse@1448521013:environment:<name>`. The condition has one entry for each environment:

```json
"StringLike": {
  "token.actions.githubusercontent.com:sub": [
    "repo:devoctomy@14939242/clypse@1448521013:environment:<name>",
    "repo:devoctomy@14939242/clypse@1448521013:environment:<another name>"
  ]
}
```

- **Each environment is listed by name, not as `environment:*`.** GitHub creates an environment the first time a
  workflow names one, with no protection rules. With `*`, a workflow that made up a new environment name could still
  assume the role.
- **A new environment needs its own entry** before its workflows can sign in to AWS.
- **Checked** by the maintainer after the change, with an Upgrade run for the test environment and its deploy tests.

## What the trust policy does not stop

A workflow on any branch can still set `environment:` to one of the listed names, and so assume the role. The role is
shared by every environment, so a run for one environment can change another's resources too. The protection rules
below close this, which is why every listed environment needs them, not only production.

## GitHub environment protection rules (to do)

In the repository, under **Settings, Environments**, open each environment (e.g. `test`) and, under **Deployment
protection rules**:

- **Required reviewers:** add yourself, and anyone else allowed to approve. Every Deploy, Upgrade or Destroy run for
  the environment then waits on its job's "Review deployments" button until one of them approves. Ticking **Prevent
  self-review** stops the person who started the run approving it; leave it off while you are the only reviewer.
- **Wait timer (optional):** a delay, in minutes, before an approved run starts. Useful for Destroy, as a last chance
  to cancel.
- **Deployment branches and tags:** choose **Selected branches and tags** and add `main`. A run from any other branch
  can't use the environment at all, so it gets neither its variables and secrets nor the AWS role. Together with the
  trust policy above, only `main` can deploy.

These rules hold the job back before it starts: the OIDC token is only issued, and AWS only reached, once they pass.
The CI check in Deploy and Upgrade (`ci-passed.yml`) runs first, so an approval is only asked for once CI has passed.

## Done when

Done:

- **The role's trust policy** names only the environments, not the whole repository.
- **An Upgrade run still works** for a listed environment.

Still to do:

- **Each environment** has required reviewers, and deploys from `main` only.
- **A Deploy or Upgrade run still works** for a listed environment, from `main`, once approved.
- **A workflow without `environment:` is refused**, or one for an environment not listed: it can't assume the role.
  The trust policy should already do this; it has not been tried.
- **A run from another branch** is refused by the environment's branch rule.
