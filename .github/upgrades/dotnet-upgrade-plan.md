# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade clypse.core\clypse.core.csproj
4. Upgrade clypse.portal.UITests\clypse.portal.UITests.csproj
5. Upgrade clypse.portal\clypse.portal.csproj
6. Upgrade clypse.core.IntTests\clypse.core.IntTests.csproj
7. Upgrade clypse.core.UnitTests\clypse.core.UnitTests.csproj
8. Run unit tests to validate upgrade in the projects listed below:
   - clypse.portal.UITests\clypse.portal.UITests.csproj
   - clypse.core.IntTests\clypse.core.IntTests.csproj
   - clypse.core.UnitTests\clypse.core.UnitTests.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|
|                                                |                             |

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                   | Current Version | New Version | Description                                   |
|:-----------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.Components.WebAssembly     |   8.0.19        |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.AspNetCore.Components.WebAssembly.DevServer |   8.0.20   |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.Extensions.DependencyInjection.Abstractions |   9.0.9    |  10.0.0     | Recommended for .NET 10.0                      |
| Microsoft.JSInterop                            |   8.0.19        |  10.0.0     | Recommended for .NET 10.0                      |

### Project upgrade details
This section contains details about each project upgrade and modifications that need to be done in the project.

#### clypse.core\clypse.core.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.Extensions.DependencyInjection.Abstractions should be updated from `9.0.9` to `10.0.0` (recommended for .NET 10.0)
  - Microsoft.JSInterop should be updated from `8.0.19` to `10.0.0` (recommended for .NET 10.0)

Other changes:
  - Review DI usage in Blazor components if any and JS interop calls for API changes in .NET 10.

#### clypse.portal.UITests\clypse.portal.UITests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

Other changes:
  - Validate test adapters and any browser automation dependencies for compatibility.

#### clypse.portal\clypse.portal.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Components.WebAssembly should be updated from `8.0.19` to `10.0.0` (recommended for .NET 10.0)
  - Microsoft.AspNetCore.Components.WebAssembly.DevServer should be updated from `8.0.20` to `10.0.0` (recommended for .NET 10.0)

Other changes:
  - Validate Program.cs bootstrapping for Blazor WASM and update any deprecated APIs.

#### clypse.core.IntTests\clypse.core.IntTests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

Other changes:
  - Ensure test SDKs are compatible with .NET 10.

#### clypse.core.UnitTests\clypse.core.UnitTests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net8.0` to `net10.0`

Other changes:
  - Ensure test SDKs are compatible with .NET 10.
