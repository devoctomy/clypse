using System.Text.Json;
using clypse.portal.setup.Services.IO;
using Moq;
using clypse.portal.setup.Enums;
using clypse.portal.setup.Services.Inventory;

namespace clypse.portal.setup.UnitTests.Services.Inventory;

public class InventoryServiceTests
{
    private readonly Mock<IIoService> _mockIoService = new();

    [Fact]
    public void GivenInventoryItem_WhenRecordResource_ThenItemIsAdded()
    {
        // Arrange
        var item = new InventoryItem
        {
            ResourceType = ResourceType.S3Bucket,
            ResourceId = "bucket-1"
        };
        var sut = new InventoryService(_mockIoService.Object);

        // Act
        sut.RecordResource(item);

        // Assert
        var resources = sut.GetResourcesByType(ResourceType.S3Bucket).ToList();
        Assert.Single(resources);
        Assert.Equal("bucket-1", resources[0].ResourceId);
    }

    [Fact]
    public void GivenMultipleItems_WhenGetResourcesByType_ThenReturnsMatchingItemsOnly()
    {
        // Arrange
        var sut = new InventoryService(_mockIoService.Object);
        sut.RecordResource(new InventoryItem { ResourceType = ResourceType.S3Bucket, ResourceId = "bucket-1" });
        sut.RecordResource(new InventoryItem { ResourceType = ResourceType.CloudFrontDistribution, ResourceId = "dist-1" });
        sut.RecordResource(new InventoryItem { ResourceType = ResourceType.S3Bucket, ResourceId = "bucket-2" });

        // Act
        var resources = sut.GetResourcesByType(ResourceType.S3Bucket).ToList();

        // Assert
        Assert.Equal(2, resources.Count);
        Assert.All(resources, r => Assert.Equal(ResourceType.S3Bucket, r.ResourceType));
        Assert.Contains(resources, r => r.ResourceId == "bucket-1");
        Assert.Contains(resources, r => r.ResourceId == "bucket-2");
    }

    [Fact]
    public void GivenInventoryItems_WhenSave_ThenWritesSerializedInventoryWithIoService()
    {
        // Arrange
        var sut = new InventoryService(_mockIoService.Object);
        sut.RecordResource(new InventoryItem { ResourceType = ResourceType.S3Bucket, ResourceId = "bucket-1" });
        sut.RecordResource(new InventoryItem { ResourceType = ResourceType.CloudFrontDistribution, ResourceId = "dist-1" });
        const string path = "dir/file.json";
        var capturedContent = string.Empty;

        _mockIoService.Setup(s => s.GetDirectoryName(path)).Returns("dir");
        _mockIoService.Setup(s => s.DirectoryExists("dir")).Returns(false);
        _mockIoService.Setup(s => s.CreateDirectory("dir"));
        _mockIoService
            .Setup(s => s.WriteAllText(path, It.IsAny<string>()))
            .Callback<string, string>((_, content) => capturedContent = content);

        // Act
        sut.Save(path);

        // Assert
        _mockIoService.Verify(s => s.GetDirectoryName(path), Times.Once);
        _mockIoService.Verify(s => s.DirectoryExists("dir"), Times.Once);
        _mockIoService.Verify(s => s.CreateDirectory("dir"), Times.Once);
        _mockIoService.Verify(s => s.WriteAllText(path, It.IsAny<string>()), Times.Once);

        var deserialized = JsonSerializer.Deserialize<List<InventoryItem>>(capturedContent);
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized!.Count);
        Assert.Contains(deserialized, r => r.ResourceType == ResourceType.S3Bucket && r.ResourceId == "bucket-1");
        Assert.Contains(deserialized, r => r.ResourceType == ResourceType.CloudFrontDistribution && r.ResourceId == "dist-1");
    }

    [Fact]
    public void GivenSerializedInventoryFile_WhenLoad_ThenInventoryIsReplaced()
    {
        // Arrange
        var sut = new InventoryService(_mockIoService.Object);
        sut.RecordResource(new InventoryItem { ResourceType = ResourceType.S3Bucket, ResourceId = "old-bucket" });
        var newItems = new List<InventoryItem>
        {
            new() { ResourceType = ResourceType.CloudFrontDistribution, ResourceId = "dist-1" },
            new() { ResourceType = ResourceType.CognitoUserPool, ResourceId = "pool-1" }
        };
        const string path = "dir/file.json";
        var serialized = JsonSerializer.Serialize(newItems);

        _mockIoService.Setup(s => s.FileExists(path)).Returns(true);
        _mockIoService.Setup(s => s.ReadAllText(path)).Returns(serialized);

        // Act
        sut.Load(path);

        // Assert
        var allItems = sut.GetResourcesByType(ResourceType.CloudFrontDistribution).Concat(
            sut.GetResourcesByType(ResourceType.CognitoUserPool)).ToList();
        Assert.Equal(2, allItems.Count);
        Assert.DoesNotContain(allItems, r => r.ResourceId == "old-bucket");
        Assert.Contains(allItems, r => r.ResourceType == ResourceType.CloudFrontDistribution && r.ResourceId == "dist-1");
        Assert.Contains(allItems, r => r.ResourceType == ResourceType.CognitoUserPool && r.ResourceId == "pool-1");
    }

    [Fact]
    public void GivenEmptySerializedInventory_WhenLoad_ThenInventoryIsCleared()
    {
        // Arrange
        var sut = new InventoryService(_mockIoService.Object);
        sut.RecordResource(new InventoryItem { ResourceType = ResourceType.S3Bucket, ResourceId = "bucket-1" });
        const string path = "dir/file.json";

        _mockIoService.Setup(s => s.FileExists(path)).Returns(true);
        _mockIoService.Setup(s => s.ReadAllText(path)).Returns(string.Empty);

        // Act
        sut.Load(path);

        // Assert
        var resources = sut.GetResourcesByType(ResourceType.S3Bucket).ToList();
        Assert.Empty(resources);
    }

    [Fact]
    public void GivenEmptyPath_WhenLoad_ThenInventoryIsClearedAndFileNotChecked()
    {
        // Arrange
        var sut = new InventoryService(_mockIoService.Object);
        sut.RecordResource(new InventoryItem { ResourceType = ResourceType.S3Bucket, ResourceId = "bucket-1" });

        // Act
        sut.Load(string.Empty);

        // Assert
        var resources = sut.GetResourcesByType(ResourceType.S3Bucket).ToList();
        Assert.Empty(resources);
        _mockIoService.Verify(s => s.FileExists(It.IsAny<string>()), Times.Never);
        _mockIoService.Verify(s => s.ReadAllText(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GivenPath_WhenFileDoesNotExist_ThenInventoryIsClearedAndFileNotRead()
    {
        // Arrange
        var sut = new InventoryService(_mockIoService.Object);
        sut.RecordResource(new InventoryItem { ResourceType = ResourceType.S3Bucket, ResourceId = "bucket-1" });
        const string path = "dir/file.json";

        _mockIoService.Setup(s => s.FileExists(path)).Returns(false);

        // Act
        sut.Load(path);

        // Assert
        var resources = sut.GetResourcesByType(ResourceType.S3Bucket).ToList();
        Assert.Empty(resources);
        _mockIoService.Verify(s => s.FileExists(path), Times.Once);
        _mockIoService.Verify(s => s.ReadAllText(It.IsAny<string>()), Times.Never);
    }
}
