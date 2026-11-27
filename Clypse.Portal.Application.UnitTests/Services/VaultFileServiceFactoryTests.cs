using Clypse.Portal.Application.Services;
using Clypse.Portal.Models.Settings;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VaultFileServiceFactoryTests
{
    private readonly Mock<ISyncProfileRegistry> mockRegistry = new();
    private readonly VaultFileTreeService tree = new();
    private readonly ClypseSettings settings = new();

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("registry", Assert.Throws<ArgumentNullException>(() => new VaultFileServiceFactory(null!, tree, settings, TimeProvider.System)).ParamName);
        Assert.Equal("tree", Assert.Throws<ArgumentNullException>(() => new VaultFileServiceFactory(mockRegistry.Object, null!, settings, TimeProvider.System)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new VaultFileServiceFactory(mockRegistry.Object, tree, null!, TimeProvider.System)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() => new VaultFileServiceFactory(mockRegistry.Object, tree, settings, null!)).ParamName);
    }

    [Fact]
    public void GivenAVault_WhenCreating_ThenItsFilesAreInItsProfilesFileStore()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("D");
        mockRegistry.Setup(x => x.GetFileStore(id)).Returns(new Mock<ISyncFileStore>().Object);
        var sut = new VaultFileServiceFactory(mockRegistry.Object, tree, settings, TimeProvider.System);

        // Act
        var service = sut.Create(new Vault { Id = id, Root = $"profiles/{id}/" });

        // Assert
        Assert.IsType<VaultFileService>(service);
        mockRegistry.Verify(x => x.GetFileStore(id), Times.Once);
    }

    [Fact]
    public void GivenNoVault_WhenCreating_ThenThrowsArgumentNullException()
    {
        // Arrange
        var sut = new VaultFileServiceFactory(mockRegistry.Object, tree, settings, TimeProvider.System);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Create(null!));
    }
}
