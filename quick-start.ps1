Write-Host "Welcome to the Clypse infrastructure setup wizard." -ForegroundColor Cyan
Write-Host "This script will provision AWS resources, build, publish, and deploy the Blazor WebAssembly portal." -ForegroundColor Cyan
Write-Host "Use the menu below to progress through each step." -ForegroundColor Cyan

$script:QuickStartRoot = if ($PSCommandPath) {
    Split-Path -Parent $PSCommandPath
}
elseif ($MyInvocation.MyCommand.Path) {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}
else {
    (Get-Location).Path
}

$script:AwsResourcePrefix = $null
$script:InitialUserEmail = $null
$script:AwsRegion = "eu-west-2"
$script:CognitoUserPoolId = $null
$script:CognitoUserPoolClientId = $null
$script:CognitoIdentityPoolId = $null
$script:PortalBucketName = $null
$script:ConfigFilePath = Join-Path -Path $script:QuickStartRoot -ChildPath "quick-start.settings.json"
$script:AwsResourcesOutputPath = Join-Path -Path $script:QuickStartRoot -ChildPath "quick-start.aws-resources.json"

function Load-QuickStartConfig {
    if (-not (Test-Path $script:ConfigFilePath)) {
        return
    }

    try {
        $raw = Get-Content -Path $script:ConfigFilePath -Raw
        if ([string]::IsNullOrWhiteSpace($raw)) { return }
        $config = $raw | ConvertFrom-Json
    }
    catch {
        Write-Host "Warning: Unable to read persisted configuration. $_" -ForegroundColor Yellow
        return
    }

    if ($config.AwsRegion) { $script:AwsRegion = $config.AwsRegion }
    if ($config.AwsResourcePrefix) { $script:AwsResourcePrefix = $config.AwsResourcePrefix }
    if ($config.InitialUserEmail) { $script:InitialUserEmail = $config.InitialUserEmail }
}

function Save-QuickStartConfig {
    $config = [ordered]@{
        AwsRegion = $script:AwsRegion
        AwsResourcePrefix = $script:AwsResourcePrefix
        InitialUserEmail = $script:InitialUserEmail
    }

    try {
        $config | ConvertTo-Json | Out-File -FilePath $script:ConfigFilePath -Encoding ascii
    }
    catch {
        Write-Host "Warning: Unable to persist configuration. $_" -ForegroundColor Yellow
    }
}

Load-QuickStartConfig

function Update-AwsRegionEnvironment {
    if ([string]::IsNullOrWhiteSpace($script:AwsRegion)) {
        Remove-Item env:AWS_REGION -ErrorAction SilentlyContinue
        Remove-Item env:AWS_DEFAULT_REGION -ErrorAction SilentlyContinue
        [Environment]::SetEnvironmentVariable('AWS_REGION', $null, 'User')
        [Environment]::SetEnvironmentVariable('AWS_DEFAULT_REGION', $null, 'User')
        return
    }

    $env:AWS_REGION = $script:AwsRegion
    $env:AWS_DEFAULT_REGION = $script:AwsRegion
    [Environment]::SetEnvironmentVariable('AWS_REGION', $script:AwsRegion, 'User')
    [Environment]::SetEnvironmentVariable('AWS_DEFAULT_REGION', $script:AwsRegion, 'User')
}

Update-AwsRegionEnvironment

function Test-AwsCredentialsPresent {
    return (-not [string]::IsNullOrWhiteSpace($env:AWS_ACCESS_KEY_ID)) -and (-not [string]::IsNullOrWhiteSpace($env:AWS_SECRET_ACCESS_KEY))
}

