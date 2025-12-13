[![Tests](https://github.com/devoctomy/clypse/actions/workflows/tests.yml/badge.svg)](https://github.com/devoctomy/clypse/actions/workflows/tests.yml)
[![codecov](https://codecov.io/gh/devoctomy/clypse/graph/badge.svg?token=6FUB0KRLUV)](https://codecov.io/gh/devoctomy/clypse)

[![Deploy](https://github.com/devoctomy/clypse/actions/workflows/deploy.yml/badge.svg)](https://github.com/devoctomy/clypse/actions/workflows/deploy.yml)

# clypse
Clypse secrets management.

## Requirements

1. Aws Cognito confgurations
    - Identity Pool
    - User Pool
2. Some Aws credentials which have access to a test S3 bucket
3. A test S3 bucket

Once you have setup all of the above, you will need to configure the 'appsettings.json' configuration file within the clypse.portal wwwroot folder,

```
  "AwsS3": {
    "BucketName": "...",
    "Region": "..."
  },
  "AwsCognito": {
    "UserPoolId": "...",
    "UserPoolClientId": "...",
    "Region": "...",
    "IdentityPoolId": "..."
  },
```

> Make sure you do not check in any sensitive information into source control. If you are testing locally you can use 'appsettings.Development.json' which is gitignored.

One way to test the setup is to use the Test Page which you can access from the Login page when in Debug configuration. Enter your Cognito user credentials and then click the button to 'Test Clypse Core', this will create a vault for you with a test credential, the password will be 'password123.

## Testing

### Unit Testing

You can simply run the unit tests from within Visual Studio or use the following command from the project root,

```
dotnet test clypse.core.UnitTests --no-build --configuration Release --verbosity normal --logger trx --results-directory ./TestResults/UnitTests
```

### Integration Testing

Integration testing requires the following environment variables

* CLYPSE_INTTEST_AWS_BUCKETREGION - Region of the test bucket
* CLYPSE_INTTEST_AWS_BUCKETNAME - Test bucket name
* CLYPSE_INTTEST_AWS_ACCESSKEY - IAM credentials for accessing the s3 bucket
* CLYPSE_INTTEST_AWS_SECRETACCESSKEY - IAM credentials for accessing the s3 bucket

The integration tests test the underlying core framework, integrating with AWS S3, without any UI. The tests will create a vault, with a number of secrets and then clean up after itself.

> The integration tests do not test any part of Aws Cognito integration. This is done with the UI tests.

### UI Tests

UI Tests are done by Playwright which should be installed prior to running.

https://playwright.dev/dotnet/docs/intro

Basically the PlayWright package adds a PowerShell script to the bin output folder, this is what you use in Windows to install Playwright and the necessary browsers. You will also require the following environment variables,

* CLYPSE_UITESTS_USERNAME - Aws Cognito username for the test user
* CLYPSE_UITESTS_PASSWORD - Aws Cognito password for the test user

### Manual Testing

You can either simply press play in Visual Studio, which should launch the portal in Debug build on port 7153, or you can perform the full WASM build which is *much* faster and is what gets published to production. The Debug build also has certain features cranked down in order to improve performance merely during running simple tests,

1. The default cryptographic Argon2id key derivation parameters are set to 64mb. This is still slow, but adequate for testing.
2. Weak password list is hardcoded to a single entry, as JS <-> C# interop is extremely slow when not running the full WASM build.

The easiest way serve the full WASM build locally you can use the dotnet-serve tool. To install it run the following command,

```
dotnet tool install -g dotnet-serve
```

Then run the following to publish the build locally and serve it over SSL.

```
dotnet tool install -g dotnet-serve
dotnet publish ./clypse.portal/clypse.portal.csproj -c Release -r browser-wasm --self-contained
dotnet serve -d ./clypse.portal/bin/Release/net10.0/publish/wwwroot -p 7153 --tls
```

> You must have CORS configured for the S3 bucket for 'https://localhost:7153' otherwise all requests to S3 will fail.

## GitHub Workflows

The project uses a comprehensive CI/CD pipeline that runs automated tests and deploys to production when requested.

### Workflow Overview

The CI/CD workflow (`cicd.yml`) consists of four sequential jobs:

#### 1. Unit Tests
- **Purpose**: Validates core library functionality without external dependencies
- **Environment**: Ubuntu container with Playwright for .NET
- **Steps**:
  - Installs Python (required for WASM build tools)
  - Sets up .NET 10.0 SDK
  - Configures HTTPS development certificates
  - Installs WASM tools workload
  - Restores dependencies and builds the project
  - Runs unit tests from `clypse.core.UnitTests`
  - Uploads test results as artifacts
- **Required Secrets**: None

#### 2. Integration Tests
- **Purpose**: Tests AWS S3 storage integration using the core library
- **Environment**: Ubuntu container with Playwright for .NET
- **Steps**:
  - Same setup as unit tests
  - Runs integration tests from `clypse.core.IntTests` with AWS credentials
  - Tests vault creation and secret management against a real S3 bucket
  - Uploads test results as artifacts
- **Required Secrets**:
  - `CLYPSE_INTTEST_AWS_BUCKETREGION`     - AWS region for test bucket
  - `CLYPSE_INTTEST_AWS_BUCKETNAME`       - Name of the S3 test bucket
  - `CLYPSE_INTTEST_AWS_ACCESSKEY`        - IAM access key with S3 permissions
  - `CLYPSE_INTTEST_AWS_SECRETACCESSKEY`  - IAM secret access key

#### 3. UI Tests
- **Purpose**: End-to-end testing of the Blazor portal including AWS Cognito authentication
- **Environment**: Ubuntu container with Playwright browsers
- **Steps**:
  - Same setup as previous jobs
  - **Updates `appsettings.json` with production configuration from secrets**
  - Runs Playwright-based UI tests from `clypse.portal.UITests`
  - Tests user authentication and vault management workflows
  - Uploads test results as artifacts
- **Required Secrets**:
  - `CLYPSE_UITESTS_USERNAME`             - AWS Cognito test user username
  - `CLYPSE_UITESTS_PASSWORD`             - AWS Cognito test user password
  - `CLYPSE_UITESTS_PORTAL_APPSETTINGS`   - Complete JSON configuration for the portal

#### 4. Deploy
- **Purpose**: Publishes the Blazor WebAssembly app to AWS S3 and invalidates CloudFront cache
- **Trigger**: Only runs when manually triggered with the `deploy` input set to `true`
- **Environment**: Ubuntu latest
- **Steps**:
  - Sets up .NET 10.0 SDK and WASM tools
  - Restores dependencies
  - **Updates `appsettings.json` with production configuration from secrets**
  - Publishes Blazor WebAssembly in Release mode
  - Configures AWS credentials
  - Syncs published files to S3 bucket with public-read ACL
  - Invalidates CloudFront distribution to refresh cached content
- **Required Secrets**:
  - `CLYPSE_PUBLISH_AWS_BUCKETREGION`              - AWS region for production bucket
  - `CLYPSE_PUBLISH_AWS_BUCKETNAME`                - Production S3 bucket name
  - `CLYPSE_PUBLISH_AWS_ACCESSKEY`                 - AWS access key for deployment
  - `CLYPSE_PUBLISH_AWS_SECRETACCESSKEY`           - AWS secret access key for deployment
  - `CLYPSE_PUBLISH_AWS_CLOUDFRONTDISTRIBUTIONID`  - CloudFront distribution ID for cache invalidation
  - `CLYPSE_PUBLISH_APPSETTINGS`                   - Complete JSON configuration for the portal

### Required GitHub Secrets Summary

To run the complete CI/CD pipeline, configure the following secrets in your GitHub repository settings:

**Testing Secrets:**
- `CLYPSE_INTTEST_AWS_BUCKETREGION`              - AWS region for test bucket
- `CLYPSE_INTTEST_AWS_BUCKETNAME`                - Name of the S3 test bucket
- `CLYPSE_INTTEST_AWS_ACCESSKEY`                 - IAM access key with S3 permissions
- `CLYPSE_INTTEST_AWS_SECRETACCESSKEY`           - IAM secret access key
- `CLYPSE_UITESTS_USERNAME`                      - AWS Cognito test user username
- `CLYPSE_UITESTS_PASSWORD`                      - AWS Cognito test user password
- `CLYPSE_UITESTS_PORTAL_APPSETTINGS`            - Complete JSON configuration for the portal

**Deployment Secrets:**
- `CLYPSE_PUBLISH_AWS_BUCKETREGION`              - AWS region for production bucket
- `CLYPSE_PUBLISH_AWS_BUCKETNAME`                - Production S3 bucket name
- `CLYPSE_PUBLISH_AWS_ACCESSKEY`                 - AWS access key for deployment
- `CLYPSE_PUBLISH_AWS_SECRETACCESSKEY`           - AWS secret access key for deployment
- `CLYPSE_PUBLISH_AWS_CLOUDFRONTDISTRIBUTIONID`  - CloudFront distribution ID for cache invalidation
- `CLYPSE_PUBLISH_APPSETTINGS`                   - Complete JSON configuration for the portal

### Workflow Execution

- **Automatic**: The workflow runs on every push and pull request, executing unit, integration, and UI tests
- **Manual Deployment**: Use the "Run workflow" button in GitHub Actions and set the `deploy` checkbox to `true` to trigger production deployment after successful tests