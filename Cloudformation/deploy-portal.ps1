param(
    [Parameter(Mandatory=$true)][string]$StackName,
    [Parameter(Mandatory=$true)][string]$TemplatePath = "./Cloudformation/clypse-aws-setup.yaml",
    [Parameter(Mandatory=$true)][string]$ResourcePrefix,
    [Parameter(Mandatory=$true)][string]$Region,
    [Parameter(Mandatory=$true)][string]$InitialUserEmail,
    [Parameter(Mandatory=$true)][string]$InitialUserTempPassword,
    [string]$CloudFrontAlias = "",
    [string]$CertificateArn = "",
    [string]$PortalProjectPath = "./clypse.portal",
    [string]$PublishOutput = "./publish",
    [string]$AppSettingsTemplate = "./Data/appsettings.json"
)

$ErrorActionPreference = 'Stop'

function Write-Info($message) {
    Write-Host "[info] $message" -ForegroundColor Cyan
}

# 1. Deploy/Update stack
Write-Info "Deploying CloudFormation stack $StackName"
aws cloudformation deploy `
    --stack-name $StackName `
    --template-file $TemplatePath `
    --capabilities CAPABILITY_IAM CAPABILITY_NAMED_IAM `
    --region $Region `
    --parameter-overrides `
        ResourcePrefix=$ResourcePrefix `
        AwsRegion=$Region `
        CloudFrontAlias=$CloudFrontAlias `
        CertificateArn=$CertificateArn `
        InitialUserEmail=$InitialUserEmail `
        InitialUserTempPassword=$InitialUserTempPassword

Write-Info "Fetching stack outputs"
$stack = aws cloudformation describe-stacks --stack-name $StackName --region $Region | ConvertFrom-Json
$outputs = @{}
foreach ($o in $stack.Stacks[0].Outputs) { $outputs[$o.OutputKey] = $o.OutputValue }

$portalBucket = $outputs['PortalBucketName']
$dataBucket   = $outputs['DataBucketName']
$userPoolId   = $outputs['UserPoolId']
$userPoolClientId = $outputs['UserPoolClientId']
$identityPoolId   = $outputs['IdentityPoolId']
$cloudFrontDomain = $outputs['CloudFrontDomain']

Write-Info "Portal bucket: $portalBucket"
Write-Info "Data bucket: $dataBucket"
Write-Info "CloudFront domain: $cloudFrontDomain"

# 2. Build WebAssembly portal
Write-Info "Cleaning publish output at $PublishOutput"
if (Test-Path $PublishOutput) { Remove-Item -Recurse -Force $PublishOutput }

Write-Info "Publishing Blazor WebAssembly app"
dotnet publish $PortalProjectPath -c Release -o $PublishOutput

# 3. Configure appsettings.json
Write-Info "Configuring appsettings.json"
$targetAppSettings = Join-Path $PublishOutput 'appsettings.json'
if (Test-Path $targetAppSettings) { Remove-Item $targetAppSettings -Force }

if (-not (Test-Path $AppSettingsTemplate)) { throw "Template appsettings not found: $AppSettingsTemplate" }

$app = Get-Content $AppSettingsTemplate -Raw | ConvertFrom-Json

$app.AwsS3.BucketName = $dataBucket
$app.AwsS3.Region = $Region
$app.AwsCognito.UserPoolId = $userPoolId
$app.AwsCognito.UserPoolClientId = $userPoolClientId
$app.AwsCognito.IdentityPoolId = $identityPoolId
$app.AwsCognito.Region = $Region

$app | ConvertTo-Json -Depth 20 | Out-File -Encoding UTF8 $targetAppSettings

# 4. Upload to S3 portal bucket
Write-Info "Uploading portal to s3://$portalBucket"
aws s3 sync $PublishOutput "s3://$portalBucket" --delete --region $Region

Write-Info "Deployment complete"
