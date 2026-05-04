# Docker Image Build and Push Instructions

This directory contains a Dockerfile for building a custom CI/CD image for Clypse workflows.

## Image Contents

The custom image is based on `mcr.microsoft.com/playwright/dotnet:v1.55.0-noble` and includes:

- **Playwright** - Pre-configured for .NET UI testing
- **Node.js 20** - For JavaScript tooling and frontend builds
- **Python 3** - Required for WASM build tools
- **.NET 10 SDK** - Latest .NET SDK
- **WASM Tools** - Pre-installed .NET workload for WebAssembly

## Building the Image

```bash
# Build the image
docker build -t yourusername/clypse-ci:latest .

# Tag with version (optional)
docker tag yourusername/clypse-ci:latest yourusername/clypse-ci:v1.0.0
```

## Pushing to Docker Hub

```bash
# Login to Docker Hub
docker login

# Push the latest tag
docker push yourusername/clypse-ci:latest

# Push version tag (if created)
docker push yourusername/clypse-ci:v1.0.0
```

## Using the Image in GitHub Actions

Update your workflow files to use your custom image:

```yaml
jobs:
  your-job:
    runs-on: ubuntu-latest
    container:
      image: yourusername/clypse-ci:latest
    steps:
      # Your steps here - no need to install Python, Node, or WASM tools
      - name: Checkout code
        uses: actions/checkout@v6
      
      # Skip the Python, Node setup, and workload install steps
      # Go straight to your build and test steps
```

## Benefits

- **Faster Workflows**: No need to install Python, Node.js, or WASM tools on each run
- **Consistency**: Same environment across all jobs and workflows
- **Cost Savings**: Reduced GitHub Actions minutes usage

## Maintenance

When updating dependencies (e.g., Node version, .NET version, Playwright version):

1. Update the Dockerfile
2. Build a new image with an incremented version tag
3. Update the workflow files to reference the new image version
