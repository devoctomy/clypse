using Clypse.Core.Enums;
using Clypse.Core.Password;
using Moq;

namespace Clypse.Core.UnitTests.Password;

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
        var mockWeakKnownPasswordsService = new Moq.Mock<IWeakKnownPasswordsService>();
        var sut = new StandardWesternPasswordComplexityEstimatorService(mockWeakKnownPasswordsService.Object);

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
        var mockWeakKnownPasswordsService = new Moq.Mock<IWeakKnownPasswordsService>();
        var sut = new StandardWesternPasswordComplexityEstimatorService(mockWeakKnownPasswordsService.Object);

        // Act
        var estimation = await sut.EstimateAsync(password, false, CancellationToken.None);

        // Assert
        Assert.Equal(expectedEstimation, estimation.ComplexityEstimation);
    }

    [Fact]
    public async Task GivenWeakKnownPassword_AndCheckForPwnedPasswords_WhenEstimate_ThenVeryWeak()
    {
        // Arrange
        var mockWeakKnownPasswordsService = new Moq.Mock<IWeakKnownPasswordsService>();
        mockWeakKnownPasswordsService
            .Setup(x => x.ContainsAsync("A1+dB2=hI3_lM4-p!!", Moq.It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = new StandardWesternPasswordComplexityEstimatorService(mockWeakKnownPasswordsService.Object);

        // Act
        var estimation = await sut.EstimateAsync("A1+dB2=hI3_lM4-p!!", true, CancellationToken.None);

        // Assert
        Assert.Equal(PasswordComplexityEstimation.VeryWeak, estimation.ComplexityEstimation);
    }

    [Fact]
    public async Task GivenPassword_AndNotCheckForPwnedPasswords_WhenEstimate_ThenListNotChecked()
    {
        // Arrange
        var mockWeakKnownPasswordsService = new Moq.Mock<IWeakKnownPasswordsService>();
        var sut = new StandardWesternPasswordComplexityEstimatorService(mockWeakKnownPasswordsService.Object);

        // Act
        var estimation = await sut.EstimateAsync("A1+dB2=hI3_lM4-p!!", false, CancellationToken.None);

        // Assert
        Assert.Equal(PasswordComplexityEstimation.VeryStrong, estimation.ComplexityEstimation);
        mockWeakKnownPasswordsService.Verify(
            x => x.ContainsAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<CancellationToken>()),
            Moq.Times.Never);
    }

    [Fact]
    public void GivenEmptyPassword_WhenEstimateEntropy_ThenZeroReturned()
    {
        // Arrange
        var mockWeakKnownPasswordsService = new Moq.Mock<IWeakKnownPasswordsService>();
        var sut = new StandardWesternPasswordComplexityEstimatorService(mockWeakKnownPasswordsService.Object);

        // Act
        var result = sut.EstimateEntropy(string.Empty);

        // Assert
        Assert.Equal(0, result);
    }
}
