using System.Text.Json;
using System.Text.Json.Serialization;
using clypse.core.Base;
using clypse.core.Base.Exceptions;
using clypse.core.Enums;
using clypse.core.Json;
using clypse.core.Secrets;

namespace clypse.core.UnitTests.Secrets;

public class SecretTests
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new ()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new JElementToPrimativesConverter(),
        },
    };

    [Fact]
    public void GivenNewSecret_WhenSerialise_ThenObjectSerialisedCorrectly()
    {
        // Arrange
        var sut = new Secret();

        // Act
        var jsonRaw = JsonSerializer.Serialize(sut, this.jsonSerializerOptions);
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
            Description = "Hello World!",
            Comments = "These are some comments.",
        };

        // Act
        var jsonRaw = JsonSerializer.Serialize(sut, this.jsonSerializerOptions);
        using JsonDocument doc = JsonDocument.Parse(jsonRaw);

        // Assert
        var dataKeys = doc.RootElement.GetProperty("Data");
        var allProperties = dataKeys.EnumerateObject().ToList();
        Assert.Equal(7, allProperties.Count);
        Assert.Contains(allProperties, x => x.Name == "Id");
        Assert.Contains(allProperties, x => x.Name == "CreatedAt");
        Assert.Contains(allProperties, x => x.Name == "LastUpdatedAt");
        Assert.Contains(allProperties, x => x.Name == "SecretType");
        Assert.Contains(allProperties, x => x.Name == "Name");
        Assert.Contains(allProperties, x => x.Name == "Description");
        Assert.Contains(allProperties, x => x.Name == "Comments");

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
            Description = "Hello World!",
            Comments = "These are some comments.",
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
    public void GivenSecret_WhenRemoveExistingTag_ThenTagRemoved_AndTrueReturned()
    {
        // Arrange
        var sut = new Secret
        {
            Name = "Foobar",
            Description = "Hello World!",
            Comments = "These are some comments.",
        };
        var tag = "apple";
        sut.AddTag(tag);

        // Act
        var result = sut.RemoveTag(tag);

        // Assert
        Assert.True(result);
        Assert.Empty(sut.Tags);
    }

    [Fact]
    public void GivenSecret_WhenRemoveNonExistingTag_ThenTagNotRemoved_AndFalseReturned()
    {
        // Arrange
        var sut = new Secret
        {
            Name = "Foobar",
            Description = "Hello World!",
            Comments = "These are some comments.",
        };
        var tag = "apple";
        sut.AddTag(tag);

        // Act
        var result = sut.RemoveTag("banana");

        // Assert
        Assert.False(result);
        Assert.Single(sut.Tags);
        Assert.Equal(tag, sut.Tags[0]);
    }

    [Fact]
    public void GivenSecret_AndExistingTag_WhenAddTag_ThenTagNotAdded_AndFalseReturned()
    {
        // Arrange
        var sut = new Secret
        {
            Name = "Foobar",
            Description = "Hello World!",
            Comments = "These are some comments.",
        };
        var tag = "apple";
        sut.AddTag(tag);

        // Act
        var result = sut.AddTag(tag);

        // Assert
        Assert.False(result);
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
            Description = "Hello World!",
            Comments = "These are some comments.",
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
            Description = "Hello World!",
            Comments = "These are some comments.",
        };
        var tag = "apple";
        sut.AddTag(tag);

        // Act
        sut.ClearTags();

        // Assert
        Assert.Empty(sut.Tags);
    }

    [Fact]
    public void GivenDataDictionary_WhenFromDictionary_ThenSecretCreatedWithDataSet()
    {
        // Arrange
        var data = new Dictionary<string, string>
        {
            { "SecretType", "Web" },
            { "Name", "Foobar" },
            { "Description", "Hello World!" },
            { "Comments", "These are some comments." },
            { "Tags", "apple,orange,pear" },
        };

        // Act
        var sut = Secret.FromDictionary(data);

        // Assert
        Assert.Equal("Foobar", sut.Name);
        Assert.Equal("Hello World!", sut.Description);
        Assert.Equal("These are some comments.", sut.Comments);
        Assert.Equal(SecretType.Web, sut.SecretType);
        Assert.Equal(3, sut.Tags.Count);
        Assert.Contains(sut.Tags, x => x == "apple");
        Assert.Contains(sut.Tags, x => x == "orange");
        Assert.Contains(sut.Tags, x => x == "pear");
    }
}
