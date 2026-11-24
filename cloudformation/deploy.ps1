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

A portal run on this machine (https://localhost:7090, http://localhost:5090) may call the environment's data bucket
only when -AllowLocalhost is given. It is said on every deploy, whatever the environment is called: without the switch,
a deploy takes the local addresses off again.
#>
param(
    # Short lowercase app name (letters and numbers), used to name resources.
    [string]$AppName = 'clypse',

    # Environment name, e.g. dev, test, prod. Passed on to the base stack's deploy.ps1 (same default).
    [string]$Environment = 'dev',

    # Lets a portal run on this machine call the environment's data bucket (its CORS list), for running locally against
    # the environment. Off unless given.
    [switch]$AllowLocalhost
)

$ErrorActionPreference = 'Stop'

# Which local addresses may call the data bucket is clypse's choice, by -AllowLocalhost. Left to the base stack's
# script, every environment not named "prod" would allow them, and the stack would keep whatever it was first given.
if ($args -contains '-LocalDevOrigins') {
    throw 'clypse decides the local addresses itself: deploy with -AllowLocalhost, or without it, not -LocalDevOrigins.'
}

# Typed, and never the result of an "if": an empty array that comes out of a statement is null, and the base stack's
# script takes null as "not given" and uses its own default.
[string[]]$localDevOrigins = @()
if ($AllowLocalhost) {
    $localDevOrigins = @('https://localhost:7090', 'http://localhost:5090')
}

# Two-step verification is each user's choice: they turn it on, or not, in their Profile. Deploy with -Mfa optional
# every time, so the stack never keeps a different value from an earlier deploy.
if ($args -contains '-Mfa') {
    throw 'clypse leaves two-step verification to each user: deploy without -Mfa (it is always optional).'
}

$base = & (Join-Path $PSScriptRoot 'base-stack.ps1')
$previousClypseEnvironment = $env:ClypseEnvironment
$env:ClypseEnvironment = $Environment
try {
    & (Join-Path $base 'deploy.ps1') -AppName $AppName -Environment $Environment -AppRoot (Split-Path -Parent $PSScriptRoot) -PortalProject 'Clypse.Portal/Clypse.Portal.csproj' -AppStackTemplate (Join-Path $PSScriptRoot 'app.yaml') -Mfa optional -LocalDevOrigins $localDevOrigins @args
}
finally {
    $env:ClypseEnvironment = $previousClypseEnvironment
}
