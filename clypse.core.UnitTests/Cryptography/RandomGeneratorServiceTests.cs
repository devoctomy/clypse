using clypse.core.Cryptogtaphy;

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
}
