<#
.SYNOPSIS
Stops with an error if clypse is set to a blazwiz build packed locally (eng/use-local-blazwiz.ps1) rather than a
released version.

.DESCRIPTION
Local builds are versioned <prefix>-local.<timestamp> and only exist on the machine that packed them, so a build, test
run or deployment anywhere else (GitHub Actions) cannot restore them. Run by the CI, deploy and upgrade workflows
before anything else.
#>
$ErrorActionPreference = 'Stop'

$props = Join-Path (Split-Path -Parent $PSScriptRoot) 'Directory.Packages.props'
$version = ([xml](Get-Content $props)).Project.PropertyGroup.BlazwizVersion | Where-Object { $_ } | Select-Object -First 1
if (-not $version) {
    throw "No BlazwizVersion in $props."
}

if ($version -match '-local\.') {
    throw "BlazwizVersion is $version, a blazwiz build packed locally from source, which only exists on the machine that packed it. Set it to a released version of the Devoctomy.Blazwiz packages on nuget.org."
}

Write-Host "BlazwizVersion is $version."