function Show-Menu {
    Write-Host "" # spacer for readability
    Write-Host "===== Main Menu =====" -ForegroundColor Yellow

    $regionSuffix = if ([string]::IsNullOrWhiteSpace($script:AwsRegion)) { "" } else { " (current: $($script:AwsRegion))" }
    Write-Host "1. Set AWS region$regionSuffix"

    $hasCreds = Test-AwsCredentialsPresent
    $credLabel = if ($hasCreds) { "Update" } else { "Set" }
    Write-Host "2. $credLabel AWS credentials"

    $hasPrefix = -not [string]::IsNullOrWhiteSpace($script:AwsResourcePrefix)
    $prefixLabel = if ($hasPrefix) { "Update" } else { "Set" }
    $prefixSuffix = if ($hasPrefix) { " (current: $($script:AwsResourcePrefix))" } else { "" }
    Write-Host "3. $prefixLabel AWS resource prefix$prefixSuffix"

    $hasEmail = -not [string]::IsNullOrWhiteSpace($script:InitialUserEmail)
    $emailLabel = if ($hasEmail) { "Update" } else { "Set" }
    $emailSuffix = if ($hasEmail) { " (current: $($script:InitialUserEmail))" } else { "" }
    Write-Host "4. $emailLabel first Cognito user email$emailSuffix"

    $cognitoStatus = if ($script:CognitoIdentityPoolId) { " (completed)" } else { "" }
    Write-Host "5. Setup AWS Cognito resources$cognitoStatus"

    $bucketStatus = if ($script:PortalBucketName) { " (bucket: $($script:PortalBucketName))" } else { "" }
    Write-Host "6. Configure portal S3 bucket$bucketStatus"

    Write-Host "7. Build and deploy portal to S3"

    Write-Host "Q. Quit"
}

function Set-AwsRegion {
    $raw = Read-Host "Enter AWS region (e.g., us-east-1)"
    if ($null -eq $raw) { $raw = "" }
    $candidate = $raw.Trim().ToLowerInvariant()
    if ($candidate -notmatch '^[a-z]{2}-[a-z]+-\d+$') {
        Write-Host "Invalid AWS region format. Please try again (e.g., eu-west-2)." -ForegroundColor Red
        return
    }

    $script:AwsRegion = $candidate
    Update-AwsRegionEnvironment
    Save-QuickStartConfig
    Write-Host "AWS region set to '$candidate'." -ForegroundColor Green
}

function Set-AwsCredentials {
    $accessKeyId = Read-Host "Enter AWS Access Key ID"

    $secureSecret = Read-Host "Enter AWS Secret Access Key" -AsSecureString
    $ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureSecret)
    try {
        $secretAccessKey = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    }

    if ([string]::IsNullOrWhiteSpace($accessKeyId) -or [string]::IsNullOrWhiteSpace($secretAccessKey)) {
        Write-Host "Credentials invalid. Please try again." -ForegroundColor Red
        return
    }

    $env:AWS_ACCESS_KEY_ID = $accessKeyId
    $env:AWS_SECRET_ACCESS_KEY = $secretAccessKey
    [Environment]::SetEnvironmentVariable('AWS_ACCESS_KEY_ID', $accessKeyId, 'User')
    [Environment]::SetEnvironmentVariable('AWS_SECRET_ACCESS_KEY', $secretAccessKey, 'User')
    Update-AwsRegionEnvironment

    Write-Host "AWS credential environment variables set." -ForegroundColor Green
}

function Set-AwsResourcePrefix {
    $raw = Read-Host "Enter AWS resource prefix (lowercase, no spaces)"
    if ($null -eq $raw) { $raw = "" }
    $normalized = ($raw -replace "\s", "").ToLowerInvariant()
    $normalized = $normalized -replace "[^a-z0-9-]", ""
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        Write-Host "Prefix invalid or empty. Please try again." -ForegroundColor Red
        return
    }
    $script:AwsResourcePrefix = $normalized
    Save-QuickStartConfig
    Write-Host "AWS resource prefix set to '$normalized'." -ForegroundColor Green
}

function Set-InitialUserEmail {
    $raw = Read-Host "Enter first Cognito user email"
    if ($null -eq $raw) { $raw = "" }
    $candidate = $raw.Trim()
    $emailPattern = '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$'
    if ($candidate -notmatch $emailPattern) {
        Write-Host "Invalid email address. Please try again." -ForegroundColor Red
        return
    }
    $script:InitialUserEmail = $candidate
    Save-QuickStartConfig
    Write-Host "Initial Cognito user email set to '$candidate'." -ForegroundColor Green
}

function Ensure-AwsCliAvailable {
    if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
        throw "AWS CLI is not installed or not available in PATH. Install it from https://aws.amazon.com/cli/ before continuing."
    }
}

function Set-AwsCredentialEnvironment {
    if (-not (Test-AwsCredentialsPresent)) {
        throw "AWS credential environment variables are not set. Use menu option 2 first."
    }

    Update-AwsRegionEnvironment
}

