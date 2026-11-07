using Clypse.Core.Configuration;
using Clypse.Core.Data;
using Clypse.Core.Password;
using Microsoft.Extensions.Options;
using Moq;

namespace Clypse.Core.UnitTests.Password;

public class WeakKnownPasswordsServiceTests
{
    [Theory]
    [InlineData("password123", true)]
    [InlineData("Tr0ub4dor&3", false)]
    public async Task GivenFullListOff_WhenContainsAsync_ThenStandInListChecked_AndEmbeddedListNotLoaded(
        string password,
        bool expected)
    {
        // Arrange
        var mockLoader = new Mock<IEmbeddedResourceLoaderService>();
        var options = Options.Create(new TextListOptions { UseFullWeakKnownPasswordsList = false });
        var sut = new WeakKnownPasswordsService(mockLoader.Object, options);

        // Act
        var contains = await sut.ContainsAsync(password, CancellationToken.None);

        // Assert
        Assert.Equal(expected, contains);
        mockLoader.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("hunter2", true)]
    [InlineData("password123", false)]
    public async Task GivenFullListOn_WhenContainsAsync_ThenEmbeddedListChecked(
        string password,
        bool expected)
    {
        // Arrange
        var mockLoader = new Mock<IEmbeddedResourceLoaderService>();
        mockLoader
            .Setup(x => x.LoadCompressedHashedTextSetAsync(
                ResourceKeys.CompressedWeakKnownPasswordsResourceKey,
                typeof(WeakKnownPasswordsService).Assembly,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashedTextSet([HashedTextSet.Hash("hunter2"u8)]));
        var options = Options.Create(new TextListOptions());
        var sut = new WeakKnownPasswordsService(mockLoader.Object, options);

        // Act
        var contains = await sut.ContainsAsync(password, CancellationToken.None);

        // Assert
        Assert.Equal(expected, contains);
    }

    [Fact]
    public async Task GivenTheEmbeddedList_WhenContainsAsyncWithAKnownWeakPassword_ThenTrue()
    {
        // Arrange
        var options = Options.Create(new TextListOptions());
        var sut = new WeakKnownPasswordsService(new EmbeddedResourceLoaderService(), options);

        // Act
        var contains = await sut.ContainsAsync("password", CancellationToken.None);

        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var loader = new Mock<IEmbeddedResourceLoaderService>().Object;
        var options = Options.Create(new TextListOptions());

        // Act & Assert
        Assert.Equal("embeddedResourceLoaderService", Assert.Throws<ArgumentNullException>(() => new WeakKnownPasswordsService(null!, options)).ParamName);
        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() => new WeakKnownPasswordsService(loader, null!)).ParamName);
    }
}
