# Custom Docker image for Clypse CI/CD workflows
# Based on Microsoft Playwright .NET image with additional tools pre-installed

FROM mcr.microsoft.com/playwright/dotnet:v1.55.0-noble

# Set environment variables
ENV DEBIAN_FRONTEND=noninteractive \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=1

# Install Python 3 and pip
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    python3 \
    python3-pip \
    curl \
    ca-certificates \
    && ln -sf /usr/bin/python3 /usr/bin/python \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install Node.js 20
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Install .NET 10 SDK
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir /usr/share/dotnet

# Ensure dotnet is in PATH
ENV PATH="${PATH}:/usr/share/dotnet"

# Pre-install .NET workloads (wasm-tools)
RUN dotnet workload install wasm-tools

# Verify installations
RUN echo "=== Installed Versions ===" && \
    node --version && \
    npm --version && \
    python --version && \
    pip3 --version && \
    dotnet --version && \
    dotnet workload list

# Set working directory
WORKDIR /workspace

# Default command
CMD ["/bin/bash"]
