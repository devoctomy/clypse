using clypse.core.Enums;
using clypse.core.Extensions;
using clypse.core.Secrets;

namespace clypse.core.UnitTests.Extensions;

public class SecretExtensionsTests
{
    [Fact]
    public void GivenSecret_WhenGetOrderedSecretFields_ThenOrderedSecretFieldsReturned()
    {
        // Arrange
        var expectedOrder = new List<string>
        {
            "Name",
            "Description",
            "Tags",
            "Comments",
        };
        var sut = new Secret();

        // Act
        var result = sut.GetOrderedSecretFields();

        // Assert
        Assert.NotNull(result);
        var fieldNames = result.Keys.Select(v => v.Name).ToList();
        Assert.True(fieldNames.SequenceEqual(expectedOrder));
    }

    [Fact]
    public void GivenWebSecret_WhenGetOrderedSecretFields_ThenOrderedSecretFieldsReturned()
    {
        // Arrange
        var expectedOrder = new List<string>
        {
            "Name",
            "Description",
            "UserName",
            "EmailAddress",
            "WebsiteUrl",
            "LoginUrl",
            "Password",
            "Tags",
            "Comments",
        };
        var sut = new WebSecret();

        // Act
        var result = sut.GetOrderedSecretFields();

        // Assert
        Assert.NotNull(result);
        var fieldNames = result.Keys.Select(v => v.Name).ToList();
        Assert.True(fieldNames.SequenceEqual(expectedOrder));
    }

    [Fact]
    public void GivenAwsCredentials_WhenGetOrderedSecretFields_ThenOrderedSecretFieldsReturned()
    {
        // Arrange
        var expectedOrder = new List<string>
        {
            "Name",
            "Description",
            "AccessKeyId",
            "SecretAccessKey",
            "Tags",
            "Comments",
        };
        var sut = new AwsCredentials();

        // Act
        var result = sut.GetOrderedSecretFields();

        // Assert
        Assert.NotNull(result);
        var fieldNames = result.Keys.Select(v => v.Name).ToList();
        Assert.True(fieldNames.SequenceEqual(expectedOrder));
    }

    [Theory]
    [InlineData(SecretType.None, typeof(Secret))]
    [InlineData(SecretType.Web, typeof(WebSecret))]
    [InlineData(SecretType.Aws, typeof(AwsCredentials))]
    public void GivenSecret_WithSecretType_WhenCastSecretToCorrectType_ThenCorrectSecretTypeReturned(
        SecretType secretType,
        Type expectedSecretType)
    {
        // Arrange
        var sut = new Secret
        {
            SecretType = secretType,
        };

        // Act
        var result = sut.CastSecretToCorrectType();

        // Assert
        Assert.IsType(expectedSecretType, result);
    }
}
