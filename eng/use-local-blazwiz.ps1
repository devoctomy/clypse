<#
.SYNOPSIS
Builds the blazwiz packages from source and makes clypse use them.

.DESCRIPTION
While clypse is being built, blazwiz changes are not released to nuget.org: clypse uses packages packed locally from a
blazwiz working tree instead. This script:
  1. Packs every Devoctomy.Blazwiz.* package from the blazwiz source into .blazwiz/packages (git ignored), with a
     version unique to this run (<VersionPrefix>-local.<timestamp>). A new version every time means the NuGet cache
     never serves an older build.
  2. Sets BlazwizVersion in Directory.Packages.props to that version. nuget.config adds .blazwiz/packages as a package
     source, next to nuget.org.
  3. Removes older local builds from .blazwiz/packages and from the NuGet cache.
  4. Restores the solution.
Run it again after every blazwiz change clypse needs. Commit the BlazwizVersion change with the clypse change that
needs it.

.PARAMETER BlazwizPath
The blazwiz working tree (the folder holding blazwiz.slnx). Defaults to the BLAZWIZ_SOURCE environment variable.

.EXAMPLE
./eng/use-local-blazwiz.ps1 -BlazwizPath ../blazwiz

.EXAMPLE
$env:BLAZWIZ_SOURCE = 'D:\src\blazwiz'; ./eng/use-local-blazwiz.ps1
#>
[CmdletBinding()]
param(
    [string]$BlazwizPath = $env:BLAZWIZ_SOURCE
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($BlazwizPath)) {
    throw 'Give the blazwiz working tree with -BlazwizPath, or set BLAZWIZ_SOURCE.'
}

$blazwiz = (Resolve-Path $BlazwizPath).Path
$solution = Join-Path $blazwiz 'blazwiz.slnx'
if (-not (Test-Path $solution)) {
    throw "No blazwiz.slnx in '$blazwiz'."
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$feed = Join-Path $repositoryRoot '.blazwiz/packages'
$packagesProps = Join-Path $repositoryRoot 'Directory.Packages.props'

$versionPrefix = ([xml](Get-Content (Join-Path $blazwiz 'Directory.Build.props'))).Project.PropertyGroup.VersionPrefix |
    Where-Object { $_ } | Select-Object -First 1
if (-not $versionPrefix) {
    throw "No VersionPrefix in blazwiz's Directory.Build.props."
}

$versionSuffix = 'local.' + (Get-Date).ToUniversalTime().ToString('yyyyMMddHHmmss')
$version = "$versionPrefix-$versionSuffix"

Write-Host "Packing blazwiz $version from $blazwiz"
New-Item -ItemType Directory -Force -Path $feed | Out-Null
Get-ChildItem -Path $feed -Filter '*.nupkg' -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -Path $feed -Filter '*.snupkg' -ErrorAction SilentlyContinue | Remove-Item -Force

$env:MSBUILDDISABLENODEREUSE = '1'
& dotnet pack $solution -c Release -o $feed --nologo -v q -m:1 "-p:VersionSuffix=$versionSuffix"
if ($LASTEXITCODE -ne 0) {
    throw 'Packing blazwiz failed.'
}

$packed = Get-ChildItem -Path $feed -Filter "Devoctomy.Blazwiz.*.$version.nupkg"
if ($packed.Count -eq 0) {
    throw "No Devoctomy.Blazwiz packages with version $version were packed."
}

Write-Host "Packed $($packed.Count) packages"

$props = Get-Content $packagesProps -Raw
$updated = [regex]::Replace($props, '<BlazwizVersion>[^<]*</BlazwizVersion>', "<BlazwizVersion>$version</BlazwizVersion>")
if ($updated -eq $props -and $props -notmatch [regex]::Escape("<BlazwizVersion>$version</BlazwizVersion>")) {
    throw "No BlazwizVersion in $packagesProps."
}

[System.IO.File]::WriteAllText($packagesProps, $updated, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "BlazwizVersion is now $version"

# Older local builds are never used again: keep the NuGet cache from filling up with them.
$cache = (& dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', ''
if ($cache -and (Test-Path $cache)) {
    Get-ChildItem -Path $cache -Directory -Filter 'devoctomy.blazwiz.*' | ForEach-Object {
        Get-ChildItem -Path $_.FullName -Directory -Filter '*-local.*' |
            Where-Object { $_.Name -ne $version.ToLowerInvariant() } |
            Remove-Item -Recurse -Force
    }
}

& dotnet restore (Join-Path $repositoryRoot 'Clypse.slnx') --nologo -v q
if ($LASTEXITCODE -ne 0) {
    throw 'Restoring clypse failed.'
}

Write-Host 'clypse now uses the local blazwiz build.'
