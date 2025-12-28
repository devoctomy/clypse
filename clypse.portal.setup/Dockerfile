FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install Python (required for WASM build)
RUN apt-get update && \
    apt-get install -y python3 && \
    ln -s /usr/bin/python3 /usr/bin/python && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy solution and project files
COPY clypse.sln .
COPY Directory.Packages.props .
COPY global.json .
COPY clypse.portal.setup/clypse.portal.setup.csproj clypse.portal.setup/
COPY clypse.portal/clypse.portal.csproj clypse.portal/
COPY clypse.core/clypse.core.csproj clypse.core/

# Install .NET WASM workload
RUN dotnet workload install wasm-tools

# Restore dependencies
RUN dotnet restore clypse.portal.setup/clypse.portal.setup.csproj
RUN dotnet restore clypse.portal/clypse.portal.csproj

# Copy source code
COPY clypse.portal.setup/ clypse.portal.setup/
COPY clypse.portal/ clypse.portal/
COPY clypse.core/ clypse.core/

# Build the setup application
WORKDIR /src/clypse.portal.setup
RUN dotnet build -c Release -o /app/build --framework net10.0

# Build and publish Blazor WebAssembly portal
WORKDIR /src/clypse.portal
RUN dotnet publish clypse.portal.csproj -c Release -r browser-wasm --self-contained -o /app/portal-output

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/build .
COPY --from=build /app/portal-output/wwwroot /app/portal-output/wwwroot

# Set environment variable for portal build output path
ENV CLYPSE_SETUP__PortalBuildOutputPath=/app/portal-output/wwwroot

ENTRYPOINT ["dotnet", "clypse.portal.setup.dll"]
