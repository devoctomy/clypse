Write-Host "Welcome to the Clypse infrastructure setup wizard." -ForegroundColor Cyan
Write-Host "This script will provision AWS resources, build, publish, and deploy the Blazor WebAssembly portal." -ForegroundColor Cyan
Write-Host "Use the menu below to progress through each step." -ForegroundColor Cyan

$script:AwsResourcePrefix = $null
$script:InitialUserEmail = $null
$script:AwsRegion = "eu-west-2"
$script:CognitoUserPoolId = $null
$script:CognitoUserPoolClientId = $null
$script:CognitoIdentityPoolId = $null
$script:ConfigFilePath = Join-Path -Path (Split-Path -Parent $MyInvocation.MyCommand.Path) -ChildPath "quick-start.settings.json"
$script:AwsResourcesOutputPath = Join-Path -Path (Split-Path -Parent $MyInvocation.MyCommand.Path) -ChildPath "quick-start.aws-resources.json"

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

function Write-AwsResourcesOutput {
    if ([string]::IsNullOrWhiteSpace($script:CognitoUserPoolId) -or
        [string]::IsNullOrWhiteSpace($script:CognitoUserPoolClientId) -or
        [string]::IsNullOrWhiteSpace($script:CognitoIdentityPoolId)) {
        return
    }

    $config = [ordered]@{
        AwsCognito = [ordered]@{
            UserPoolId = $script:CognitoUserPoolId
            UserPoolClientId = $script:CognitoUserPoolClientId
            Region = $script:AwsRegion
            IdentityPoolId = $script:CognitoIdentityPoolId
        }
    }

    try {
        $config | ConvertTo-Json -Depth 4 | Out-File -FilePath $script:AwsResourcesOutputPath -Encoding ascii
        Write-Host "AWS resource configuration written to '$($script:AwsResourcesOutputPath)'." -ForegroundColor Green
    }
    catch {
        Write-Host "Warning: Unable to write AWS resource config file. $_" -ForegroundColor Yellow
    }
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
        "Q" { Write-Host "Exiting setup wizard."; return }
        default { Write-Host "Invalid selection. Please try again." -ForegroundColor Red }
    }
}