function Invoke-AwsCli {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [switch]$ExpectJson
    )

    $fullArgs = $Arguments + @('--region', $script:AwsRegion)
    if ($ExpectJson) {
        $fullArgs += @('--output', 'json')
    }

    $output = & aws @fullArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "AWS CLI command failed: $($output -join [Environment]::NewLine)"
    }

    if ($ExpectJson) {
        return $output | ConvertFrom-Json
    }

    return $output
}

function New-JsonTempFile {
    param([Parameter(Mandatory = $true)]$Object)

    $tempPath = [System.IO.Path]::GetTempFileName()
    $Object | ConvertTo-Json -Depth 10 | Out-File -FilePath $tempPath -Encoding ascii
    return $tempPath
}

function Write-AwsResourcesOutput {
    $config = [ordered]@{}
    if (Test-Path $script:AwsResourcesOutputPath) {
        try {
            $existingRaw = Get-Content -Path $script:AwsResourcesOutputPath -Raw
            if (-not [string]::IsNullOrWhiteSpace($existingRaw)) {
                $existing = $existingRaw | ConvertFrom-Json
                if ($existing) {
                    foreach ($prop in $existing.PSObject.Properties) {
                        $config[$prop.Name] = $prop.Value
                    }
                }
            }
        }
        catch {
            Write-Host "Warning: Unable to read existing AWS resource config. $_" -ForegroundColor Yellow
        }
    }

    if ($script:CognitoUserPoolId -and $script:CognitoUserPoolClientId -and $script:CognitoIdentityPoolId) {
        $config.AwsCognito = [ordered]@{
            UserPoolId = $script:CognitoUserPoolId
            UserPoolClientId = $script:CognitoUserPoolClientId
            Region = $script:AwsRegion
            IdentityPoolId = $script:CognitoIdentityPoolId
        }
    }

    if ($script:PortalBucketName) {
        $websiteHost = "http://$($script:PortalBucketName).s3-website-$($script:AwsRegion).amazonaws.com"
        $config.AwsS3 = [ordered]@{
            BucketName = $script:PortalBucketName
            Region = $script:AwsRegion
            WebsiteUrl = $websiteHost
        }
    }

    if ($config.Count -eq 0) {
        return
    }

    try {
        $config | ConvertTo-Json -Depth 4 | Out-File -FilePath $script:AwsResourcesOutputPath -Encoding ascii
        Write-Host "AWS resource configuration written to '$($script:AwsResourcesOutputPath)'." -ForegroundColor Green
    }
    catch {
        Write-Host "Warning: Unable to write AWS resource config file. $_" -ForegroundColor Yellow
    }
}

