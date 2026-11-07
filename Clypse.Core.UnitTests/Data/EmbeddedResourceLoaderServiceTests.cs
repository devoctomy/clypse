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
    public async Task GivenCompressedResourceKey_AndResourceExists_WhenLoadCompressedHashedTextSetAsync_ThenEveryLineIsInTheSet()
    {
        // Arrange: the list starts with a byte order mark, has CRLF line breaks and no line break after the last line.
        var key = "Clypse.Core.UnitTests.Data.Lists.fruit.txt.gz";
        var sut = new EmbeddedResourceLoaderService();

        // Act
        var set = await sut.LoadCompressedHashedTextSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None);

        // Assert
        Assert.Equal(32, set.Count);
        Assert.True(set.Contains("apple"));
        Assert.True(set.Contains("starfruit"));
        Assert.False(set.Contains("Apple"));
        Assert.False(set.Contains("apple" + (char)13));
        Assert.False(set.Contains("potato"));
    }

    [Fact]
    public async Task GivenCompressedResourceKey_AndResourceExists_WhenLoadCompressedHashedTextSetAsyncTwiceAtOnce_ThenLoadedOnce_AndSetReturned()
    {
        // Arrange
        var key = "Clypse.Core.UnitTests.Data.Lists.fruit.txt.gz";
        var sut = new EmbeddedResourceLoaderService();

        // Act
        var first = sut.LoadCompressedHashedTextSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None);
        var second = sut.LoadCompressedHashedTextSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None);
        var sets = await Task.WhenAll(first, second);

        // Assert
        Assert.Same(sets[0], sets[1]);
        Assert.True(sets[0].Contains("starfruit"));
    }

    [Fact]
    public async Task GivenACancelledWait_WhenLoadCompressedHashedTextSetAsyncAgain_ThenTheLoadStillCompletes()
    {
        // Arrange
        var key = "Clypse.Core.UnitTests.Data.Lists.fruit.txt.gz";
        var sut = new EmbeddedResourceLoaderService();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // Act
        var wait = sut.LoadCompressedHashedTextSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            cancelled.Token);
        var set = await sut.LoadCompressedHashedTextSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        Assert.True(set.Contains("starfruit"));
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
    public async Task GivenResourceKey_AndResourceNotExists_WhenLoadCompressedHashedTextSetAsync_ThenExceptionThrown()
    {
        // Arrange
        var key = "Clypse.Core.UnitTests.Data.Lists.dummy.txt";
        var sut = new EmbeddedResourceLoaderService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.LoadCompressedHashedTextSetAsync(
            key,
            typeof(EmbeddedResourceLoaderServiceTests).Assembly,
            CancellationToken.None));
        Assert.Contains(key, exception.Message);
    }
}