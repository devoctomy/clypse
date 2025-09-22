using clypse.core.Data;

namespace clypse.core.UnitTests.Data;

public class EmbeddedResorceLoaderServiceTests
{
    [Fact]
    public async Task GivenResourceKey_AndResourceExists_WhenLoadHashSetAsync_ThenResourceLoaded_AndHashSetReturned()
    {
        // Arrange
        var key = "clypse.core.UnitTests.Data.Lists.fruit.txt";
        var sut = new EmbeddedResorceLoaderService();

        // Act
        var hashSet = await sut.LoadHashSetAsync(
            key,
            typeof(EmbeddedResorceLoaderServiceTests).Assembly,
            CancellationToken.None);

        // Assert
        Assert.Equal(32, hashSet.Count);
        Assert.Contains("starfruit", hashSet);
    }

    [Fact]
    public async Task GivenResourceKey_AndResourceExists_WhenLoadHashSetAsyncTwice_ThenResourceLoaded_AndHashSetCached_AndHashSetReturned()
    {
        // Arrange
        var key = "clypse.core.UnitTests.Data.Lists.fruit.txt";
        var sut = new EmbeddedResorceLoaderService();

        // Act
        var hashSet1 = await sut.LoadHashSetAsync(
            key,
            typeof(EmbeddedResorceLoaderServiceTests).Assembly,
            CancellationToken.None);
        var hashSet2 = await sut.LoadHashSetAsync(
            key,
            typeof(EmbeddedResorceLoaderServiceTests).Assembly,
            CancellationToken.None);

        // Assert
        Assert.Equal(32, hashSet1.Count);
        Assert.Contains("starfruit", hashSet1);
        Assert.Equal(hashSet1, hashSet2);
        Assert.Equal(EmbeddedResorceLoaderService.CachedDictionaries[key], hashSet1);
    }

    [Fact]
    public async Task GivenCompressedResourceKey_AndResourceExists_WhenLoadCompressedHashSetAsync_ThenResourceLoaded_AndHashSetReturned()
    {
        // Arrange
        var key = "clypse.core.UnitTests.Data.Lists.fruit.txt.gz";
        var sut = new EmbeddedResorceLoaderService();

        // Act
        var hashSet = await sut.LoadCompressedHashSetAsync(
            key,
            typeof(EmbeddedResorceLoaderServiceTests).Assembly,
            CancellationToken.None);

        // Assert
        Assert.Equal(32, hashSet.Count);
        Assert.Contains("starfruit", hashSet);
    }

    [Fact]
    public async Task GivenCompressedResourceKey_AndResourceExists_WhenLoadCompressedHashSetAsyncTwice_ThenResourceLoaded_AndHashSetCached_AndHashSetReturned()
    {
        // Arrange
        var key = "clypse.core.UnitTests.Data.Lists.fruit.txt.gz";
        var sut = new EmbeddedResorceLoaderService();

        // Act
        var hashSet1 = await sut.LoadCompressedHashSetAsync(
            key,
            typeof(EmbeddedResorceLoaderServiceTests).Assembly,
            CancellationToken.None);
        var hashSet2 = await sut.LoadCompressedHashSetAsync(
            key,
            typeof(EmbeddedResorceLoaderServiceTests).Assembly,
            CancellationToken.None);

        // Assert
        Assert.Equal(32, hashSet1.Count);
        Assert.Contains("starfruit", hashSet1);
        Assert.Equal(hashSet1, hashSet2);
        Assert.Equal(EmbeddedResorceLoaderService.CachedDictionaries[key], hashSet1);
    }

    [Fact]
    public async Task GivenResourceKey_AndResourceNotExists_WhenLoadHashSetAsync_ThenExceptionThrown()
    {
        // Arrange
        var key = "clypse.core.UnitTests.Data.Lists.dummy.txt";
        var sut = new EmbeddedResorceLoaderService();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.LoadHashSetAsync(
            key,
            typeof(EmbeddedResorceLoaderServiceTests).Assembly,
            CancellationToken.None));
        Assert.Contains(key, exception.Message);
    }

    [Fact]
    public async Task GivenResourceKey_AndResourceNotExists_WhenLoadCompressedHashSetAsync_ThenExceptionThrown()
    {
        // Arrange
        var key = "clypse.core.UnitTests.Data.Lists.dummy.txt";
        var sut = new EmbeddedResorceLoaderService();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.LoadCompressedHashSetAsync(
            key,
            typeof(EmbeddedResorceLoaderServiceTests).Assembly,
            CancellationToken.None));
        Assert.Contains(key, exception.Message);
    }
}