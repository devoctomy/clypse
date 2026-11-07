# Backlog: limit the GitHub deploy role to the deploy workflows' environments

**Raised:** 2026-11-07, while setting up the deploy workflows.
**Status:** not started. Done in the AWS console; no code changes.

## The problem

The `clypse-github-deploy` role, created with IAM's **Web identity** wizard, gets a trust policy condition like:

```json
"StringLike": { "token.actions.githubusercontent.com:sub": "repo:devoctomy/clypse:*" }
```

That lets any workflow run in the repository assume the role: any workflow, on any branch, including one added on a
branch later. The role has the full `clypse-deploy` policy, so such a run could change or delete the environments.

## The change

On the role's **Trust relationships** tab, limit the condition to the GitHub environments the Deploy, Upgrade and
Destroy workflows run in. Their jobs set `environment:`, so GitHub's token says
`repo:devoctomy/clypse:environment:<name>`:

```json
"StringLike": { "token.actions.githubusercontent.com:sub": "repo:devoctomy/clypse:environment:*" }
```

or one environment only, e.g. `repo:devoctomy/clypse:environment:test`.

## GitHub environment protection rules

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

- **The role's trust policy** names only the environments, not the whole repository.
- **Each environment** has required reviewers, and deploys from `main` only.
- **A Deploy or Upgrade run still works** for an allowed environment, from `main`, once approved.
- **A workflow without `environment:` is refused**, or one for an environment not listed: it can't assume the role.
- **A run from another branch** is refused by the environment's branch rule.
