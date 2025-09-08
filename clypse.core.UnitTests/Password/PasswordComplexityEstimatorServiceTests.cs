using clypse.core.Enums;
using clypse.core.Password;

namespace clypse.core.UnitTests.Password;

public class PasswordComplexityEstimatorServiceTests
{
    [Theory]
    [InlineData("abcefg", 28)]
    [InlineData("abcEFG", 34)]
    [InlineData("abcEFG123", 54)]
    [InlineData("!&^abcEFG123", 79)]
    public void GivenPassword_WhenEstimateScore_ThenReturnsExpectedScore(
        string password,
        int estimatedScore)
    {
        // Arrange
        var service = new PasswordComplexityEstimatorService();

        // Act
        var score = service.EstimateEntropy(password);

        // Assert
        Assert.Equal(estimatedScore, Math.Round(score, 0));
    }

    [Theory]
    [InlineData("Ç", PasswordComplexityEstimation.Unknown)] // Currently we do not support extended characters
    [InlineData("", PasswordComplexityEstimation.None)]
    [InlineData("abcefg", PasswordComplexityEstimation.VeryWeak)]
    [InlineData("abcEFG", PasswordComplexityEstimation.Weak)]
    [InlineData("abcEFG1", PasswordComplexityEstimation.Medium)]
    [InlineData("abcEFG123", PasswordComplexityEstimation.Strong)]
    [InlineData("!&^abcEFG123", PasswordComplexityEstimation.VeryStrong)]
    public void GivenPassword_WhenEstimate_ThenReturnsExpectedEstimation(
        string password,
        PasswordComplexityEstimation expectedEstimation)
    {
        // Arrange
        var service = new PasswordComplexityEstimatorService();

        // Act
        var estimation = service.Estimate(password);

        // Assert
        Assert.Equal(expectedEstimation, estimation);
    }
}