function Setup-PortalBucket {
    if ([string]::IsNullOrWhiteSpace($script:AwsResourcePrefix)) {
        throw "AWS resource prefix must be set before creating the portal bucket."
    }

    $bucketName = "$($script:AwsResourcePrefix).clypse"
    $script:PortalBucketName = $bucketName

    $bucketExists = $false
    try {
        Invoke-AwsCli -Arguments @('s3api', 'head-bucket', '--bucket', $bucketName) | Out-Null
        $bucketExists = $true
        Write-Host "Bucket '$bucketName' already exists. Reapplying configuration." -ForegroundColor Yellow
    }
    catch {
        $bucketExists = $false
    }

    if (-not $bucketExists) {
        Write-Host "Creating portal S3 bucket '$bucketName'..." -ForegroundColor Cyan
        $createArgs = @('s3api', 'create-bucket', '--bucket', $bucketName)
        if ($script:AwsRegion -ne 'us-east-1') {
            $createArgs += @('--create-bucket-configuration', "LocationConstraint=$($script:AwsRegion)")
        }
        Invoke-AwsCli -Arguments $createArgs | Out-Null
    }

    Write-Host "Enabling ACL support (BucketOwnerPreferred) for '$bucketName'..." -ForegroundColor Cyan
    $ownershipConfigPath = New-JsonTempFile -Object (@{ Rules = @(@{ ObjectOwnership = 'BucketOwnerPreferred' }) })
    try {
        Invoke-AwsCli -Arguments @('s3api', 'put-bucket-ownership-controls', '--bucket', $bucketName, '--ownership-controls', "file://$ownershipConfigPath") | Out-Null
    }
    finally {
        Remove-Item $ownershipConfigPath -ErrorAction SilentlyContinue
    }

    Write-Host "Disabling public access blocking for '$bucketName'..." -ForegroundColor Cyan
    Invoke-AwsCli -Arguments @('s3api', 'put-public-access-block', '--bucket', $bucketName, '--public-access-block-configuration', 'BlockPublicAcls=false,IgnorePublicAcls=false,BlockPublicPolicy=false,RestrictPublicBuckets=false') | Out-Null

    Write-Host "Applying bucket ACL grants (public read)..." -ForegroundColor Cyan
    $currentAcl = Invoke-AwsCli -Arguments @('s3api', 'get-bucket-acl', '--bucket', $bucketName) -ExpectJson
    $ownerId = $currentAcl.Owner.ID
    $ownerName = $currentAcl.Owner.DisplayName
    $ownerObject = [ordered]@{ ID = $ownerId }
    if ($ownerName) { $ownerObject.DisplayName = $ownerName }
    $ownerGrantee = [ordered]@{ Type = 'CanonicalUser'; ID = $ownerId }
    if ($ownerName) { $ownerGrantee.DisplayName = $ownerName }
    $aclPolicy = [ordered]@{
        Owner = $ownerObject
        Grants = @(
            @{ Grantee = $ownerGrantee; Permission = 'FULL_CONTROL' }
            @{ Grantee = @{ Type = 'Group'; URI = 'http://acs.amazonaws.com/groups/global/AllUsers' }; Permission = 'READ_ACP' }
        )
    }
    $aclPolicyPath = New-JsonTempFile -Object $aclPolicy
    try {
        Invoke-AwsCli -Arguments @('s3api', 'put-bucket-acl', '--bucket', $bucketName, '--access-control-policy', "file://$aclPolicyPath") | Out-Null
    }
    finally {
        Remove-Item $aclPolicyPath -ErrorAction SilentlyContinue
    }

    Write-Host "Configuring static website hosting..." -ForegroundColor Cyan
    $websiteConfigPath = New-JsonTempFile -Object (@{
            IndexDocument = @{ Suffix = 'index.html' }
            ErrorDocument = @{ Key = 'index.html' }
        })
    try {
        Invoke-AwsCli -Arguments @('s3api', 'put-bucket-website', '--bucket', $bucketName, '--website-configuration', "file://$websiteConfigPath") | Out-Null
    }
    finally {
        Remove-Item $websiteConfigPath -ErrorAction SilentlyContinue
    }

    Write-Host "Applying public-read bucket policy..." -ForegroundColor Cyan
    $policyPath = New-JsonTempFile -Object (@{
            Version = '2012-10-17'
            Statement = @(
                @{
                    Sid = 'PublicReadGetObject'
                    Effect = 'Allow'
                    Principal = '*'
                    Action = 's3:GetObject'
                    Resource = "arn:aws:s3:::$bucketName/*"
                }
            )
        })
    try {
        Invoke-AwsCli -Arguments @('s3api', 'put-bucket-policy', '--bucket', $bucketName, '--policy', "file://$policyPath") | Out-Null
    }
    finally {
        Remove-Item $policyPath -ErrorAction SilentlyContinue
    }

    $websiteEndpoint = "http://$bucketName.s3-website-$($script:AwsRegion).amazonaws.com"
    Write-Host "Portal bucket '$bucketName' configured. Website endpoint: $websiteEndpoint" -ForegroundColor Green
}

