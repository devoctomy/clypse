using clypse.core.Cryptogtaphy;
using System;

namespace clypse.core.UnitTests.Cryptography;

public class RandomGeneratorServiceTests
{
    private readonly RandomGeneratorService sut;

    public RandomGeneratorServiceTests()
    {
        this.sut = new RandomGeneratorService();
    }

    [Fact]
    public void GivenNoParams_WhenGetRandomDouble100Times_ThenReturnsDifferentValues()
    {
        // Arrange
        var doubles = new List<double>();

        // Act
        for (var i = 0; i < 100; i++)
        {
            doubles.Add(this.sut.GetRandomDouble());
        }

        // Assert
        var distinctDoubles = doubles.Distinct().ToList();
        Assert.Equal(doubles.Count, distinctDoubles.Count);
    }

    [Fact]
    public void GivenMin_AndMax_WhenGetRandomInt500Times_ThenReturnsDifferentValues()
    {
        // Arrange
        var ints = new List<int>();
        var min = 0;
        var max = 20;

        // Act
        for (var i = 0; i < 500; i++)
        {
            ints.Add(this.sut.GetRandomInt(min, max));
        }

        // Assert
        var distinctDoubles = ints.Distinct().ToList();
        Assert.True(distinctDoubles.Count == max);
    }

    [Fact]
    public void GivenMin_AndMax_WhenGetRandomInt_ThenValueReturnedIsInRange()
    {
        // Arrange
        var min = 10;
        var max = 20;

        // Act
        var value = this.sut.GetRandomInt(min, max);

        // Assert
        Assert.InRange(value, min, max - 1);
    }

    [Fact]
    public void GivenLength_WhenGetRandomByteArray_ThenArrayReturnedIsOfCorrectLength()
    {
        // Arrange
        var length = 32;

        // Act
        var value = this.sut.GetRandomBytes(length);

        // Assert
        Assert.Equal(length, value.Length);
    }

    [Fact]
    public void GivenArray_WhenGetRandomArrayEntry_ThenValueReturnedIsFromArray()
    {
        // Arrange
        var array = new[]
        {
            "apple",
            "banana",
            "cherry",
            "date",
            "elderberry",
        };

        // Act
        var value = this.sut.GetRandomArrayEntry<string>(array);

        // Assert
        Assert.Contains(value, array);
    }

    [Fact]
    public void GivenArray_WhenGetRandomArrayEntry500Times_ThenReturnsDifferentValues()
    {
        // Arrange
        var values = new List<string>();
        var array = new[]
        {
            "apple",
            "banana",
            "cherry",
            "date",
            "elderberry",
            "fig",
            "grape",
            "honeydew",
            "kiwi",
            "lemon",
        };

        // Act
        for (var i = 0; i < 500; i++)
        {
            values.Add(this.sut.GetRandomArrayEntry<string>(array));
        }

        // Assert
        var distinctDoubles = values.Distinct().ToList();
        Assert.True(distinctDoubles.Count == array.Length);
    }

    [Fact]
    public void GivenLength_AndValidCharacters_WhenGetRandomStringContainingCharacters_ThenStringReturnedIsOfCorrectLengthAndContainsOnlyValidCharacters()
    {
        // Arrange
        var length = 16;
        var validCharacters = "abcdef0123456789";

        // Act
        var value = this.sut.GetRandomStringContainingCharacters(length, validCharacters);

        // Assert
        Assert.Equal(length, value.Length);
        foreach (var c in value)
        {
            Assert.Contains(c, validCharacters);
        }
    }

    [Fact]
    public void GivenDisposedRngService_WhenGetDouble_ThenObjectDisposedExceptionThrown()
    {
        // Arrange
        var sut = new RandomGeneratorService();
        sut.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
        {
            sut.GetRandomDouble();
        });
    }

    [Fact]
    public void GivenList_WhenRandomiseList_ThenReturnsRandomisedList()
    {
        // Arrange
        var length = 100;
        var list = new List<int>();
        for (var i = 0; i < length; i++)
        {
            list.Add(i);
        }

        // Act
        var randomisedList = this.sut.RandomiseList(list);

        // Assert
        Assert.Equal(length, randomisedList.Count);
        Assert.True(list.All(randomisedList.Contains));
        Assert.NotEqual(list, randomisedList);
    }
}
