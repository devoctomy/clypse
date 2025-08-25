using clypse.core.Base;
using clypse.core.Base.Exceptions;
using clypse.core.Secrets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.core.UnitTests.Secrets;

public class SecretTests
{
    private readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    [Fact]
    public void GivenNewSecret_WhenSerialise_ThenObjectSerialisedCorrectly()
    {
        // Arrange
        var sut = new Secret();

        // Act
        var jsonRaw = JsonSerializer.Serialize(sut, JsonSerializerOptions);
        using JsonDocument doc = JsonDocument.Parse(jsonRaw);

        // Assert
        var dataKeys = doc.RootElement.GetProperty("Data");
        var allProperties = dataKeys.EnumerateObject().ToList();
        Assert.Equal(4, allProperties.Count);
        Assert.Contains(allProperties, x => x.Name == "Id");
        Assert.Contains(allProperties, x => x.Name == "CreatedAt");
        Assert.Contains(allProperties, x => x.Name == "LastUpdatedAt");
        Assert.Contains(allProperties, x => x.Name == "SecretType");

        var validator = new ClypseObjectValidator(sut);
        var exception = Assert.ThrowsAny<ClypseObjectValidatorException>(() =>
        {
            validator.Validate();
        });
        Assert.Single(exception.MissingProperties);
        Assert.Contains("Name", exception.MissingProperties);
    }

    [Fact]
    public void GivenNewSecret_AndName_AndDescription_WhenSerialise_ThenObjectSerialisedCorrectly()
    {
        // Arrange
        var sut = new Secret
        {
            Name = "Foobar",
            Description = "Hello World!"
        };

        // Act
        var jsonRaw = JsonSerializer.Serialize(sut, JsonSerializerOptions);
        using JsonDocument doc = JsonDocument.Parse(jsonRaw);

        // Assert
        var dataKeys = doc.RootElement.GetProperty("Data");
        var allProperties = dataKeys.EnumerateObject().ToList();
        Assert.Equal(6, allProperties.Count);
        Assert.Contains(allProperties, x => x.Name == "Id");
        Assert.Contains(allProperties, x => x.Name == "CreatedAt");
        Assert.Contains(allProperties, x => x.Name == "LastUpdatedAt");
        Assert.Contains(allProperties, x => x.Name == "SecretType");
        Assert.Contains(allProperties, x => x.Name == "Name");
        Assert.Contains(allProperties, x => x.Name == "Description");

        var validator = new ClypseObjectValidator(sut);
        validator.Validate();
    }

    [Fact]
    public void GivenSecret_AndNewTag_WhenAddTag_ThenTagAdded_AndTrueReturned()
    {
        // Arrange
        var sut = new Secret
        {
            Name = "Foobar",
            Description = "Hello World!"
        };
        var tag = "apple";

        // Act
        var result = sut.AddTag(tag);

        // Assert
        Assert.True(result);
        Assert.Single(sut.Tags);
        Assert.Equal(tag, sut.Tags[0]);
    }

    [Fact]
    public void GivenSecretWithTag_WhenUpdateTags_ThenTagsReplaced()
    {
        // Arrange
        var sut = new Secret
        {
            Name = "Foobar",
            Description = "Hello World!"
        };
        var tag = "apple";
        sut.AddTag(tag);

        var updateTags = new List<string>(["red", "green", "blue"]);

        // Act
        sut.UpdateTags(updateTags);

        // Assert
        Assert.Equal(updateTags.Count, sut.Tags.Count);
        Assert.Equal(string.Join(',', updateTags), string.Join(',', sut.Tags));
    }


    [Fact]
    public void GivenSecretWithTag_WhenClearTags_ThenTagsCleared()
    {
        // Arrange
        var sut = new Secret
        {
            Name = "Foobar",
            Description = "Hello World!"
        };
        var tag = "apple";
        sut.AddTag(tag);


        // Act
        sut.ClearTags();

        // Assert
        Assert.Empty(sut.Tags);
    }
}
