function Initialise {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $repoRoot = Split-Path -Parent $repoRoot
    $setupProject = Join-Path $repoRoot 'clypse.portal.setup/clypse.portal.setup.csproj'

    dotnet build $setupProject -c Release -f net10.0 /p:CopyLocalLockFileAssemblies=true | Out-Null

    $setupBin = Join-Path $repoRoot 'clypse.portal.setup/bin/Release/net10.0'

    $dllPaths = Get-ChildItem -Path $setupBin -Filter '*.dll' -File |
        Sort-Object -Property Name |
        Select-Object -ExpandProperty FullName

    foreach ($dllPath in $dllPaths) {
        if (Test-Path $dllPath) {
            Add-Type -Path $dllPath
        }
    }

    $services = [Microsoft.Extensions.DependencyInjection.ServiceCollection]::new()
    [clypse.core.Extensions.ServiceCollectionExtensions]::AddClypseSetupServices($services) | Out-Null

    $provider = [Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions]::BuildServiceProvider($services)

    return $provider
}

Write-Host "Initialising Clypse setup services..."
$provider = Initialise

Write-Host "Creating IS3Service..."
$s3Service = $provider.GetService([clypse.portal.setup.S3.IS3Service])

Write-Host "Creating portal bucket..."
$null = $s3Service.CreateBucket('portal').GetAwaiter().GetResult()

Write-Host "Creating data bucket..."
$null = $s3Service.CreateBucket('data').GetAwaiter().GetResult()

Write-Host "Operation completed."