namespace clypse.portal.setup.UnitTests;

public class SetupOptionsTests
{
    [Theory]
    [InlineData("base-url", "access-id", "secret-access-key", "region", "resource-prefix", "bob@hoskins.com", false, true)]
    [InlineData("", "access-id", "secret-access-key", "region", "resource-prefix", "bob@hoskins.com", false, true)]
    [InlineData("", "", "secret-access-key", "region", "resource-prefix", "bob@hoskins.com", false, false)]
    [InlineData("", "access-id", "", "region", "resource-prefix", "bob@hoskins.com", false, false)]
    [InlineData("", "access-id", "secret-access-key", "", "resource-prefix", "bob@hoskins.com", false, false)]
    [InlineData("", "access-id", "secret-access-key", "region", "", "bob@hoskins.com", false, false)]
    [InlineData("", "access-id", "secret-access-key", "region", "resource-prefix", "", false, false)]
    [InlineData("", "access-id", "secret-access-key", "region", "resource-prefix", "", true, true)]
    public void GivenValidOptions_WhenIsValid_ThenReturnsTrue(
        string baseUrl,
        string accessId,
        string secretAccessKey,
        string region,
        string resourcePrefix,
        string initialUserEmail,
        bool isUpgrade,
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
        var isValid = sut.IsValid(isUpgrade);

        // Assert
        Assert.Equal(expectedIsValid, isValid);
    }

    [Fact]
    public void GivenSetupOptions_WhenNewPropertiesSet_ThenValuesAreCorrect()
    {
        // Arrange & Act
        var sut = new SetupOptions
        {
            EnableUpgradeMode = true,
            BuildPortal = true,
            ForceUpgrade = true,
            CloudFrontDistributionId = "E1234567890ABC"
        };

        // Assert
        Assert.True(sut.EnableUpgradeMode);
        Assert.True(sut.BuildPortal);
        Assert.True(sut.ForceUpgrade);
        Assert.Equal("E1234567890ABC", sut.CloudFrontDistributionId);
    }

    [Fact]
    public void GivenSetupOptions_WhenDefaultValues_ThenNewPropertiesHaveExpectedDefaults()
    {
        // Arrange & Act
        var sut = new SetupOptions();

        // Assert
        Assert.True(sut.InteractiveMode); // InteractiveMode defaults to true
        Assert.False(sut.EnableUpgradeMode);
        Assert.False(sut.BuildPortal);
        Assert.False(sut.ForceUpgrade);
        Assert.Equal(string.Empty, sut.CloudFrontDistributionId);
    }

    [Fact]
    public void GivenSetupOptions_WhenInteractiveModeSetToFalse_ThenInteractiveModeIsFalse()
    {
        // Arrange & Act
        var sut = new SetupOptions
        {
            InteractiveMode = false
        };

        // Assert
        Assert.False(sut.InteractiveMode);
    }
}
