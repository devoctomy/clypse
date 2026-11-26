using System.Text.Json;
using Clypse.Core.Secrets;
using Clypse.Core.Secrets.Import;

namespace Clypse.Core.UnitTests.Secrets.Import;

public class JsonSecretsImporterServiceTests
{
    [Fact]
    public void GivenSecrets_WhenRead_ThenTheirFieldsAreKept_WithLineBreaks()
    {
        // Arrange
        var sut = new JsonSecretsImporterService();
        const string Json = """
            [
              {
                "SecretType": "Web",
                "Name": "Forum",
                "UserName": "quietowl",
                "Password": "qwerty",
                "Comments": "Line one\nLine two\r\nLine three",
                "Tags": "games,social",
                "CreatedAt": "01-01-2025T09:00:00",
                "LastUpdatedAt": "26-08-2026T10:00:00",
                "PasswordUpdatedAt": "26-05-2026T10:00:00"
              }
            ]
            """;

        // Act
        var rows = sut.Read(Json);

        // Assert
        var row = Assert.Single(rows);
        Assert.Equal(9, row.Count);
        Assert.Equal("Forum", row[nameof(Secret.Name)]);
        Assert.Equal("Line one\nLine two\r\nLine three", row[nameof(Secret.Comments)]);
        Assert.Equal("26-05-2026T10:00:00", row[nameof(WebSecret.PasswordUpdatedAt)]);
    }

    [Fact]
    public void GivenAnIdAndKeysThatAreNotFields_WhenRead_ThenTheyAreLeftOut()
    {
        // Arrange
        var sut = new JsonSecretsImporterService();
        const string Json = """[ { "Id": "6f1c2a8e-1111-4c3b-9d7e-1a2b3c4d5e6f", "Name": "Forum", "Hidden": "x", "Description": "" } ]""";

        // Act
        var rows = sut.Read(Json);

        // Assert
        var row = Assert.Single(rows);
        Assert.Equal(["Name"], row.Keys);
    }

    [Fact]
    public void GivenADateInAnotherFormat_WhenRead_ThenThrowsFormatException()
    {
        // Arrange
        var sut = new JsonSecretsImporterService();
        const string Json = """[ { "Name": "Forum", "LastUpdatedAt": "2026-08-26T10:00:00Z" } ]""";

        // Act & Assert
        Assert.Throws<FormatException>(() => sut.Read(Json));
    }

    [Theory]
    [InlineData("""{ "Name": "Forum" }""")]
    [InlineData("""[ { "Name": 42 } ]""")]
    [InlineData("""[ null ]""")]
    [InlineData("null")]
    [InlineData("\"Account\",\"Username\"")]
    public void GivenSomethingOtherThanAnArrayOfSecrets_WhenRead_ThenThrowsJsonException(string json)
    {
        // Arrange
        var sut = new JsonSecretsImporterService();

        // Act & Assert
        Assert.ThrowsAny<JsonException>(() => sut.Read(json));
    }

    [Fact]
    public void GivenNull_WhenRead_ThenThrowsArgumentNullException()
    {
        // Arrange
        var sut = new JsonSecretsImporterService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Read(null!));
    }
}
