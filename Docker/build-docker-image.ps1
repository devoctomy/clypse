#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build and push the Clypse CI/CD Docker image

.DESCRIPTION
    This script builds the custom Docker image for Clypse workflows and optionally pushes it to Docker Hub.

.PARAMETER DockerUsername
    Your Docker Hub username (required for pushing)

.PARAMETER ImageName
    The name of the Docker image (default: clypse-ci)

.PARAMETER Version
    The version tag for the image (default: latest)

.PARAMETER Push
    If specified, pushes the image to Docker Hub after building

.PARAMETER SkipBuild
    If specified, skips the build step (useful for just pushing an existing image)

.EXAMPLE
    .\build-docker-image.ps1 -DockerUsername myusername
    Builds the image as myusername/clypse-ci:latest

.EXAMPLE
    .\build-docker-image.ps1 -DockerUsername myusername -Version v1.0.0 -Push
    Builds and pushes the image with version tag v1.0.0
#>

param(
    [Parameter(Mandatory=$true, HelpMessage="Your Docker Hub username")]
    [string]$DockerUsername,
    
    [Parameter(Mandatory=$false)]
    [string]$ImageName = "clypse-ci",
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "latest",
    
    [Parameter(Mandatory=$false)]
    [switch]$Push,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$FullImageName = "${DockerUsername}/${ImageName}:${Version}"
$LatestImageName = "${DockerUsername}/${ImageName}:latest"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Clypse CI/CD Docker Image Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is available
Write-Host "Checking Docker availability..." -ForegroundColor Yellow
try {
    docker version | Out-Null
    Write-Host "[OK] Docker is available" -ForegroundColor Green
} catch {
    Write-Error "Docker is not available. Please install Docker Desktop and ensure it is running."
    exit 1
}

# Build the image
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Building Docker image: $FullImageName" -ForegroundColor Yellow
    Write-Host ""
    
    docker build -f ./Dockerfile -t $FullImageName ..
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker build failed!"
        exit 1
    }
    
    Write-Host ""
    Write-Host "[OK] Build successful!" -ForegroundColor Green
    
    # Tag as latest if version is not latest
    if ($Version -ne "latest") {
        Write-Host ""
        Write-Host "Tagging image as latest..." -ForegroundColor Yellow
        docker tag $FullImageName $LatestImageName
        Write-Host "[OK] Tagged as $LatestImageName" -ForegroundColor Green
    }
} else {
    Write-Host "Skipping build as requested." -ForegroundColor Yellow
}

# Push the image
if ($Push) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Pushing to Docker Hub" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Check if logged in
    Write-Host "Checking Docker Hub authentication..." -ForegroundColor Yellow
    $loginCheck = docker info 2>&1 | Select-String "Username"
    
    if (-not $loginCheck) {
        Write-Host "Not logged in to Docker Hub. Please login:" -ForegroundColor Yellow
        docker login
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Docker login failed!"
            exit 1
        }
    } else {
        Write-Host "[OK] Already logged in to Docker Hub" -ForegroundColor Green
    }
    
    # Push version tag
    Write-Host ""
    Write-Host "Pushing $FullImageName..." -ForegroundColor Yellow
    docker push $FullImageName
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to push $FullImageName"
        exit 1
    }
    
    Write-Host "[OK] Pushed $FullImageName" -ForegroundColor Green
    
    # Push latest tag if version is not latest
    if ($Version -ne "latest") {
        Write-Host ""
        Write-Host "Pushing $LatestImageName..." -ForegroundColor Yellow
        docker push $LatestImageName
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to push $LatestImageName"
            exit 1
        }
        
        Write-Host "[OK] Pushed $LatestImageName" -ForegroundColor Green
    }
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Image Name:     $FullImageName" -ForegroundColor White
if ($Version -ne "latest") {
    Write-Host "Also Tagged:    $LatestImageName" -ForegroundColor White
}
$buildStatus = if ($SkipBuild) { "Skipped" } else { "Completed" }
$pushStatus = if ($Push) { "Completed" } else { "Skipped" }
Write-Host "Build:          $buildStatus" -ForegroundColor White
Write-Host "Push:           $pushStatus" -ForegroundColor White
Write-Host ""

if (-not $Push) {
    Write-Host "To push this image to Docker Hub, run:" -ForegroundColor Yellow
    Write-Host "  .\build-docker-image.ps1 -DockerUsername $DockerUsername -Version $Version -Push" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "To use this image in your workflows, update the container image to:" -ForegroundColor Yellow
Write-Host "  image: $FullImageName" -ForegroundColor Cyan
Write-Host ""
Write-Host "[DONE] All operations completed successfully!" -ForegroundColor Green
