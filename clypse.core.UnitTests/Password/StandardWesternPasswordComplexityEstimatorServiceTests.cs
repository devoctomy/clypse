using clypse.core.Data;
using clypse.core.Enums;
using clypse.core.Password;

namespace clypse.core.UnitTests.Password;

public class StandardWesternPasswordComplexityEstimatorServiceTests
{
    [Theory]
    [InlineData("abcdefghijklmnop", 75)]
    [InlineData("AbcdBfghIjklMnop", 91)]
    [InlineData("A1cdB2ghI3klM4op", 95)]
    [InlineData("A1+dB2=hI3_lM4-p", 105)]
    [InlineData("A1+dB2=hI3_lM4-p!!", 118)]
    public void GivenPassword_WhenEstimateScore_ThenReturnsExpectedScore(
        string password,
        int estimatedScore)
    {
        // Arrange
        var mockEmbeddedResourceLoaderService = new Moq.Mock<IEmbeddedResorceLoaderService>();
        var sut = new StandardWesternPasswordComplexityEstimatorService(mockEmbeddedResourceLoaderService.Object);

        // Act
        var score = sut.EstimateEntropy(password);

        // Assert
        Assert.Equal(estimatedScore, Math.Round(score, 0));
    }

    [Theory]
    [InlineData("Ç", PasswordComplexityEstimation.Unknown)] // Not supported on Standard Western variant.
    [InlineData("", PasswordComplexityEstimation.None)]
    [InlineData("abcdefghijklmnop", PasswordComplexityEstimation.VeryWeak)]
    [InlineData("AbcdBfghIjklMnop", PasswordComplexityEstimation.Weak)]
    [InlineData("A1cdB2ghI3klM4op", PasswordComplexityEstimation.Medium)]
    [InlineData("A1+dB2=hI3_lM4-p", PasswordComplexityEstimation.Strong)]
    [InlineData("A1+dB2=hI3_lM4-p!!", PasswordComplexityEstimation.VeryStrong)]
    public async Task GivenPassword_WhenEstimate_ThenReturnsExpectedEstimation(
        string password,
        PasswordComplexityEstimation expectedEstimation)
    {
        // Arrange
        var mockEmbeddedResourceLoaderService = new Moq.Mock<IEmbeddedResorceLoaderService>();
        var sut = new StandardWesternPasswordComplexityEstimatorService(mockEmbeddedResourceLoaderService.Object);

        // Act
        var estimation = await sut.EstimateAsync(password, false, CancellationToken.None);

        // Assert
        Assert.Equal(expectedEstimation, estimation.ComplexityEstimation);
    }
}
