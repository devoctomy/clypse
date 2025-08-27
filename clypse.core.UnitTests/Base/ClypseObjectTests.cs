using System.Text.Json;
using System.Text.Json.Serialization;
using clypse.core.Base;

namespace clypse.core.UnitTests.Base;

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
        var jsonRaw = JsonSerializer.Serialize(sut, jsonSerializerOptions);
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
        var jsonRaw = JsonSerializer.Serialize(sut, jsonSerializerOptions);
        var sut2 = JsonSerializer.Deserialize<ClypseObject>(jsonRaw);

        // Assert
        Assert.Equal(sut.Id, sut2!.Id);
        Assert.Equal(sut.CreatedAt, sut2!.CreatedAt);
        Assert.Equal(sut.LastUpdatedAt, sut2!.LastUpdatedAt);

        var validator = new ClypseObjectValidator(sut);
        validator.Validate();
    }
}
