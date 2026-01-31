#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates PNG diagrams from PlantUML files and creates a README.md

.DESCRIPTION
    This script:
    1. Downloads PlantUML JAR if not present
    2. Converts all .puml files to PNG images in the png/ subfolder
    3. Generates a README.md file displaying all diagrams

.EXAMPLE
    .\Generate-Diagrams.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$pngDir = Join-Path $scriptDir "png"
$plantumlJar = Join-Path $scriptDir "plantuml.jar"
$plantumlUrl = "https://github.com/plantuml/plantuml/releases/download/v1.2024.8/plantuml-1.2024.8.jar"

# Ensure png directory exists
if (-not (Test-Path $pngDir)) {
    New-Item -ItemType Directory -Path $pngDir | Out-Null
    Write-Host "Created png directory: $pngDir" -ForegroundColor Green
}

# Download PlantUML if not present
if (-not (Test-Path $plantumlJar)) {
    Write-Host "Downloading PlantUML..." -ForegroundColor Yellow
    try {
        Invoke-WebRequest -Uri $plantumlUrl -OutFile $plantumlJar
        Write-Host "PlantUML downloaded successfully" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to download PlantUML: $_"
        exit 1
    }
}

# Check if Java is available
$javaCommand = Get-Command java -ErrorAction SilentlyContinue
if (-not $javaCommand) {
    Write-Error "Java is not installed or not in PATH. Please install Java to run PlantUML."
    exit 1
}
Write-Host "Using Java from: $($javaCommand.Source)" -ForegroundColor Cyan

# Find all .puml files
$pumlFiles = Get-ChildItem -Path $scriptDir -Filter "*.puml" | Sort-Object Name

if ($pumlFiles.Count -eq 0) {
    Write-Warning "No .puml files found in $scriptDir"
    exit 0
}

Write-Host "`nFound $($pumlFiles.Count) PlantUML file(s)" -ForegroundColor Cyan

# Generate PNG for each .puml file
foreach ($file in $pumlFiles) {
    Write-Host "Generating PNG for: $($file.Name)" -ForegroundColor Yellow
    
    $outputFile = Join-Path $pngDir "$($file.BaseName).png"
    
    # Run PlantUML
    $result = java -jar $plantumlJar -tpng -o $pngDir $file.FullName 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Generated: $($file.BaseName).png" -ForegroundColor Green
    }
    else {
        Write-Warning "  Failed to generate: $($file.BaseName).png"
        Write-Host $result -ForegroundColor Red
    }
}

# Generate README.md
Write-Host "`nGenerating README.md..." -ForegroundColor Yellow

$readmeLines = @()
$readmeLines += "# Architecture Diagrams"
$readmeLines += ""

foreach ($file in $pumlFiles) {
    $baseName = $file.BaseName
    # Convert kebab-case to Title Case
    $title = ($baseName -split '-' | ForEach-Object { 
        $_.Substring(0,1).ToUpper() + $_.Substring(1) 
    }) -join ' '
    
    $pngPath = "png/$baseName.png"
    
    $readmeLines += "## $title"
    $readmeLines += ""
    $readmeLines += "![$title]($pngPath)"
    $readmeLines += ""
}

$readmeContent = $readmeLines -join "`n"

$readmePath = Join-Path $scriptDir "README.md"
Set-Content -Path $readmePath -Value $readmeContent -Encoding UTF8
Write-Host "  README.md generated successfully" -ForegroundColor Green

Write-Host "`nAll done!" -ForegroundColor Green
Write-Host "PNG files: $pngDir" -ForegroundColor Cyan
Write-Host "README: $readmePath" -ForegroundColor Cyan
