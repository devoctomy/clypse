using clypse.core.Data;
using Moq;
using System.Reflection;

namespace clypse.core.UnitTests.Data;

public class SsoProvidersServiceTests
{
    [Fact]
    public async Task GivenInstance_WhenGetSsoProvidersAsync_ThenProvidersReturned()
    {
        // Arrange
        var mockEmbeddedResourceLoaderService = new Mock<IEmbeddedResorceLoaderService>();
        var sut = new SsoProvidersService(mockEmbeddedResourceLoaderService.Object);
        var hashSet = new HashSet<string>(["apple", "orange"]);

        mockEmbeddedResourceLoaderService.Setup(
            x => x.LoadHashSetAsync(
                It.Is<string>(y => y == ResourceKeys.SsoProvidersResourceKey),
                It.Is<Assembly>(y => y == typeof(IEmbeddedResorceLoaderService).Assembly),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hashSet);

        // Act
        var providers = await sut.GetSsoProvidersAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, providers.Count);
        Assert.Equal(hashSet.First(), providers[0]);
        Assert.Equal(hashSet.Last(), providers[1]);
    }
}