function Setup-Cognito {
    $missing = @()
    if (-not (Test-AwsCredentialsPresent)) {
        $missing += "AWS credentials"
    }
    if ([string]::IsNullOrWhiteSpace($script:AwsResourcePrefix)) {
        $missing += "AWS resource prefix"
    }
    if ([string]::IsNullOrWhiteSpace($script:InitialUserEmail)) {
        $missing += "First Cognito user email"
    }

    if ($missing.Count -gt 0) {
        Write-Host "Cannot continue. Missing: $($missing -join ', ')." -ForegroundColor Red
        return
    }

    try {
        Ensure-AwsCliAvailable
    }
    catch {
        Write-Host $_ -ForegroundColor Red
        return
    }

    try {
        Set-AwsCredentialEnvironment
    }
    catch {
        Write-Host $_ -ForegroundColor Red
        return
    }

    $poolName = "$($script:AwsResourcePrefix)-user-pool"
    $clientName = "$($script:AwsResourcePrefix)-portal-client"
    $identityPoolName = "$($script:AwsResourcePrefix)-identity-pool"
    $providerName = "cognito-idp.$($script:AwsRegion).amazonaws.com/"

    Write-Host "Creating Cognito user pool '$poolName'..." -ForegroundColor Cyan
    try {
        $userPoolResponse = Invoke-AwsCli -Arguments @('cognito-idp', 'create-user-pool', '--pool-name', $poolName, '--username-attributes', 'email', '--auto-verified-attributes', 'email') -ExpectJson
        if (-not $userPoolResponse.UserPool -or -not $userPoolResponse.UserPool.Id) {
            throw "Unexpected response when creating user pool."
        }
        $script:CognitoUserPoolId = $userPoolResponse.UserPool.Id
    }
    catch {
        Write-Host "Failed to create user pool: $_" -ForegroundColor Red
        return
    }

    Write-Host "Creating Cognito user pool client '$clientName'..." -ForegroundColor Cyan
    try {
        if ([string]::IsNullOrWhiteSpace($script:CognitoUserPoolId)) {
            throw "User pool id missing prior to client creation."
        }
        $tokenUnits = 'AccessToken=minutes,IdToken=minutes,RefreshToken=days'
        $userPoolClientResponse = Invoke-AwsCli -Arguments @('cognito-idp', 'create-user-pool-client', '--user-pool-id', $script:CognitoUserPoolId, '--client-name', $clientName, '--no-generate-secret', '--explicit-auth-flows', 'ALLOW_USER_PASSWORD_AUTH', 'ALLOW_REFRESH_TOKEN_AUTH', '--refresh-token-validity', '30', '--access-token-validity', '60', '--id-token-validity', '60', '--token-validity-units', $tokenUnits) -ExpectJson
        if (-not $userPoolClientResponse.UserPoolClient -or -not $userPoolClientResponse.UserPoolClient.ClientId) {
            throw "Unexpected response when creating user pool client."
        }
        $script:CognitoUserPoolClientId = $userPoolClientResponse.UserPoolClient.ClientId
    }
    catch {
        Write-Host "Failed to create user pool client: $_" -ForegroundColor Red
        return
    }

    Write-Host "Creating Cognito identity pool '$identityPoolName'..." -ForegroundColor Cyan
    $providerDescriptor = "ProviderName=$providerName$($script:CognitoUserPoolId),ClientId=$($script:CognitoUserPoolClientId)"
    try {
        $identityPool = Invoke-AwsCli -Arguments @('cognito-identity', 'create-identity-pool', '--identity-pool-name', $identityPoolName, '--no-allow-unauthenticated-identities', '--cognito-identity-providers', $providerDescriptor) -ExpectJson
        $script:CognitoIdentityPoolId = $identityPool.IdentityPoolId
    }
    catch {
        Write-Host "Failed to create identity pool: $_" -ForegroundColor Red
        return
    }

    Write-Host "Creating initial Cognito user '$($script:InitialUserEmail)' and sending invitation email..." -ForegroundColor Cyan
    try {
        Invoke-AwsCli -Arguments @('cognito-idp', 'admin-create-user', '--user-pool-id', $script:CognitoUserPoolId, '--username', $script:InitialUserEmail, '--user-attributes', "Name=email,Value=$($script:InitialUserEmail)", '--desired-delivery-mediums', 'EMAIL') -ExpectJson | Out-Null
    }
    catch {
        Write-Host "Failed to create the initial user: $_" -ForegroundColor Red
        return
    }

    Write-Host "AWS Cognito setup complete." -ForegroundColor Green
    Write-Host "User Pool ID: $($script:CognitoUserPoolId)"
    Write-Host "User Pool Client ID: $($script:CognitoUserPoolClientId)"
    Write-Host "Identity Pool ID: $($script:CognitoIdentityPoolId)"

    Write-AwsResourcesOutput
}

