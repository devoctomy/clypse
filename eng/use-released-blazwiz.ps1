<#
.SYNOPSIS
Makes clypse use a released version of blazwiz from nuget.org, in place of a build packed locally from source.

.DESCRIPTION
1. Sets BlazwizVersion in Directory.Packages.props to the version.
2. Removes the local builds from .blazwiz/packages (eng/use-local-blazwiz.ps1), so nothing restores from them by
   mistake. The empty folder stays: nuget.config lists it as a package source.
3. Restores clypse, which fails if the version is not on nuget.org.

The CI, deploy and upgrade workflows only work with a released version (eng/check-blazwiz-version.ps1). To work against
the blazwiz source again, run eng/use-local-blazwiz.ps1.

.EXAMPLE
./eng/use-released-blazwiz.ps1 -Version 0.5.0
#>
[CmdletBinding()]
param(
    # The released version, e.g. 0.5.0, or a beta such as 0.5.0-beta.12.
    [Parameter(Mandatory)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

if ($Version -notmatch '^\d+\.\d+\.\d+(-beta\.\d+)?$') {
    throw "'$Version' is not a released blazwiz version (MAJOR.MINOR.PATCH, or MAJOR.MINOR.PATCH-beta.N)."
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$packagesProps = Join-Path $repositoryRoot 'Directory.Packages.props'
$feed = Join-Path $repositoryRoot '.blazwiz/packages'

$props = Get-Content $packagesProps -Raw
if ($props -notmatch '<BlazwizVersion>[^<]*</BlazwizVersion>') {
    throw "No BlazwizVersion in $packagesProps."
}

$updated = [regex]::Replace($props, '<BlazwizVersion>[^<]*</BlazwizVersion>', "<BlazwizVersion>$Version</BlazwizVersion>")
[System.IO.File]::WriteAllText($packagesProps, $updated, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "BlazwizVersion is now $Version"

New-Item -ItemType Directory -Force -Path $feed | Out-Null
Get-ChildItem -Path $feed -Filter '*.nupkg' -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -Path $feed -Filter '*.snupkg' -ErrorAction SilentlyContinue | Remove-Item -Force

& dotnet restore (Join-Path $repositoryRoot 'Clypse.slnx') --nologo -v q
if ($LASTEXITCODE -ne 0) {
    throw "Restoring clypse failed. Is blazwiz $Version on nuget.org?"
}

Write-Host "clypse now uses blazwiz $Version from nuget.org."
