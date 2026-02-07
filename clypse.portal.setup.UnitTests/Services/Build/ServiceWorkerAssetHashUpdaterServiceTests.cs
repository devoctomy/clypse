using clypse.portal.setup.Services.Build;
using clypse.portal.setup.Services.IO;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.portal.setup.UnitTests.Services.Build;

public class ServiceWorkerAssetHashUpdaterServiceTests
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public async Task GivenValidAssetAndManifest_WhenUpdateAssetAsync_ThenReturnsTrue()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var mockLogger = new Mock<ILogger<ServiceWorkerAssetHashUpdaterService>>();
        var sut = new ServiceWorkerAssetHashUpdaterService(mockIoService.Object, mockLogger.Object);

        var publishDirectory = "/publish";
        var assetPath = "appsettings.json";
        var assetFilePath = "/publish/appsettings.json";
        var manifestFilePath = "/publish/service-worker-assets.js";

        var assetContent = "{\"test\":\"data\"}"u8.ToArray();
        var expectedHash = $"sha256-{Convert.ToBase64String(SHA256.HashData(assetContent))}";

        var manifestContent = """
            self.assetsManifest = {
              "assets": [
                {
                  "url": "appsettings.json",
                  "hash": "sha256-oldHashValue"
                }
              ],
              "version": "test"
            };
            """;

        mockIoService.Setup(io => io.CombinePath(publishDirectory, assetPath)).Returns(assetFilePath);
        mockIoService.Setup(io => io.CombinePath(publishDirectory, "service-worker-assets.js")).Returns(manifestFilePath);
        mockIoService.Setup(io => io.FileExists(assetFilePath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(manifestFilePath)).Returns(true);
        mockIoService.Setup(io => io.ReadAllBytesAsync(assetFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(assetContent);
        mockIoService.Setup(io => io.ReadAllTextAsync(manifestFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(manifestContent);
        mockIoService.Setup(io => io.WriteAllTextAsync(manifestFilePath, It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await sut.UpdateAssetAsync(publishDirectory, assetPath);

        // Assert
        Assert.True(result);
        mockIoService.Verify(io => io.WriteAllTextAsync(
            manifestFilePath,
            It.Is<string>(s => s.Contains(expectedHash)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenMissingAssetFile_WhenUpdateAssetAsync_ThenReturnsFalse()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var mockLogger = new Mock<ILogger<ServiceWorkerAssetHashUpdaterService>>();
        var sut = new ServiceWorkerAssetHashUpdaterService(mockIoService.Object, mockLogger.Object);

        var publishDirectory = "/publish";
        var assetPath = "appsettings.json";
        var assetFilePath = "/publish/appsettings.json";
        var manifestFilePath = "/publish/service-worker-assets.js";

        mockIoService.Setup(io => io.CombinePath(publishDirectory, assetPath)).Returns(assetFilePath);
        mockIoService.Setup(io => io.CombinePath(publishDirectory, "service-worker-assets.js")).Returns(manifestFilePath);
        mockIoService.Setup(io => io.FileExists(assetFilePath)).Returns(false);
        mockIoService.Setup(io => io.FileExists(manifestFilePath)).Returns(true);

        // Act
        var result = await sut.UpdateAssetAsync(publishDirectory, assetPath);

        // Assert
        Assert.False(result);
        mockIoService.Verify(io => io.ReadAllBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenMissingManifestFile_WhenUpdateAssetAsync_ThenReturnsFalse()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var mockLogger = new Mock<ILogger<ServiceWorkerAssetHashUpdaterService>>();
        var sut = new ServiceWorkerAssetHashUpdaterService(mockIoService.Object, mockLogger.Object);

        var publishDirectory = "/publish";
        var assetPath = "appsettings.json";
        var assetFilePath = "/publish/appsettings.json";
        var manifestFilePath = "/publish/service-worker-assets.js";

        mockIoService.Setup(io => io.CombinePath(publishDirectory, assetPath)).Returns(assetFilePath);
        mockIoService.Setup(io => io.CombinePath(publishDirectory, "service-worker-assets.js")).Returns(manifestFilePath);
        mockIoService.Setup(io => io.FileExists(assetFilePath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(manifestFilePath)).Returns(false);

        // Act
        var result = await sut.UpdateAssetAsync(publishDirectory, assetPath);

        // Assert
        Assert.False(result);
        mockIoService.Verify(io => io.ReadAllBytesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenRealWorldAssetJs_WhenUpdateAssetAsync_ThenReturnsTrue()
    {
        var mockIoService = new Mock<IIoService>();
        var mockLogger = new Mock<ILogger<ServiceWorkerAssetHashUpdaterService>>();
        var sut = new ServiceWorkerAssetHashUpdaterService(mockIoService.Object, mockLogger.Object);

        var publishDirectory = "/publish";
        var assetPath = "appsettings.json";
        var assetFilePath = "/publish/missing.json";
        var manifestFilePath = "/publish/service-worker-assets.js";

        var assetContent = "{\"test\":\"data\"}"u8.ToArray();

        var realWorldAssetsText = await File.ReadAllTextAsync("Data/service-worker-assets.js");

        mockIoService.Setup(io => io.CombinePath(publishDirectory, assetPath)).Returns(assetFilePath);
        mockIoService.Setup(io => io.CombinePath(publishDirectory, "service-worker-assets.js")).Returns(manifestFilePath);
        mockIoService.Setup(io => io.FileExists(assetFilePath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(manifestFilePath)).Returns(true);
        mockIoService.Setup(io => io.ReadAllBytesAsync(assetFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(assetContent);
        mockIoService.Setup(io => io.ReadAllTextAsync(manifestFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(realWorldAssetsText);

        // Act
        var result = await sut.UpdateAssetAsync(publishDirectory, assetPath);

        // Assert
        Assert.True(result);
        mockIoService.Verify(io => io.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAssetNotInManifest_WhenUpdateAssetAsync_ThenReturnsFalse()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var mockLogger = new Mock<ILogger<ServiceWorkerAssetHashUpdaterService>>();
        var sut = new ServiceWorkerAssetHashUpdaterService(mockIoService.Object, mockLogger.Object);

        var publishDirectory = "/publish";
        var assetPath = "missing.json";
        var assetFilePath = "/publish/missing.json";
        var manifestFilePath = "/publish/service-worker-assets.js";

        var assetContent = "{\"test\":\"data\"}"u8.ToArray();

        var manifestContentObject = new
        {
            version = "test",
            assets = new[]
            {
                new
                {
                    url = "appsettings.json",
                    hash = "sha256-oldHashValue"
                }
            }
        };
        var manifestContent = $"self.assetsManifest = {JsonSerializer.Serialize(manifestContentObject, jsonSerializerOptions)};\r\n";

        mockIoService.Setup(io => io.CombinePath(publishDirectory, assetPath)).Returns(assetFilePath);
        mockIoService.Setup(io => io.CombinePath(publishDirectory, "service-worker-assets.js")).Returns(manifestFilePath);
        mockIoService.Setup(io => io.FileExists(assetFilePath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(manifestFilePath)).Returns(true);
        mockIoService.Setup(io => io.ReadAllBytesAsync(assetFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(assetContent);
        mockIoService.Setup(io => io.ReadAllTextAsync(manifestFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(manifestContent);

        // Act
        var result = await sut.UpdateAssetAsync(publishDirectory, assetPath);

        // Assert
        Assert.False(result);
        mockIoService.Verify(io => io.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenInvalidManifestJson_WhenUpdateAssetAsync_ThenReturnsFalse()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var mockLogger = new Mock<ILogger<ServiceWorkerAssetHashUpdaterService>>();
        var sut = new ServiceWorkerAssetHashUpdaterService(mockIoService.Object, mockLogger.Object);

        var publishDirectory = "/publish";
        var assetPath = "appsettings.json";
        var assetFilePath = "/publish/appsettings.json";
        var manifestFilePath = "/publish/service-worker-assets.js";

        var assetContent = "{\"test\":\"data\"}"u8.ToArray();
        var invalidManifestContent = "self.assetsManifest = { invalid json };";

        mockIoService.Setup(io => io.CombinePath(publishDirectory, assetPath)).Returns(assetFilePath);
        mockIoService.Setup(io => io.CombinePath(publishDirectory, "service-worker-assets.js")).Returns(manifestFilePath);
        mockIoService.Setup(io => io.FileExists(assetFilePath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(manifestFilePath)).Returns(true);
        mockIoService.Setup(io => io.ReadAllBytesAsync(assetFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(assetContent);
        mockIoService.Setup(io => io.ReadAllTextAsync(manifestFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(invalidManifestContent);

        // Act
        var result = await sut.UpdateAssetAsync(publishDirectory, assetPath);

        // Assert
        Assert.False(result);
        mockIoService.Verify(io => io.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenManifestWithoutAssetsArray_WhenUpdateAssetAsync_ThenReturnsFalse()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var mockLogger = new Mock<ILogger<ServiceWorkerAssetHashUpdaterService>>();
        var sut = new ServiceWorkerAssetHashUpdaterService(mockIoService.Object, mockLogger.Object);

        var publishDirectory = "/publish";
        var assetPath = "appsettings.json";
        var assetFilePath = "/publish/appsettings.json";
        var manifestFilePath = "/publish/service-worker-assets.js";

        var assetContent = "{\"test\":\"data\"}"u8.ToArray();
        var manifestContent = """
            self.assetsManifest = {
              "version": "test"
            };
            """;

        mockIoService.Setup(io => io.CombinePath(publishDirectory, assetPath)).Returns(assetFilePath);
        mockIoService.Setup(io => io.CombinePath(publishDirectory, "service-worker-assets.js")).Returns(manifestFilePath);
        mockIoService.Setup(io => io.FileExists(assetFilePath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(manifestFilePath)).Returns(true);
        mockIoService.Setup(io => io.ReadAllBytesAsync(assetFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(assetContent);
        mockIoService.Setup(io => io.ReadAllTextAsync(manifestFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(manifestContent);

        // Act
        var result = await sut.UpdateAssetAsync(publishDirectory, assetPath);

        // Assert
        Assert.False(result);
        mockIoService.Verify(io => io.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenAssetWithBackslashPath_WhenUpdateAssetAsync_ThenNormalizesToForwardSlash()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var mockLogger = new Mock<ILogger<ServiceWorkerAssetHashUpdaterService>>();
        var sut = new ServiceWorkerAssetHashUpdaterService(mockIoService.Object, mockLogger.Object);

        var publishDirectory = "/publish";
        var assetPath = "subfolder\\appsettings.json";
        var assetFilePath = "/publish/subfolder/appsettings.json";
        var manifestFilePath = "/publish/service-worker-assets.js";

        var assetContent = "{\"test\":\"data\"}"u8.ToArray();
        var expectedHash = $"sha256-{Convert.ToBase64String(SHA256.HashData(assetContent))}";

        var manifestContent = """
            self.assetsManifest = {
              "assets": [
                {
                  "url": "subfolder/appsettings.json",
                  "hash": "sha256-oldHashValue"
                }
              ],
              "version": "test"
            };
            """;

        mockIoService.Setup(io => io.CombinePath(publishDirectory, assetPath)).Returns(assetFilePath);
        mockIoService.Setup(io => io.CombinePath(publishDirectory, "service-worker-assets.js")).Returns(manifestFilePath);
        mockIoService.Setup(io => io.FileExists(assetFilePath)).Returns(true);
        mockIoService.Setup(io => io.FileExists(manifestFilePath)).Returns(true);
        mockIoService.Setup(io => io.ReadAllBytesAsync(assetFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(assetContent);
        mockIoService.Setup(io => io.ReadAllTextAsync(manifestFilePath, It.IsAny<CancellationToken>())).ReturnsAsync(manifestContent);
        mockIoService.Setup(io => io.WriteAllTextAsync(manifestFilePath, It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await sut.UpdateAssetAsync(publishDirectory, assetPath);

        // Assert
        Assert.True(result);
        mockIoService.Verify(io => io.WriteAllTextAsync(
            manifestFilePath,
            It.Is<string>(s => s.Contains(expectedHash)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
