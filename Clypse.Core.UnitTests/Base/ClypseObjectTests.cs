using System.Text.Json;
using System.Text.Json.Serialization;
using Clypse.Core.Base;

namespace Clypse.Core.UnitTests.Base;

public class ClypseObjectTests
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new ()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };

    [Fact]
    public void GivenNewClypseObject_WhenSerialise_ThenObjectSerialisedCorrectly()
    {
        // Arrange
        var sut = new ClypseObject();

        // Act
        var jsonRaw = JsonSerializer.Serialize(sut, this.jsonSerializerOptions);
        using JsonDocument doc = JsonDocument.Parse(jsonRaw);

        // Assert
        var dataKeys = doc.RootElement.GetProperty("Data");
        var allProperties = dataKeys.EnumerateObject().ToList();
        Assert.Contains(allProperties, x => x.Name == "Id");
        Assert.Contains(allProperties, x => x.Name == "CreatedAt");
        Assert.Contains(allProperties, x => x.Name == "LastUpdatedAt");

        var validator = new ClypseObjectValidator(sut);
        validator.Validate();
    }

    [Fact]
    public void GivenNewClypseObject_WhenSerialise_AndDeserialize_ThenObjectDeserialisedCorrectly()
    {
        // Arrange
        var sut = new ClypseObject();

        // Act
        var jsonRaw = JsonSerializer.Serialize(sut, this.jsonSerializerOptions);
        var sut2 = JsonSerializer.Deserialize<ClypseObject>(jsonRaw);

        // Assert
        Assert.Equal(sut.Id, sut2!.Id);
        Assert.Equal(sut.CreatedAt, sut2!.CreatedAt);
        Assert.Equal(sut.LastUpdatedAt, sut2!.LastUpdatedAt);

        var validator = new ClypseObjectValidator(sut);
        validator.Validate();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GivenNoValueForANewKey_WhenSetData_ThenNothingIsStored(string? value)
    {
        // Arrange
        var sut = new ClypseObject();

        // Act
        sut.SetData("Name", value);

        // Assert
        Assert.False(sut.Data.ContainsKey("Name"));
        Assert.Null(sut.GetData("Name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GivenNoValueForAnExistingKey_WhenSetData_ThenTheKeyIsRemoved(string? value)
    {
        // Arrange
        var sut = new ClypseObject();
        sut.SetData("Name", "Gmail");

        // Act
        sut.SetData("Name", value);

        // Assert
        Assert.False(sut.Data.ContainsKey("Name"));
    }

    [Fact]
    public void GivenAValue_WhenSetData_ThenItIsStored()
    {
        // Arrange
        var sut = new ClypseObject();

        // Act
        sut.SetData("Name", "Gmail");

        // Assert
        Assert.Equal("Gmail", sut.GetData("Name"));
    }
}
