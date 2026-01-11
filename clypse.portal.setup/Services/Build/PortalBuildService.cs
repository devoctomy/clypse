using System.Diagnostics;
using clypse.portal.setup.Services.IO;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services.Build;

public class PortalBuildService(
    SetupOptions options,
    IIoService ioService,
    ILogger<PortalBuildService> logger) : IPortalBuildService
{
    public async Task<PortalBuildResult> Run()
    {
        var repoRoot =
            FindRepoRoot(ioService.GetCurrentDirectory()) ??
            ioService.GetCurrentDirectory();

        var portalProjectPath = Path.Combine(repoRoot, "clypse.portal", "clypse.portal.csproj");
        if (!ioService.FileExists(portalProjectPath))
        {
            logger.LogError(
                "Unable to locate portal project at '{portalProjectPath}'. CurrentDirectory='{currentDirectory}'.",
                portalProjectPath,
                ioService.GetCurrentDirectory());
            return new PortalBuildResult(false, string.Empty);
        }

        var (publishOutputPath, wwwrootOutputPath) = ResolvePublishPaths(repoRoot, options.PortalBuildOutputPath);
        ioService.CreateDirectory(publishOutputPath);

        logger.LogInformation(
            "Building portal WASM via dotnet publish. Project='{project}', PublishOutput='{publishOutput}', Wwwroot='{wwwroot}'.",
            portalProjectPath,
            publishOutputPath,
            wwwrootOutputPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(portalProjectPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("publish");
        startInfo.ArgumentList.Add(portalProjectPath);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("-r");
        startInfo.ArgumentList.Add("browser-wasm");
        startInfo.ArgumentList.Add("--self-contained");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(publishOutputPath);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                logger.LogError("Failed to start dotnet publish process.");
                return new PortalBuildResult(false, string.Empty);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start dotnet publish process.");
            return new PortalBuildResult(false, string.Empty);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync().ConfigureAwait(false);

        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            logger.LogError(
                "dotnet publish failed with exit code {exitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}",
                process.ExitCode,
                standardOutput,
                standardError);

            return new PortalBuildResult(false, string.Empty);
        }

        if (!ioService.DirectoryExists(wwwrootOutputPath))
        {
            logger.LogError(
                "dotnet publish succeeded but wwwroot output path '{wwwroot}' does not exist.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}",
                wwwrootOutputPath,
                standardOutput,
                standardError);

            return new PortalBuildResult(false, string.Empty);
        }

        logger.LogInformation("Portal build succeeded. Output='{wwwroot}'.", wwwrootOutputPath);
        return new PortalBuildResult(true, wwwrootOutputPath);
    }

    private (string publishOutputPath, string wwwrootOutputPath) ResolvePublishPaths(
        string repoRoot,
        string? configuredPortalBuildOutputPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPortalBuildOutputPath))
        {
            var publishOutputPath = Path.Combine(repoRoot, "portal-output");
            return (publishOutputPath, Path.Combine(publishOutputPath, "wwwroot"));
        }

        var trimmed = configuredPortalBuildOutputPath.Trim();
        var endsWithWwwroot = string.Equals(
            Path.GetFileName(trimmed),
            "wwwroot",
            StringComparison.OrdinalIgnoreCase);

        if (endsWithWwwroot)
        {
            var publishOutputPath = ioService.GetParentDirectory(trimmed)
                ?? Path.GetDirectoryName(trimmed)
                ?? trimmed;
            return (publishOutputPath, trimmed);
        }

        return (trimmed, Path.Combine(trimmed, "wwwroot"));
    }

    private string? FindRepoRoot(string startDirectory)
    {
        var directoryInfo = new DirectoryInfo(startDirectory);

        for (var i = 0; i < 10 && directoryInfo is not null; i++)
        {
            var solutionPath = Path.Combine(directoryInfo.FullName, "clypse.sln");
            var portalProjectPath = Path.Combine(directoryInfo.FullName, "clypse.portal", "clypse.portal.csproj");

            if (ioService.FileExists(solutionPath) || ioService.FileExists(portalProjectPath))
            {
                return directoryInfo.FullName;
            }

            directoryInfo = directoryInfo.Parent;
        }

        return null;
    }
}
