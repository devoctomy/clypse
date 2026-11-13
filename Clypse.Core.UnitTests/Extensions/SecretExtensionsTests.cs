using Clypse.Core.Enums;
using Clypse.Core.Extensions;
using Clypse.Core.Secrets;

namespace Clypse.Core.UnitTests.Extensions;

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

    [Fact]
    public void GivenSecretsOfEachKind_WhenGettingTheirFields_ThenOnlyPasswordsAndTheAwsSecretKeyAreSecretValues()
    {
        // Arrange
        var secrets = new Secret[] { new Secret(), new WebSecret(), new AwsCredentials() };

        // Act
        var secretValues = secrets
            .SelectMany(s => s.GetOrderedSecretFields()!)
            .Where(f => f.Value.IsSecretValue)
            .Select(f => f.Key.Name)
            .ToList();

        // Assert
        Assert.Equal(["Password", "SecretAccessKey"], secretValues);
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

    [Fact]
    public void GivenSecretOfAnUnknownType_WhenCastSecretToCorrectType_ThenNotSupportedExceptionThrown()
    {
        // Arrange
        var sut = new Secret
        {
            SecretType = (SecretType)99,
        };

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => sut.CastSecretToCorrectType());
    }

    [Fact]
    public void GivenAFieldWithNoDisplayOrder_WhenGetOrderedSecretFields_ThenItComesLast()
    {
        // Arrange
        var sut = new SecretWithUnorderedField();

        // Act
        var result = sut.GetOrderedSecretFields();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nameof(SecretWithUnorderedField.Unordered), result.Keys.Last().Name);
    }

    private sealed class SecretWithUnorderedField : Secret
    {
        [SecretField]
        public string? Unordered { get; set; }
    }
}
