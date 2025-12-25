namespace clypse.portal.setup.UnitTests;

public class AwsServiceOptionsTests
{
    [Theory]
    [InlineData("base-url", "access-id", "secret-access-key", "region", "resource-prefix", true)]
    [InlineData("", "access-id", "secret-access-key", "region", "resource-prefix", true)]
    [InlineData("", "", "secret-access-key", "region", "resource-prefix", false)]
    [InlineData("", "access-id", "", "region", "resource-prefix", false)]
    [InlineData("", "access-id", "secret-access-key", "", "resource-prefix", false)]
    [InlineData("", "access-id", "secret-access-key", "region", "", false)]
    public void GivenValidOptions_WhenIsValid_ThenReturnsTrue(
        string baseUrl,
        string accessId,
        string secretAccessKey,
        string region,
        string resourcePrefix,
        bool expectedIsValid)
    {
        // Arrange
        var sut = new AwsServiceOptions
        {
            BaseUrl = baseUrl,
            AccessId = accessId,
            SecretAccessKey = secretAccessKey,
            Region = region,
            ResourcePrefix = resourcePrefix
        };

        // Act
        var isValid = sut.IsValid();

        // Assert
        Assert.Equal(expectedIsValid, isValid);
    }
}
