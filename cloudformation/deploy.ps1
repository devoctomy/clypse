<#
.SYNOPSIS
Deploys clypse to AWS: the blazwiz base stack, this app's own stack (app.yaml), the portal and a test user.

.DESCRIPTION
Runs deploy.ps1 from the blazwiz base stack released with the framework (Devoctomy.Blazwiz.Aws, at the app's
BlazwizVersion), for this app. Takes the same parameters, e.g. -Region, -Environment, -TestUserEmail, -SkipPortal, -Force; see that script's help
(Get-Help ./cloudformation/deploy.ps1 -Full, after finding it with ./cloudformation/base-stack.ps1).

If Resources/environments/<Environment>/wwwroot exists, the portal is published with its files (icons, logo,
css/theme.css) in place of its own; otherwise with the default ones. The portal project does the swap, from the
ClypseEnvironment variable set here.
#>
param(
    # Short lowercase app name (letters and numbers), used to name resources.
    [string]$AppName = 'clypse',

    # Environment name, e.g. dev, test, prod. Passed on to the base stack's deploy.ps1 (same default).
    [string]$Environment = 'dev'
)

$ErrorActionPreference = 'Stop'

# Two-step verification is each user's choice: they turn it on, or not, in their Profile. Deploy with -Mfa optional
# every time, so the stack never keeps a different value from an earlier deploy.
if ($args -contains '-Mfa') {
    throw 'clypse leaves two-step verification to each user: deploy without -Mfa (it is always optional).'
}

$base = & (Join-Path $PSScriptRoot 'base-stack.ps1')
$previousClypseEnvironment = $env:ClypseEnvironment
$env:ClypseEnvironment = $Environment
try {
    & (Join-Path $base 'deploy.ps1') -AppName $AppName -Environment $Environment -AppRoot (Split-Path -Parent $PSScriptRoot) -PortalProject 'Clypse.Portal/Clypse.Portal.csproj' -AppStackTemplate (Join-Path $PSScriptRoot 'app.yaml') -Mfa optional @args
}
finally {
    $env:ClypseEnvironment = $previousClypseEnvironment
}
