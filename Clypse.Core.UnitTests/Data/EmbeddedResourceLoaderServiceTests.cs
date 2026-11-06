using Clypse.Core.Data;

namespace Clypse.Core.UnitTests.Data;

public class EmbeddedResourceLoaderServiceTests
{
    [Fact]
    public async Task GivenResourceKey_AndResourceExists_WhenLoadHashSetAsync_ThenResourceLoaded_AndHashSetReturned()
    {
        // Arrange
        var key = "Clypse.Core.UnitTests.Data.Lists.fruit.txt";
        var sut = new EmbeddedResourceLoaderService();

        // Act
        var hashSet = await sut.LoadHashSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None);

        // Assert
        Assert.Equal(32, hashSet.Count);
        Assert.Contains("starfruit", hashSet);
    }

    [Fact]
    public async Task GivenResourceKey_AndResourceExists_WhenLoadHashSetAsyncTwice_ThenResourceLoaded_AndHashSetCached_AndHashSetReturned()
    {
        // Arrange
        var key = "Clypse.Core.UnitTests.Data.Lists.fruit.txt";
        var sut = new EmbeddedResourceLoaderService();

        // Act
        var hashSet1 = await sut.LoadHashSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None);
        var hashSet2 = await sut.LoadHashSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None);

        // Assert
        Assert.Equal(32, hashSet1.Count);
        Assert.Contains("starfruit", hashSet1);
        Assert.Same(hashSet1, hashSet2);
    }

    [Fact]
    public async Task GivenCompressedResourceKey_AndResourceExists_WhenLoadCompressedHashSetAsync_ThenResourceLoaded_AndHashSetReturned()
    {
        // Arrange
        var key = "Clypse.Core.UnitTests.Data.Lists.fruit.txt.gz";
        var sut = new EmbeddedResourceLoaderService();

        // Act
        var hashSet = await sut.LoadCompressedHashSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None);

        // Assert
        Assert.Equal(32, hashSet.Count);
        Assert.Contains("starfruit", hashSet);
    }

    [Fact]
    public async Task GivenCompressedResourceKey_AndResourceExists_WhenLoadCompressedHashSetAsyncTwice_ThenResourceLoaded_AndHashSetCached_AndHashSetReturned()
    {
        // Arrange
        var key = "Clypse.Core.UnitTests.Data.Lists.fruit.txt.gz";
        var sut = new EmbeddedResourceLoaderService();

        // Act
        var hashSet1 = await sut.LoadCompressedHashSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None);
        var hashSet2 = await sut.LoadCompressedHashSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None);

        // Assert
        Assert.Equal(32, hashSet1.Count);
        Assert.Contains("starfruit", hashSet1);
        Assert.Same(hashSet1, hashSet2);
    }

    [Fact]
    public async Task GivenResourceKey_AndResourceNotExists_WhenLoadHashSetAsync_ThenExceptionThrown()
    {
        // Arrange
        var key = "Clypse.Core.UnitTests.Data.Lists.dummy.txt";
        var sut = new EmbeddedResourceLoaderService();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.LoadHashSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None));
        Assert.Contains(key, exception.Message);
    }

    [Fact]
    public async Task GivenResourceKey_AndResourceNotExists_WhenLoadCompressedHashSetAsync_ThenExceptionThrown()
    {
        // Arrange
        var key = "Clypse.Core.UnitTests.Data.Lists.dummy.txt";
        var sut = new EmbeddedResourceLoaderService();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.LoadCompressedHashSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None));
        Assert.Contains(key, exception.Message);
    }
}