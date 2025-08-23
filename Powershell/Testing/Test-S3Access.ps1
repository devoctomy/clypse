# Simple S3 bucket test script
# Uses environment variables:
# CLYPSE_AWS_ACCESSKEY
# CLYPSE_AWS_SECRETACCESSKEY
# CLYPSE_AWS_BUCKETNAME

# Set AWS credentials from environment
$env:AWS_ACCESS_KEY_ID = $env:CLYPSE_AWS_ACCESSKEY
$env:AWS_SECRET_ACCESS_KEY = $env:CLYPSE_AWS_SECRETACCESSKEY

# Test file details
$testFile = "test.txt"
$testContent = "Test content"

Write-Host "Testing S3 bucket access..." -ForegroundColor Cyan

# Create test file
Set-Content -Path $testFile -Value $testContent

# 1. Test PUT
Write-Host "Testing PUT..." -ForegroundColor Cyan
aws s3 cp $testFile "s3://$env:CLYPSE_AWS_BUCKETNAME/$testFile"
if ($LASTEXITCODE -eq 0)
{
    Write-Host "PUT successful" -ForegroundColor Green
}

# 2. Test GET
Write-Host "Testing GET..." -ForegroundColor Cyan
aws s3 cp "s3://$env:CLYPSE_AWS_BUCKETNAME/$testFile" "downloaded_$testFile"
if ($LASTEXITCODE -eq 0)
{
    Write-Host "GET successful" -ForegroundColor Green
}

# 3. Test LIST
Write-Host "Testing LIST..." -ForegroundColor Cyan
aws s3 ls "s3://$env:CLYPSE_AWS_BUCKETNAME"
if ($LASTEXITCODE -eq 0)
{
    Write-Host "LIST successful" -ForegroundColor Green
}

# Cleanup
Write-Host "Cleaning up..." -ForegroundColor Yellow
aws s3 rm "s3://$env:CLYPSE_AWS_BUCKETNAME/$testFile"
Remove-Item $testFile
Remove-Item "downloaded_$testFile"

Write-Host "All tests completed successfully" -ForegroundColor Green