function Setup-PortalHosting {
    $missing = @()
    if (-not (Test-AwsCredentialsPresent)) { $missing += "AWS credentials" }
    if ([string]::IsNullOrWhiteSpace($script:AwsResourcePrefix)) { $missing += "AWS resource prefix" }

    if ($missing.Count -gt 0) {
        Write-Host "Cannot continue. Missing: $($missing -join ', ')." -ForegroundColor Red
        return
    }

    try {
        Ensure-AwsCliAvailable
    }
    catch {
        Write-Host $_ -ForegroundColor Red
        return
    }

    try {
        Set-AwsCredentialEnvironment
    }
    catch {
        Write-Host $_ -ForegroundColor Red
        return
    }

    try {
        Setup-PortalBucket
    }
    catch {
        Write-Host "Failed to configure portal bucket: $_" -ForegroundColor Red
        return
    }

    if ($script:PortalBucketName) {
        $websiteEndpoint = "http://$($script:PortalBucketName).s3-website-$($script:AwsRegion).amazonaws.com"
        Write-Host "Portal Bucket: $($script:PortalBucketName)" -ForegroundColor Green
        Write-Host "Website Endpoint: $websiteEndpoint" -ForegroundColor Green
    }

    Write-AwsResourcesOutput
}

function Publish-PortalSite {
    $missing = @()
    if (-not (Test-AwsCredentialsPresent)) { $missing += "AWS credentials" }
    if ([string]::IsNullOrWhiteSpace($script:PortalBucketName) -and -not [string]::IsNullOrWhiteSpace($script:AwsResourcePrefix)) {
        $script:PortalBucketName = "$($script:AwsResourcePrefix).clypse"
    }
    if ([string]::IsNullOrWhiteSpace($script:PortalBucketName)) { $missing += "Portal S3 bucket" }

    if ($missing.Count -gt 0) {
        Write-Host "Cannot continue. Missing: $($missing -join ', ')." -ForegroundColor Red
        return
    }

    try {
        Ensure-AwsCliAvailable
    }
    catch {
        Write-Host $_ -ForegroundColor Red
        return
    }

    try {
        Set-AwsCredentialEnvironment
    }
    catch {
        Write-Host $_ -ForegroundColor Red
        return
    }

    $scriptDir = $script:QuickStartRoot
    $projectPath = Join-Path -Path $scriptDir -ChildPath 'clypse.portal/clypse.portal.csproj'
    $publishOutput = Join-Path -Path $scriptDir -ChildPath 'clypse.portal/bin/Release/net10.0/publish/wwwroot'

    Write-Host "Building and publishing Blazor WebAssembly portal..." -ForegroundColor Cyan
    Push-Location $scriptDir
    try {
        dotnet restore $projectPath
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

        dotnet build $projectPath -c Release
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

        dotnet publish $projectPath -c Release -r browser-wasm --self-contained
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }
    }
    catch {
        Write-Host "Failed to build/publish portal: $_" -ForegroundColor Red
        return
    }
    finally {
        Pop-Location
    }

    if (-not (Test-Path $publishOutput)) {
        Write-Host "Publish output not found at '$publishOutput'." -ForegroundColor Red
        return
    }

    Write-Host "Uploading published assets to S3 bucket '$($script:PortalBucketName)'..." -ForegroundColor Cyan
    try {
        Invoke-AwsCli -Arguments @('s3', 'sync', $publishOutput, "s3://$($script:PortalBucketName)/", '--delete', '--acl', 'public-read') | Out-Null
    }
    catch {
        Write-Host "Failed to upload to S3: $_" -ForegroundColor Red
        return
    }

    Write-Host "Portal deployment complete." -ForegroundColor Green
}

while ($true) {
    Show-Menu
    $choice = Read-Host "Select an option"
    if ($null -eq $choice) { $choice = "" }
    switch ($choice.ToUpperInvariant()) {
        "1" { Set-AwsRegion }
        "2" { Set-AwsCredentials }
        "3" { Set-AwsResourcePrefix }
        "4" { Set-InitialUserEmail }
        "5" { Setup-Cognito }
        "6" { Setup-PortalHosting }
        "7" { Publish-PortalSite }
        "Q" { Write-Host "Exiting setup wizard."; return }
        default { Write-Host "Invalid selection. Please try again." -ForegroundColor Red }
    }
}
