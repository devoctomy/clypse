# clypse AWS deployment

The infrastructure is in two stacks:

- **The blazwiz base stack**, `<AppName>-<Environment>`: CloudFront and S3 for the portal, Cognito, the per-user data
  bucket. It is released with the framework (`Devoctomy.Blazwiz.Aws`, at the app's `BlazwizVersion`), not copied
  here, so upgrading the framework upgrades it: deploy after upgrading. `base-stack.ps1` prints where it is.
- **The app's own stack**, `<AppName>-<Environment>-app`, from `app.yaml`: add the app's own resources there. It can
  import every output of the base stack (`!ImportValue !Sub '${BaseStackName}-DataBucketName'`).

| Script | What it does |
| --- | --- |
| `deploy.ps1` | Creates or updates the base stack, then the app's stack, publishes the portal to S3, invalidates CloudFront and sets up a test user in Cognito. |
| `remove.ps1` | Deletes the app's stack, the users' keys secrets, then empties the buckets (every version) and deletes the base stack, turning the user pool's deletion protection off first. |
| `app.yaml` | The app's own CloudFormation template. |
| `base-stack.ps1` | Finds the base stack and its scripts. |

The scripts here pass the app's name, folder and portal project to the base stack's scripts, and every other
parameter through.

The scripts use the AWS CLI v2 with your normal credential chain. Pass `-AwsProfile` to use a named profile.

## Minimum IAM policy for running the scripts

`deploy.ps1` deploys the base stack through a change set (and the app's stack with `aws cloudformation deploy`) without a CloudFormation service role, so CloudFormation creates the resources using **your** credentials. The deploying identity needs permission for every resource type in the template, as well as for the S3 upload, the CloudFront invalidation, the Cognito user admin calls, and deleting the users' tagged secrets when the environment is removed.

[`deploy-policy.json`](deploy-policy.json) covers `deploy.ps1` and `remove.ps1`, and nothing more.
Before you use it:

- Replace `ACCOUNT_ID` with your 12-digit AWS account id.
- If you deploy with an `-AppName` other than `clypse`, change the `clypse-*` prefixes to match. Stack, bucket and IAM role names all start with `<AppName>-`.

To create it as a managed policy, with your account id filled in:

```powershell
$account = aws sts get-caller-identity --query Account --output text
(Get-Content ./cloudformation/deploy-policy.json -Raw) -replace 'ACCOUNT_ID', $account | Set-Content "$env:TEMP/clypse-deploy-policy.json" -Encoding ascii
aws iam create-policy --policy-name clypse-deploy --policy-document "file://$env:TEMP/clypse-deploy-policy.json"
```

To change an existing policy after updating the file, use `aws iam create-policy-version` with `--set-as-default`, or
paste the file into the policy's JSON editor in the IAM console.

Notes:

- **You don't need permission for `sts:GetCallerIdentity`.** It is always allowed.
- **`UsersSecretsDelete` only matches secrets tagged `app=clypse`**, which the portal adds as it creates them. That is how `remove.ps1` deletes the users' keys secrets with the environment without being able to touch any other secret in the account.
- **`IamPassRoleToIdentityPool` has no `iam:PassedToService` condition.** A condition of `cognito-identity.amazonaws.com` doesn't match when the role is attached to the identity pool, and the deploy fails at `IdentityPoolRoleAttachment`. It is limited by the `clypse-*` role name instead.
- **`CustomDomainCertificate`** is only needed when you pass `-CustomDomainName` / `-CertificateArn`. You can drop it otherwise.
- **IAM is the sensitive part.** The template creates an IAM role, so this identity can create and edit any role whose name starts with `clypse-`. In principle that lets it create a more powerful role for itself. For throwaway testing that is usually fine. For stronger isolation, use a dedicated sandbox account, or add a permissions boundary condition (`iam:PermissionsBoundary`) to `CreateRole` / `PutRolePolicy`.
- **If a deploy fails with `AccessDenied`**, the stack events name the exact missing action. Run `aws cloudformation describe-stack-events --stack-name clypse-dev`, add that action to the policy and deploy again. A failed first deploy leaves the stack in `ROLLBACK_COMPLETE`. Delete it with `remove.ps1` before you retry. A failed update whose rollback also failed (`UPDATE_ROLLBACK_FAILED`, e.g. because the same permission was missing both ways) is recovered with `aws cloudformation continue-update-rollback --stack-name <stack>` once the policy has the action, then deployed again.

### Setting up the credentials

1. Create a customer managed policy (e.g. `clypse-deploy`) from `deploy-policy.json`, as above.
2. Create an IAM user (e.g. `clypse-deployer`) with no console access and attach only that policy.
3. Create an access key for the user and configure a named CLI profile:

   ```powershell
   aws configure --profile clypse-deploy
   ```

4. Use that profile with the scripts:

   ```powershell
   ./cloudformation/deploy.ps1   -AwsProfile clypse-deploy -Region eu-west-2 -TestUserEmail me@example.com
   ./cloudformation/remove.ps1   -AwsProfile clypse-deploy -Region eu-west-2
   ```

When you've finished testing, delete the access key, or the whole user.

## Deploying from GitHub

These workflows deploy from GitHub, each run by hand (Actions, then the workflow, then **Run workflow**):

| Workflow | What it does |
| --- | --- |
| **Deploy** (`deploy.yml`) | Creates a new environment: both stacks, the portal, and optionally the deploy tests' user and the deploy tests. Refused if the environment already exists. |
| **Upgrade** (`upgrade.yml`) | Updates an existing environment to the commit it is run on: both stacks, then the portal (or only the portal), and optionally the deploy tests' user and the deploy tests. Refused if the environment does not exist. |
| **Deploy tests** (`deploy-tests.yml`) | Runs the deploy tests against an existing environment (below). Also run by Deploy and Upgrade when "Run the deploy tests afterwards" is ticked. |
| **Destroy** (`destroy.yml`) | Deletes an environment, once its name is typed again. |

Deploy and Upgrade only run on a commit the CI workflow (`ci.yml`, on every push) has passed, waiting for it if it is still running. Deploy, Upgrade and Destroy:

- use the blazwiz version set in `Directory.Packages.props` (`BlazwizVersion`), from nuget.org;
- refuse a blazwiz build packed locally (`eng/check-blazwiz-version.ps1`).

They run the same scripts as above, with `-Force`, because a workflow can't answer prompts:

- **Replaced resources.** Resources a change replaces are replaced without asking. The user pool, identity pool and data bucket are never replaced (stack policy): a change that would replace one fails the run.
- **No AWS keys in GitHub.** The workflows sign in to AWS through OIDC, assuming an IAM role for the run.

### Setting up GitHub to deploy

Once per AWS account:

1. **Create the OIDC identity provider.** In IAM, go to Identity providers and add one:
   - type **OpenID Connect**;
   - provider URL `https://token.actions.githubusercontent.com`;
   - audience `sts.amazonaws.com`.
2. **Create the `clypse-deploy` policy** from `deploy-policy.json`, if you haven't already.
3. **Create a role,** e.g. `clypse-github-deploy`. Attach only `clypse-deploy`, and give it this trust policy (with `ACCOUNT_ID` replaced). It lets only this repository's workflows assume the role, and only for its GitHub environments:

   ```json
   {
     "Version": "2012-10-17",
     "Statement": [
       {
         "Effect": "Allow",
         "Principal": { "Federated": "arn:aws:iam::ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com" },
         "Action": "sts:AssumeRoleWithWebIdentity",
         "Condition": {
           "StringEquals": { "token.actions.githubusercontent.com:aud": "sts.amazonaws.com" },
           "StringLike": { "token.actions.githubusercontent.com:sub": "repo:devoctomy@14939242/clypse@1448521013:environment:*" }
         }
       }
     ]
   }
   ```

   - **The subject contains numeric ids.** GitHub's token names the repository with its owner's and its own numeric ids
     (`devoctomy@14939242/clypse@1448521013`), so a repository later renamed or recreated with the same name doesn't
     match.
   - **The IAM wizard writes it wrong.** It writes `repo:devoctomy/clypse:*`, without the ids, which never matches: AWS
     refuses with "Not authorized to perform sts:AssumeRoleWithWebIdentity".
   - **To see what GitHub sends,** read the Deploy, Upgrade and Destroy workflows' "Show the OIDC token's claims" step.
   - **To allow only some environments,** list them instead of `*`, e.g.
     `repo:devoctomy@14939242/clypse@1448521013:environment:test`.

Then, in the repository's **Settings, Environments**, add one GitHub environment for each AWS environment, named the same (e.g. `test`). Give it:

| Name | Kind | Value |
| --- | --- | --- |
| `AWS_DEPLOY_ROLE_ARN` | Variable | The role's ARN, e.g. `arn:aws:iam::ACCOUNT_ID:role/clypse-github-deploy`. |
| `AWS_REGION` | Variable | The environment's region, e.g. `eu-west-2`. |
| `TEST_USER_EMAIL` | Variable | For the deploy tests: the test user's email. Use an account kept only for these tests. |
| `TEST_USER_PASSWORD` | Secret | For the deploy tests: the test user's password, at least 12 characters, with upper case, lower case and a number. |
| `CUSTOM_DOMAIN_NAME` | Variable | Optional: the portal's own address, e.g. `test.clypse.net`. Set it with `CERTIFICATE_ARN`. |
| `CERTIFICATE_ARN` | Variable | Optional: an ACM certificate in **us-east-1** covering that address (a wildcard such as `*.clypse.net` will do). |

With a custom address, Deploy and Upgrade put it on the CloudFront distribution and allow it in the data bucket's CORS.
They pass it on every run, because the stack drops the address when it is not given.

They don't create the DNS record: point the address at the distribution yourself. Use a CNAME to its
`dxxxxxxxx.cloudfront.net` name, or a Route 53 alias. The deploy user, or the role, needs the policy's
`CustomDomainCertificate` statement.

To approve each run before it starts, add **required reviewers** to the GitHub environment.

## Checking a deployed environment: the deploy tests

The unit and UI tests run before anything is deployed, with a stand-in sign-in. The deploy tests
(`Clypse.Portal.DeployTests`, Playwright) run after, against the deployed portal: clypse's own UI with the real user
pool. What the blazwiz base stack does (sign-in with the SDK, per-user S3 access, keys) is tested by blazwiz's own
deploy tests, and is not tested again here.

| Test | What it checks |
| --- | --- |
| Signing in | The test user signs in and the vaults page is shown. |
| A wrong password | Signing in with a wrong password shows an error and stays on the login page. |
| Signing out | A signed-in test user signs out and is back at the login page. |

Run them with the **Deploy tests** workflow, or tick "Run the deploy tests afterwards" on Deploy or Upgrade. They need
the environment deployed, and the test user: set `TEST_USER_EMAIL` and `TEST_USER_PASSWORD` on the GitHub environment,
then tick "Create or update the deploy tests' user" on a Deploy or Upgrade run once (again whenever the password
changes).

The workflow uses the deploy role only to read the portal's address from the stack. The tests run with no AWS
credentials: they use the portal in a browser, as the test user. If one fails, its screenshot is in the run's
`deploy-test-results` artifact.

To run them from your own machine, install the browser once
(`pwsh Clypse.Portal.DeployTests/bin/Debug/net10.0/playwright.ps1 install chromium`), set
`CLYPSE_DEPLOYTEST_PORTAL_URL`, `CLYPSE_DEPLOYTEST_EMAIL` and `CLYPSE_DEPLOYTEST_PASSWORD`, and run
`dotnet test Clypse.Portal.DeployTests`. Without them, every test is skipped.
