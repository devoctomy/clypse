using System.Text.Json;
using System.Text.Json.Serialization;
using clypse.core.Base;
using clypse.core.Base.Exceptions;
using clypse.core.Secrets;

namespace clypse.core.UnitTests.Secrets;

public class WebSecretTests
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
    public void GivenNewSecret_WhenSerialise_ThenObjectSerialisedCorrectly()
    {
        // Arrange
        var sut = new WebSecret();

        // Act
        var jsonRaw = JsonSerializer.Serialize(sut, jsonSerializerOptions);
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
        var name = "Foobar";
        var description = "Hello World!";
        var userName = "BobHoskins";
        var emailAddress = "bob@hoskins.com";
        var websiteUrl = "https://web.foobar.com";
        var loginUrl = "https://login.foobar.com";
        var password = "password123";

        var sut = new WebSecret
        {
            Name = name,
            Description = description,
            UserName = userName,
            EmailAddress = emailAddress,
            WebsiteUrl = websiteUrl,
            LoginUrl = loginUrl,
            Password = password,
        };

        // Act
        var jsonRaw = JsonSerializer.Serialize(sut, jsonSerializerOptions);
        using JsonDocument doc = JsonDocument.Parse(jsonRaw);

        // Assert
        var dataKeys = doc.RootElement.GetProperty("Data");
        var allProperties = dataKeys.EnumerateObject().ToList();
        Assert.Equal(name, sut.Name);
        Assert.Equal(description, sut.Description);
        Assert.Equal(userName, sut.UserName);
        Assert.Equal(emailAddress, sut.EmailAddress);
        Assert.Equal(websiteUrl, sut.WebsiteUrl);
        Assert.Equal(loginUrl, sut.LoginUrl);
        Assert.Equal(password, sut.Password);
        Assert.Equal(11, allProperties.Count);
        Assert.Contains(allProperties, x => x.Name == "Id");
        Assert.Contains(allProperties, x => x.Name == "CreatedAt");
        Assert.Contains(allProperties, x => x.Name == "LastUpdatedAt");
        Assert.Contains(allProperties, x => x.Name == "SecretType");
        Assert.Contains(allProperties, x => x.Name == "Name");
        Assert.Contains(allProperties, x => x.Name == "Description");
        Assert.Contains(allProperties, x => x.Name == "EmailAddress");
        Assert.Contains(allProperties, x => x.Name == "WebsiteUrl");
        Assert.Contains(allProperties, x => x.Name == "LoginUrl");
        Assert.Contains(allProperties, x => x.Name == "Password");

        var validator = new ClypseObjectValidator(sut);
        validator.Validate();
    }

    [Fact]
    public void GivenSecret_WhenFromSecret_ThenWebSecretReturned()
    {
        // Arrange
        var secret = new Secret
        {
            Name = "Foobar",
            Description = "Hello World!",
        };

        // Act
        var webSecret = WebSecret.FromSecret(secret);

        // Assert
        Assert.NotNull(webSecret);
        Assert.Equal(secret.Id, webSecret.Id);
        Assert.Equal(secret.Name, webSecret.Name);
        Assert.Equal(secret.Description, webSecret.Description);
        Assert.Equal(secret.CreatedAt, webSecret.CreatedAt);
        Assert.Equal(secret.LastUpdatedAt, webSecret.LastUpdatedAt);
    }
}
