using System.Diagnostics;
using clypse.portal.setup.Services.IO;
using clypse.portal.setup.Services.Process;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services.Build;

public class PortalBuildService(
    SetupOptions options,
    IProcessRunnerService processRunnerService,
    IIoService ioService,
    ILogger<PortalBuildService> logger) : IPortalBuildService
{
    public async Task<PortalBuildResult> Run()
    {
        var repoRoot =
            FindRepoRoot(ioService.GetCurrentDirectory()) ??
            ioService.GetCurrentDirectory();

        var portalProjectPath = ioService.CombinePath(repoRoot, "clypse.portal", "clypse.portal.csproj");
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
            WorkingDirectory = ioService.GetDirectoryName(portalProjectPath)!,
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

        var processResult = await processRunnerService.Run(startInfo);
        if (!processResult.Success)
        {
            logger.LogError(
                "dotnet publish failed with exit code {exitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}",
                processResult.ExitCode,
                processResult.OutputStreamText,
                processResult.ErrorStreamText);

            return new PortalBuildResult(false, string.Empty);
        }

        if (!ioService.DirectoryExists(wwwrootOutputPath))
        {
            logger.LogError(
                "dotnet publish succeeded but wwwroot output path '{wwwroot}' does not exist.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}",
                wwwrootOutputPath,
                processResult.OutputStreamText,
                processResult.ErrorStreamText);

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
            var publishOutputPath = ioService.CombinePath(repoRoot, "portal-output");
            return (publishOutputPath, ioService.CombinePath(publishOutputPath, "wwwroot"));
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

        return (trimmed, ioService.CombinePath(trimmed, "wwwroot"));
    }

    private string? FindRepoRoot(string startDirectory)
    {
        var currentDirectory = startDirectory;

        for (var i = 0; i < 10 && currentDirectory is not null; i++)
        {
            var solutionPath = ioService.CombinePath(currentDirectory, "clypse.sln");
            var portalProjectPath = ioService.CombinePath(currentDirectory, "clypse.portal", "clypse.portal.csproj");

            if (ioService.FileExists(solutionPath) || ioService.FileExists(portalProjectPath))
            {
                return currentDirectory;
            }

            currentDirectory = ioService.GetParentDirectory(currentDirectory);
        }

        return null;
    }
}
