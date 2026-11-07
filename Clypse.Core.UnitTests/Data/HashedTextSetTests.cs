using System.Text;
using Clypse.Core.Data;

namespace Clypse.Core.UnitTests.Data;

public class HashedTextSetTests
{
    [Fact]
    public void GivenHashesInAnyOrder_WhenContains_ThenEveryItemFound_AndOthersNot()
    {
        // Arrange
        var items = new[] { "zebra", "apple", "mango", "kiwi" };
        var sut = new HashedTextSet(items.Select(i => HashedTextSet.Hash(Encoding.UTF8.GetBytes(i))).ToArray());

        // Act
        var found = items.Select(sut.Contains).ToList();

        // Assert
        Assert.All(found, Assert.True);
        Assert.False(sut.Contains("banana"));
        Assert.False(sut.Contains(string.Empty));
        Assert.Equal(4, sut.Count);
    }

    [Fact]
    public void GivenTextLongerThanTheStackBuffer_AndNotAscii_WhenContains_ThenFound()
    {
        // Arrange
        var text = new string('é', 300) + "ü";
        var sut = new HashedTextSet([HashedTextSet.Hash(Encoding.UTF8.GetBytes(text))]);

        // Act
        var contains = sut.Contains(text);

        // Assert
        Assert.True(contains);
        Assert.False(sut.Contains(text[..^1]));
    }

    [Fact]
    public void GivenNoHashes_WhenContains_ThenFalse()
    {
        // Arrange
        var sut = new HashedTextSet([]);

        // Act
        var contains = sut.Contains("anything");

        // Assert
        Assert.False(contains);
        Assert.Equal(0, sut.Count);
    }

    [Fact]
    public void GivenNull_WhenConstructingOrContains_ThenThrowsArgumentNullException()
    {
        // Arrange
        var sut = new HashedTextSet([]);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HashedTextSet(null!));
        Assert.Throws<ArgumentNullException>(() => sut.Contains(null!));
    }
}
