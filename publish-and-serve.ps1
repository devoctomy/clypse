# PowerShell script to publish clypse.portal for browser-wasm and serve it locally
# This script builds the project for WebAssembly and serves it on HTTPS
#
# Usage examples:
#   .\publish-and-serve.ps1                              # Default: Release build on port 7153
#   .\publish-and-serve.ps1 -Clean                       # Clean obj/bin folders first
#   .\publish-and-serve.ps1 -Configuration Debug         # Use Debug configuration
#   .\publish-and-serve.ps1 -Port 8080                   # Use different port
#   .\publish-and-serve.ps1 -Clean -Configuration Debug -Port 8080  # All options combined

param(
    [string]$Configuration = "Release",
    [int]$Port = 7153,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

Write-Host "Publishing clypse.portal for browser-wasm..." -ForegroundColor Green

# Navigate to project root
$projectRoot = $PSScriptRoot
Set-Location $projectRoot

try {
    # Clean obj and bin folders if requested
    if ($Clean) {
        Write-Host "Cleaning obj and bin folders..." -ForegroundColor Yellow
        
        # Find all obj and bin folders recursively
        $foldersToDelete = Get-ChildItem -Path $projectRoot -Recurse -Directory | Where-Object { $_.Name -eq "obj" -or $_.Name -eq "bin" }
        
        if ($foldersToDelete.Count -eq 0) {
            Write-Host "  No obj or bin folders found to clean." -ForegroundColor Cyan
        } else {
            foreach ($folder in $foldersToDelete) {
                Write-Host "  Deleting: $($folder.FullName)" -ForegroundColor Cyan
                Remove-Item -Path $folder.FullName -Recurse -Force
            }
            Write-Host "  Deleted $($foldersToDelete.Count) folders." -ForegroundColor Green
        }
        
        Write-Host "Clean completed!" -ForegroundColor Green
    }

    # Clean previous publish output
    $publishPath = ".\clypse.portal\bin\$Configuration\net8.0\publish"
    if (Test-Path $publishPath) {
        Write-Host "Cleaning previous publish output..." -ForegroundColor Yellow
        Remove-Item -Path $publishPath -Recurse -Force
    }

    # Publish the project
    Write-Host "Running dotnet publish..." -ForegroundColor Yellow
    dotnet publish .\clypse.portal\clypse.portal.csproj -c $Configuration -r browser-wasm --self-contained

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    Write-Host "Publish completed successfully!" -ForegroundColor Green

    # Check if dotnet-serve is installed
    Write-Host "Checking for dotnet-serve tool..." -ForegroundColor Yellow
    $serveInstalled = $false
    try {
        dotnet tool list -g | Select-String "dotnet-serve" | Out-Null
        $serveInstalled = $true
        Write-Host "dotnet-serve is already installed" -ForegroundColor Green
    }
    catch {
        Write-Host "dotnet-serve not found in global tools" -ForegroundColor Yellow
    }

    if (-not $serveInstalled) {
        Write-Host "Installing dotnet-serve..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-serve
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to install dotnet-serve"
            exit $LASTEXITCODE
        }
    }

    # Serve the application
    $wwwrootPath = ".\clypse.portal\bin\$Configuration\net8.0\publish\wwwroot"
    Write-Host "Starting server on https://localhost:$Port..." -ForegroundColor Green
    Write-Host "Serving from: $wwwrootPath" -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow

    dotnet-serve -d $wwwrootPath -p $Port --tls
}
catch {
    Write-Error "Script failed: $($_.Exception.Message)"
    exit 1
}
