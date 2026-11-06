using Clypse.Portal.Application.Services;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class SecretServiceFactoryTests
{
    private readonly Mock<ISyncProfileRegistry> mockRegistry = new();

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("registry", Assert.Throws<ArgumentNullException>(() => new SecretServiceFactory(null!, TimeProvider.System)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() => new SecretServiceFactory(mockRegistry.Object, null!)).ParamName);
    }

    [Fact]
    public void GivenAVault_WhenCreating_ThenItsSecretsAreInItsProfilesFileStore()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("D");
        mockRegistry.Setup(x => x.GetFileStore(id)).Returns(new Mock<ISyncFileStore>().Object);
        var sut = new SecretServiceFactory(mockRegistry.Object, TimeProvider.System);

        // Act
        var service = sut.Create(new Vault { Id = id, Root = $"profiles/{id}/" });

        // Assert
        Assert.IsType<SecretService>(service);
        mockRegistry.Verify(x => x.GetFileStore(id), Times.Once);
    }

    [Fact]
    public void GivenNoVault_WhenCreating_ThenThrowsArgumentNullException()
    {
        // Arrange
        var sut = new SecretServiceFactory(mockRegistry.Object, TimeProvider.System);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Create(null!));
    }
}
