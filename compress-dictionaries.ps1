# PowerShell script to compress dictionary files for clypse.portal
# This script compresses knownpasswords.txt using gzip and stores it in the portal's wwwroot data directory

param(
    [string]$ProjectRoot = $PSScriptRoot
)

# Define paths
$sourceFile = Join-Path $ProjectRoot "clypse.core\Data\Dictionaries\weakknownpasswords.txt"
$targetDir = Join-Path $ProjectRoot "clypse.portal\wwwroot\data\dictionaries"
$targetFile = Join-Path $targetDir "weakknownpasswords.txt.gz"

Write-Host "Compressing dictionary files for clypse.portal..." -ForegroundColor Green

# Check if source file exists
if (-not (Test-Path $sourceFile)) {
    Write-Error "Source file not found: $sourceFile"
    exit 1
}

# Create target directory if it doesn't exist
if (-not (Test-Path $targetDir)) {
    Write-Host "Creating target directory: $targetDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

# Check if compressed file already exists
if (Test-Path $targetFile) {
    Write-Host "Compressed file already exists: $targetFile" -ForegroundColor Yellow
    Write-Host "Skipping compression (file already up to date)" -ForegroundColor Yellow
    exit 0
}

# Compress the file using .NET's GZipStream
try {
    Write-Host "Compressing $sourceFile to $targetFile" -ForegroundColor Yellow
    
    # Read the source file
    $sourceBytes = [System.IO.File]::ReadAllBytes($sourceFile)
    
    # Create the compressed file using GZipStream
    $fileStream = [System.IO.File]::Create($targetFile)
    $gzipStream = New-Object System.IO.Compression.GZipStream($fileStream, [System.IO.Compression.CompressionMode]::Compress)
    
    $gzipStream.Write($sourceBytes, 0, $sourceBytes.Length)
    $gzipStream.Close()
    $fileStream.Close()
    
    Write-Host "Successfully compressed dictionary file!" -ForegroundColor Green
    
    # Display file sizes for verification
    $originalSize = (Get-Item $sourceFile).Length
    $compressedSize = (Get-Item $targetFile).Length
    $compressionRatio = [math]::Round((($originalSize - $compressedSize) / $originalSize) * 100, 2)
    
    Write-Host "Original size: $originalSize bytes" -ForegroundColor Cyan
    Write-Host "Compressed size: $compressedSize bytes" -ForegroundColor Cyan
    Write-Host "Compression ratio: $compressionRatio%" -ForegroundColor Cyan
}
catch {
    Write-Error "Failed to compress file: $($_.Exception.Message)"
    exit 1
}

Write-Host "Dictionary compression completed successfully!" -ForegroundColor Green
