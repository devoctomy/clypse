using Clypse.Core.Enums;
using Clypse.Core.Password;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class PasswordStrengthServiceTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IWeakKnownPasswordsService> mockWeak = new();

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("estimator", Assert.Throws<ArgumentNullException>(() => new PasswordStrengthService(null!, text.For<ClypseText>())).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new PasswordStrengthService(new Mock<IPasswordComplexityEstimatorService>().Object, null!)).ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GivenNoPassword_WhenEstimated_ThenNone(string? password)
    {
        // Act
        var strength = await CreateSut().EstimateAsync(password);

        // Assert
        Assert.Same(PasswordStrength.None, strength);
        Assert.Equal(0, strength.Percent);
    }

    [Theory]
    [InlineData("abcdefghijklmnop", PasswordComplexityEstimation.VeryWeak, "danger")]
    [InlineData("A1cdB2ghI3klM4op", PasswordComplexityEstimation.Medium, "warning")]
    [InlineData("A1+dB2=hI3_lM4-p!!", PasswordComplexityEstimation.VeryStrong, "success")]
    public async Task GivenAPassword_WhenEstimated_ThenItsStrengthAndTone(string password, PasswordComplexityEstimation expected, string tone)
    {
        // Act
        var strength = await CreateSut().EstimateAsync(password);

        // Assert
        Assert.Equal(expected, strength.Estimation);
        Assert.Equal(tone, strength.Tone);
        Assert.True(strength.Entropy > 0);
        mockWeak.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GivenAStrongPassword_WhenDescribed_ThenItsLevelAndBits()
    {
        // Arrange
        var sut = CreateSut();
        var strength = await sut.EstimateAsync("A1+dB2=hI3_lM4-p!!");

        // Act
        var description = sut.Describe(strength);

        // Assert
        Assert.Equal("Very strong, about 118 bits", description);
    }

    [Fact]
    public void GivenNoOrAnUnknownStrength_WhenDescribed_ThenNothingOrUnknown()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        Assert.Equal(string.Empty, sut.Describe(PasswordStrength.None));
        Assert.StartsWith("Strength unknown", sut.Describe(new PasswordStrength { Estimation = PasswordComplexityEstimation.Unknown }), StringComparison.Ordinal);
    }

    private PasswordStrengthService CreateSut() => new(new StandardWesternPasswordComplexityEstimatorService(mockWeak.Object), text.For<ClypseText>());
}
