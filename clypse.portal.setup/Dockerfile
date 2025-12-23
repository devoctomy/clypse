FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY clypse.sln .
COPY Directory.Packages.props .
COPY global.json .
COPY clypse.portal.setup/clypse.portal.setup.csproj clypse.portal.setup/
COPY clypse.core/clypse.core.csproj clypse.core/

# Restore dependencies
RUN dotnet restore clypse.portal.setup/clypse.portal.setup.csproj

# Copy source code
COPY clypse.portal.setup/ clypse.portal.setup/
COPY clypse.core/ clypse.core/

# Build the application
WORKDIR /src/clypse.portal.setup
RUN dotnet build -c Release -o /app/build --framework net10.0

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "clypse.portal.setup.dll"]
