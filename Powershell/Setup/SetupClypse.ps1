function Setup-Clypse {
    $ErrorActionPreference = 'Stop'

    $repoRoot = Split-Path -Parent $PSScriptRoot
    $repoRoot = Split-Path -Parent $repoRoot

    $setupProject = Join-Path $repoRoot 'clypse.portal.setup/clypse.portal.setup.csproj'

    dotnet build $setupProject -c Release -f net10.0 /p:CopyLocalLockFileAssemblies=true

    $setupBin = Join-Path $repoRoot 'clypse.portal.setup/bin/Release/net10.0'

    $dllPaths = @(
        (Join-Path $setupBin 'clypse.portal.setup.dll'),
        (Join-Path $setupBin 'Amazon.S3.dll'),
        (Join-Path $setupBin 'Microsoft.Extensions.Logging.dll'),
        (Join-Path $setupBin 'Microsoft.Extensions.Logging.Abstractions.dll'),
        (Join-Path $setupBin 'Microsoft.Extensions.Logging.Console.dll'),
        (Join-Path $setupBin 'Microsoft.Extensions.DependencyInjection.dll'),
        (Join-Path $setupBin 'Microsoft.Extensions.DependencyInjection.Abstractions.dll'),
        (Join-Path $setupBin 'Microsoft.Extensions.Configuration.dll'),
        (Join-Path $setupBin 'Microsoft.Extensions.Configuration.Binder.dll'),
        (Join-Path $setupBin 'Microsoft.Extensions.Configuration.EnvironmentVariables.dll')
    )

    foreach ($dllPath in $dllPaths) {
        if (Test-Path $dllPath) {
            Add-Type -Path $dllPath
        }
    }

    $services = [Microsoft.Extensions.DependencyInjection.ServiceCollection]::new()
    [clypse.core.Extensions.ServiceCollectionExtensions]::AddClypseSetupServices($services) | Out-Null

    $provider = [Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions]::BuildServiceProvider($services)

    $s3Service = [Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions]::GetRequiredService(
        $provider,
        [clypse.portal.setup.S3.IS3Service]
    )

    $null = $s3Service.CreateBucket('test').GetAwaiter().GetResult()
    Write-Host "Created bucket 'test'."
}

Setup-Clypse
