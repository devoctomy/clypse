# S3 Access Testing

This folder contains PowerShell scripts for testing AWS S3 access.

## Test-S3Access.ps1

A comprehensive script that tests basic S3 operations using AWS CLI.

### Prerequisites

1. **AWS CLI installed**: Download from [AWS CLI Installation Guide](https://aws.amazon.com/cli/)
2. **Environment Variables**: Set the following environment variables:
   - `CLYPSE_AWS_ACCESSKEY` - Your AWS Access Key ID
   - `CLYPSE_AWS_SECRETACCESSKEY` - Your AWS Secret Access Key
   - `CLYPSE_AWS_BUCKETNAME` - Your S3 bucket name

### Usage

```powershell
# Basic usage - uses CLYPSE_AWS_BUCKETNAME environment variable
.\Test-S3Access.ps1

# Override bucket name with parameter
.\Test-S3Access.ps1 -BucketName "your-bucket-name"

# Custom test object name and content
.\Test-S3Access.ps1 -TestObjectKey "my-test-file.txt" -TestContent "Custom test content"

# Override bucket and customize test
.\Test-S3Access.ps1 -BucketName "your-bucket-name" -TestObjectKey "my-test-file.txt" -TestContent "Custom test content"
```

### Environment Setup Example

```powershell
# Set environment variables (replace with your actual credentials)
$env:CLYPSE_AWS_ACCESSKEY = "AKIAIOSFODNN7EXAMPLE"
$env:CLYPSE_AWS_SECRETACCESSKEY = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
$env:CLYPSE_AWS_BUCKETNAME = "my-test-bucket"

# Optional: Set AWS region (defaults to us-east-1)
$env:AWS_DEFAULT_REGION = "us-west-2"

# Run the test (no parameters needed - uses environment variables)
.\Test-S3Access.ps1
```

### What the Script Tests

1. **PUT Object**: Uploads a test file to the specified S3 bucket
2. **GET Object**: Downloads the test file from the S3 bucket
3. **LIST Objects**: Lists all objects in the S3 bucket

### Output

The script provides colored output showing:
- ✓ Success indicators in green
- ✗ Error indicators in red
- General information in various colors
- A final summary of test results

### Exit Codes

- `0`: All tests passed
- `1`: One or more tests failed or setup issues occurred

### Notes

- The script automatically cleans up the test object after completion
- Requires appropriate S3 permissions for the target bucket
- Uses temporary files for upload/download operations
