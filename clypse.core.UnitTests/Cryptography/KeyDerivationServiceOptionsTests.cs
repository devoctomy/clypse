using clypse.core.Cryptography;

namespace clypse.core.UnitTests.Cryptography;

public class KeyDerivationServiceOptionsTests
{
    [Fact]
    public void GivenKey_AndKeyExists_AndValueIsString_WhenGetAsString_ThenStringReturned()
    {
        // Arrange
        var key = "foo";
        var value = "bar";
        var sut = new KeyDerivationServiceOptions();
        sut.Parameters.Add(key, value);

        // Act
        var result = sut.GetAsString(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void GivenKey_AndKeyExists_AndValueNotString_WhenGetAsString_ThenInvalidCastExceptionThrown()
    {
        // Arrange
        var key = "foo";
        var value = 69;
        var sut = new KeyDerivationServiceOptions();
        sut.Parameters.Add(key, value);

        // Act & Assert
        Assert.ThrowsAny<InvalidCastException>(() => sut.GetAsString(key));
    }

    [Fact]
    public void GivenKey_AndKeyNotExists_WhenGetAsString_ThenKeyNotFoundExceptionThrown()
    {
        // Arrange
        var key = "foo";
        var sut = new KeyDerivationServiceOptions();

        // Act & Assert
        Assert.ThrowsAny<KeyNotFoundException>(() => sut.GetAsString(key));
    }

    [Fact]
    public void GivenKey_AndKeyExists_AndValueIsInt_WhenGetAsInt_ThenStringReturned()
    {
        // Arrange
        var key = "foo";
        var value = 69;
        var sut = new KeyDerivationServiceOptions();
        sut.Parameters.Add(key, value);

        // Act
        var result = sut.GetAsInt(key);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void GivenKey_AndKeyExists_AndValueIsInt_WhenGetAsInt_ThenInvalidCastExceptionThrown()
    {
        // Arrange
        var key = "foo";
        var value = "bar";
        var sut = new KeyDerivationServiceOptions();
        sut.Parameters.Add(key, value);

        // Act & Assert
        Assert.ThrowsAny<InvalidCastException>(() => sut.GetAsInt(key));
    }

    [Fact]
    public void GivenKey_AndKeyNotExists_WhenGetAsInt_ThenKeyNotFoundExceptionThrown()
    {
        // Arrange
        var key = "foo";
        var sut = new KeyDerivationServiceOptions();

        // Act & Assert
        Assert.ThrowsAny<KeyNotFoundException>(() => sut.GetAsInt(key));
    }
}
