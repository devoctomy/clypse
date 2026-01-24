using System.Diagnostics;
using clypse.portal.setup.Services.Build;
using clypse.portal.setup.Services.IO;
using clypse.portal.setup.Services.Process;
using Microsoft.Extensions.Logging;
using Moq;

namespace clypse.portal.setup.UnitTests.Services.Build;

public class PortalBuildServiceTests
{
    [Fact]
    public async Task GivenValidRepoStructure_WhenRun_ThenBuildsSuccessfully()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunnerService>();
        var mockIoService = new Mock<IIoService>();
        var options = new SetupOptions();
        var sut = new PortalBuildService(
            options,
            mockProcessRunner.Object,
            mockIoService.Object,
            Mock.Of<ILogger<PortalBuildService>>());

        var currentDir = Path.Combine(Path.GetTempPath(), "test-repo");
        var portalProjectPath = Path.Combine(currentDir, "clypse.portal", "clypse.portal.csproj");
        var solutionPath = Path.Combine(currentDir, "clypse.sln");
        var publishOutputPath = Path.Combine(currentDir, "portal-output");
        var wwwrootPath = Path.Combine(publishOutputPath, "wwwroot");

        mockIoService.Setup(io => io.GetCurrentDirectory()).Returns(currentDir);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.portal", "clypse.portal.csproj")).Returns(portalProjectPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.sln")).Returns(solutionPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "portal-output")).Returns(publishOutputPath);
        mockIoService.Setup(io => io.CombinePath(publishOutputPath, "wwwroot")).Returns(wwwrootPath);
        mockIoService.Setup(io => io.FileExists(solutionPath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(portalProjectPath)).Returns(true);
        mockIoService.Setup(io => io.DirectoryExists(wwwrootPath)).Returns(true);
        mockIoService.Setup(io => io.CreateDirectory(publishOutputPath));

        mockProcessRunner
            .Setup(pr => pr.Run(
                It.Is<ProcessStartInfo>(si =>
                    si.FileName == "dotnet" &&
                    si.ArgumentList.Contains("publish") &&
                    si.ArgumentList.Contains(portalProjectPath)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0, "Build succeeded", ""));

        // Act
        var result = await sut.Run();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(wwwrootPath, result.OutputPath);
        mockIoService.Verify(io => io.CreateDirectory(publishOutputPath), Times.Once);
        mockProcessRunner.Verify(pr => pr.Run(
            It.Is<ProcessStartInfo>(si =>
                si.FileName == "dotnet" &&
                si.ArgumentList.Contains("publish") &&
                si.ArgumentList.Contains("-c") &&
                si.ArgumentList.Contains("Release") &&
                si.ArgumentList.Contains("-r") &&
                si.ArgumentList.Contains("browser-wasm") &&
                si.ArgumentList.Contains("--self-contained") &&
                si.ArgumentList.Contains("-o") &&
                si.ArgumentList.Contains(publishOutputPath)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenCustomOutputPath_WhenRun_ThenUsesCustomPath()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunnerService>();
        var mockIoService = new Mock<IIoService>();
        var customOutputPath = "/custom/output";
        var options = new SetupOptions
        {
            PortalBuildOutputPath = customOutputPath
        };
        var sut = new PortalBuildService(
            options,
            mockProcessRunner.Object,
            mockIoService.Object,
            Mock.Of<ILogger<PortalBuildService>>());

        var currentDir = "/repo";
        var portalProjectPath = "/repo/clypse.portal/clypse.portal.csproj";
        var solutionPath = "/repo/clypse.sln";
        var wwwrootPath = "/custom/output/wwwroot";

        mockIoService.Setup(io => io.GetCurrentDirectory()).Returns(currentDir);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.portal", "clypse.portal.csproj")).Returns(portalProjectPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.sln")).Returns(solutionPath);
        mockIoService.Setup(io => io.CombinePath(customOutputPath, "wwwroot")).Returns(wwwrootPath);
        mockIoService.Setup(io => io.FileExists(solutionPath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(portalProjectPath)).Returns(true);
        mockIoService.Setup(io => io.DirectoryExists(wwwrootPath)).Returns(true);
        mockIoService.Setup(io => io.CreateDirectory(customOutputPath));

        mockProcessRunner
            .Setup(pr => pr.Run(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0, "Build succeeded", ""));

        // Act
        var result = await sut.Run();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(wwwrootPath, result.OutputPath);
        mockIoService.Verify(io => io.CreateDirectory(customOutputPath), Times.Once);
        mockProcessRunner.Verify(pr => pr.Run(
            It.Is<ProcessStartInfo>(si => si.ArgumentList.Contains(customOutputPath)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenCustomOutputPathEndingWithWwwroot_WhenRun_ThenExtractsParentDirectory()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunnerService>();
        var mockIoService = new Mock<IIoService>();
        var wwwrootPath = "/custom/output/wwwroot";
        var publishOutputPath = "/custom/output";
        var options = new SetupOptions
        {
            PortalBuildOutputPath = wwwrootPath
        };
        var sut = new PortalBuildService(
            options,
            mockProcessRunner.Object,
            mockIoService.Object,
            Mock.Of<ILogger<PortalBuildService>>());

        var currentDir = "/repo";
        var portalProjectPath = "/repo/clypse.portal/clypse.portal.csproj";
        var solutionPath = "/repo/clypse.sln";

        mockIoService.Setup(io => io.GetCurrentDirectory()).Returns(currentDir);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.portal", "clypse.portal.csproj")).Returns(portalProjectPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.sln")).Returns(solutionPath);
        mockIoService.Setup(io => io.FileExists(solutionPath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(portalProjectPath)).Returns(true);
        mockIoService.Setup(io => io.DirectoryExists(wwwrootPath)).Returns(true);
        mockIoService.Setup(io => io.GetParentDirectory(wwwrootPath)).Returns(publishOutputPath);
        mockIoService.Setup(io => io.CreateDirectory(publishOutputPath));

        mockProcessRunner
            .Setup(pr => pr.Run(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0, "Build succeeded", ""));

        // Act
        var result = await sut.Run();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(wwwrootPath, result.OutputPath);
        mockIoService.Verify(io => io.CreateDirectory(publishOutputPath), Times.Once);
        mockProcessRunner.Verify(pr => pr.Run(
            It.Is<ProcessStartInfo>(si => si.ArgumentList.Contains(publishOutputPath)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenPortalProjectNotFound_WhenRun_ThenReturnsFailure()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunnerService>();
        var mockIoService = new Mock<IIoService>();
        var options = new SetupOptions();
        var sut = new PortalBuildService(
            options,
            mockProcessRunner.Object,
            mockIoService.Object,
            Mock.Of<ILogger<PortalBuildService>>());

        var currentDir = "/repo";
        var portalProjectPath = "/repo/clypse.portal/clypse.portal.csproj";
        var solutionPath = "/repo/clypse.sln";

        mockIoService.Setup(io => io.GetCurrentDirectory()).Returns(currentDir);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.portal", "clypse.portal.csproj")).Returns(portalProjectPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.sln")).Returns(solutionPath);
        mockIoService.Setup(io => io.FileExists(solutionPath)).Returns(false);
        mockIoService.Setup(io => io.FileExists(portalProjectPath)).Returns(false);

        // Act
        var result = await sut.Run();

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.OutputPath);
        mockProcessRunner.Verify(pr => pr.Run(
            It.IsAny<ProcessStartInfo>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenBuildFails_WhenRun_ThenReturnsFailure()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunnerService>();
        var mockIoService = new Mock<IIoService>();
        var options = new SetupOptions();
        var sut = new PortalBuildService(
            options,
            mockProcessRunner.Object,
            mockIoService.Object,
            Mock.Of<ILogger<PortalBuildService>>());

        var currentDir = "/repo";
        var portalProjectPath = "/repo/clypse.portal/clypse.portal.csproj";
        var solutionPath = "/repo/clypse.sln";
        var publishOutputPath = "/repo/portal-output";

        mockIoService.Setup(io => io.GetCurrentDirectory()).Returns(currentDir);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.portal", "clypse.portal.csproj")).Returns(portalProjectPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.sln")).Returns(solutionPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "portal-output")).Returns(publishOutputPath);
        mockIoService.Setup(io => io.FileExists(solutionPath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(portalProjectPath)).Returns(true);
        mockIoService.Setup(io => io.CreateDirectory(publishOutputPath));

        mockProcessRunner
            .Setup(pr => pr.Run(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, 1, "Build output", "Build failed with errors"));

        // Act
        var result = await sut.Run();

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.OutputPath);
        mockProcessRunner.Verify(pr => pr.Run(
            It.IsAny<ProcessStartInfo>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenWwwrootNotCreatedAfterBuild_WhenRun_ThenReturnsFailure()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunnerService>();
        var mockIoService = new Mock<IIoService>();
        var options = new SetupOptions();
        var sut = new PortalBuildService(
            options,
            mockProcessRunner.Object,
            mockIoService.Object,
            Mock.Of<ILogger<PortalBuildService>>());

        var currentDir = "/repo";
        var portalProjectPath = "/repo/clypse.portal/clypse.portal.csproj";
        var solutionPath = "/repo/clypse.sln";
        var publishOutputPath = "/repo/portal-output";
        var wwwrootPath = "/repo/portal-output/wwwroot";

        mockIoService.Setup(io => io.GetCurrentDirectory()).Returns(currentDir);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.portal", "clypse.portal.csproj")).Returns(portalProjectPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.sln")).Returns(solutionPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "portal-output")).Returns(publishOutputPath);
        mockIoService.Setup(io => io.CombinePath(publishOutputPath, "wwwroot")).Returns(wwwrootPath);
        mockIoService.Setup(io => io.FileExists(solutionPath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(portalProjectPath)).Returns(true);
        mockIoService.Setup(io => io.DirectoryExists(wwwrootPath)).Returns(false);
        mockIoService.Setup(io => io.CreateDirectory(publishOutputPath));

        mockProcessRunner
            .Setup(pr => pr.Run(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0, "Build succeeded", ""));

        // Act
        var result = await sut.Run();

        // Assert
        Assert.False(result.Success);
        Assert.Empty(result.OutputPath);
        mockProcessRunner.Verify(pr => pr.Run(
            It.IsAny<ProcessStartInfo>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenRepoRootNotFoundByWalkingUp_WhenRun_ThenUsesCurrentDirectory()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunnerService>();
        var mockIoService = new Mock<IIoService>();
        var options = new SetupOptions();
        var sut = new PortalBuildService(
            options,
            mockProcessRunner.Object,
            mockIoService.Object,
            Mock.Of<ILogger<PortalBuildService>>());

        var currentDir = "/some/deep/path";
        var portalProjectPath = "/some/deep/path/clypse.portal/clypse.portal.csproj";
        var publishOutputPath = "/some/deep/path/portal-output";
        var wwwrootPath = "/some/deep/path/portal-output/wwwroot";

        mockIoService.Setup(io => io.GetCurrentDirectory()).Returns(currentDir);
        mockIoService.Setup(io => io.CombinePath(It.IsAny<string[]>())).Returns((string[] paths) => string.Join("/", paths));
        mockIoService.Setup(io => io.FileExists(It.IsAny<string>())).Returns(false);
        mockIoService.Setup(io => io.FileExists(portalProjectPath)).Returns(true);
        mockIoService.Setup(io => io.DirectoryExists(wwwrootPath)).Returns(true);
        mockIoService.Setup(io => io.CreateDirectory(publishOutputPath));

        mockProcessRunner
            .Setup(pr => pr.Run(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0, "Build succeeded", ""));

        // Act
        var result = await sut.Run();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(wwwrootPath, result.OutputPath);
    }

    [Fact]
    public async Task GivenPortalProjectFoundByWalkingUpToSolution_WhenRun_ThenBuildsSuccessfully()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunnerService>();
        var mockIoService = new Mock<IIoService>();
        var options = new SetupOptions();
        var sut = new PortalBuildService(
            options,
            mockProcessRunner.Object,
            mockIoService.Object,
            Mock.Of<ILogger<PortalBuildService>>());

        var currentDir = "/repo/subfolder";
        var solutionPath = "/repo/clypse.sln";
        var portalProjectPath = "/repo/clypse.portal/clypse.portal.csproj";
        var publishOutputPath = "/repo/portal-output";
        var wwwrootPath = "/repo/portal-output/wwwroot";

        mockIoService.Setup(io => io.GetCurrentDirectory()).Returns(currentDir);
        mockIoService.Setup(io => io.GetParentDirectory("/repo/subfolder")).Returns("/repo");
        mockIoService.Setup(io => io.CombinePath("/repo/subfolder", "clypse.sln")).Returns("/repo/subfolder/clypse.sln");
        mockIoService.Setup(io => io.CombinePath("/repo/subfolder", "clypse.portal", "clypse.portal.csproj")).Returns("/repo/subfolder/clypse.portal/clypse.portal.csproj");
        mockIoService.Setup(io => io.CombinePath("/repo", "clypse.sln")).Returns(solutionPath);
        mockIoService.Setup(io => io.CombinePath("/repo", "clypse.portal", "clypse.portal.csproj")).Returns(portalProjectPath);
        mockIoService.Setup(io => io.CombinePath("/repo", "portal-output")).Returns(publishOutputPath);
        mockIoService.Setup(io => io.CombinePath(publishOutputPath, "wwwroot")).Returns(wwwrootPath);
        mockIoService.Setup(io => io.FileExists("/repo/subfolder/clypse.sln")).Returns(false);
        mockIoService.Setup(io => io.FileExists("/repo/subfolder/clypse.portal/clypse.portal.csproj")).Returns(false);
        mockIoService.Setup(io => io.FileExists(solutionPath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(portalProjectPath)).Returns(true);
        mockIoService.Setup(io => io.DirectoryExists(wwwrootPath)).Returns(true);
        mockIoService.Setup(io => io.CreateDirectory(publishOutputPath));

        mockProcessRunner
            .Setup(pr => pr.Run(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0, "Build succeeded", ""));

        // Act
        var result = await sut.Run();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(wwwrootPath, result.OutputPath);
        mockProcessRunner.Verify(pr => pr.Run(
            It.Is<ProcessStartInfo>(si => si.ArgumentList.Contains(portalProjectPath)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenPortalProjectFoundByWalkingUpToPortalProject_WhenRun_ThenBuildsSuccessfully()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunnerService>();
        var mockIoService = new Mock<IIoService>();
        var options = new SetupOptions();
        var sut = new PortalBuildService(
            options,
            mockProcessRunner.Object,
            mockIoService.Object,
            Mock.Of<ILogger<PortalBuildService>>());

        var currentDir = "/repo/subfolder";
        var solutionPath = "/repo/clypse.sln";
        var portalProjectPath = "/repo/clypse.portal/clypse.portal.csproj";
        var publishOutputPath = "/repo/portal-output";
        var wwwrootPath = "/repo/portal-output/wwwroot";

        mockIoService.Setup(io => io.GetCurrentDirectory()).Returns(currentDir);
        mockIoService.Setup(io => io.GetParentDirectory("/repo/subfolder")).Returns("/repo");
        mockIoService.Setup(io => io.CombinePath("/repo/subfolder", "clypse.sln")).Returns("/repo/subfolder/clypse.sln");
        mockIoService.Setup(io => io.CombinePath("/repo/subfolder", "clypse.portal", "clypse.portal.csproj")).Returns("/repo/subfolder/clypse.portal/clypse.portal.csproj");
        mockIoService.Setup(io => io.CombinePath("/repo", "clypse.sln")).Returns(solutionPath);
        mockIoService.Setup(io => io.CombinePath("/repo", "clypse.portal", "clypse.portal.csproj")).Returns(portalProjectPath);
        mockIoService.Setup(io => io.CombinePath("/repo", "portal-output")).Returns(publishOutputPath);
        mockIoService.Setup(io => io.CombinePath(publishOutputPath, "wwwroot")).Returns(wwwrootPath);
        mockIoService.Setup(io => io.FileExists("/repo/subfolder/clypse.sln")).Returns(false);
        mockIoService.Setup(io => io.FileExists("/repo/subfolder/clypse.portal/clypse.portal.csproj")).Returns(false);
        mockIoService.Setup(io => io.FileExists(solutionPath)).Returns(false);
        mockIoService.Setup(io => io.FileExists(portalProjectPath)).Returns(true);
        mockIoService.Setup(io => io.DirectoryExists(wwwrootPath)).Returns(true);
        mockIoService.Setup(io => io.CreateDirectory(publishOutputPath));

        mockProcessRunner
            .Setup(pr => pr.Run(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, 0, "Build succeeded", ""));

        // Act
        var result = await sut.Run();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(wwwrootPath, result.OutputPath);
        mockProcessRunner.Verify(pr => pr.Run(
            It.Is<ProcessStartInfo>(si => si.ArgumentList.Contains(portalProjectPath)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenValidBuild_WhenRun_ThenProcessStartInfoIsConfiguredCorrectly()
    {
        // Arrange
        var mockProcessRunner = new Mock<IProcessRunnerService>();
        var mockIoService = new Mock<IIoService>();
        var options = new SetupOptions();
        var sut = new PortalBuildService(
            options,
            mockProcessRunner.Object,
            mockIoService.Object,
            Mock.Of<ILogger<PortalBuildService>>());

        var currentDir = "/repo";
        var portalProjectPath = "/repo/clypse.portal/clypse.portal.csproj";
        var portalProjectDir = "/repo/clypse.portal";
        var solutionPath = "/repo/clypse.sln";
        var publishOutputPath = "/repo/portal-output";
        var wwwrootPath = "/repo/portal-output/wwwroot";

        mockIoService.Setup(io => io.GetCurrentDirectory()).Returns(currentDir);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.portal", "clypse.portal.csproj")).Returns(portalProjectPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "clypse.sln")).Returns(solutionPath);
        mockIoService.Setup(io => io.CombinePath(currentDir, "portal-output")).Returns(publishOutputPath);
        mockIoService.Setup(io => io.CombinePath(publishOutputPath, "wwwroot")).Returns(wwwrootPath);
        mockIoService.Setup(io => io.GetDirectoryName(portalProjectPath)).Returns(portalProjectDir);
        mockIoService.Setup(io => io.FileExists(solutionPath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(portalProjectPath)).Returns(true);
        mockIoService.Setup(io => io.DirectoryExists(wwwrootPath)).Returns(true);
        mockIoService.Setup(io => io.CreateDirectory(publishOutputPath));

        ProcessStartInfo? capturedStartInfo = null;
        mockProcessRunner
            .Setup(pr => pr.Run(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
            .Callback<ProcessStartInfo, CancellationToken>((si, _) => capturedStartInfo = si)
            .ReturnsAsync((true, 0, "Build succeeded", ""));

        // Act
        await sut.Run();

        // Assert
        Assert.NotNull(capturedStartInfo);
        Assert.Equal("dotnet", capturedStartInfo.FileName);
        Assert.Equal(portalProjectDir, capturedStartInfo.WorkingDirectory);
        Assert.True(capturedStartInfo.RedirectStandardOutput);
        Assert.True(capturedStartInfo.RedirectStandardError);
        Assert.False(capturedStartInfo.UseShellExecute);
        Assert.True(capturedStartInfo.CreateNoWindow);
    }
}
