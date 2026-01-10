namespace clypse.portal.setup.UnitTests;

public class SetupOptionsTests
{
    [Theory]
    [InlineData("base-url", "access-id", "secret-access-key", "region", "resource-prefix", "bob@hoskins.com", true)]
    [InlineData("", "access-id", "secret-access-key", "region", "resource-prefix", "bob@hoskins.com", true)]
    [InlineData("", "", "secret-access-key", "region", "resource-prefix", "bob@hoskins.com", false)]
    [InlineData("", "access-id", "", "region", "resource-prefix", "bob@hoskins.com", false)]
    [InlineData("", "access-id", "secret-access-key", "", "resource-prefix", "bob@hoskins.com", false)]
    [InlineData("", "access-id", "secret-access-key", "region", "", "bob@hoskins.com", false)]
    [InlineData("", "access-id", "secret-access-key", "region", "resource-prefix", "", false)]
    public void GivenValidOptions_WhenIsValid_ThenReturnsTrue(
        string baseUrl,
        string accessId,
        string secretAccessKey,
        string region,
        string resourcePrefix,
        string initialUserEmail,
        bool expectedIsValid)
    {
        // Arrange
        var sut = new SetupOptions
        {
            BaseUrl = baseUrl,
            AccessId = accessId,
            SecretAccessKey = secretAccessKey,
            Region = region,
            ResourcePrefix = resourcePrefix,
            InitialUserEmail = initialUserEmail
        };

        // Act
        var isValid = sut.IsValid();

        // Assert
        Assert.Equal(expectedIsValid, isValid);
    }
}
