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
        var estimator = new Mock<IPasswordComplexityEstimatorService>().Object;
        Assert.Equal("estimator", Assert.Throws<ArgumentNullException>(() => new PasswordStrengthService(null!, mockWeak.Object, text.For<ClypseText>())).ParamName);
        Assert.Equal("weakPasswords", Assert.Throws<ArgumentNullException>(() => new PasswordStrengthService(estimator, null!, text.For<ClypseText>())).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new PasswordStrengthService(estimator, mockWeak.Object, null!)).ParamName);
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

    [Fact]
    public async Task GivenAKnownWeakPassword_WhenEstimatedCheckingKnownPasswords_ThenVeryWeak_AndDescribedAsCommon()
    {
        // Arrange: long and varied, so it would be very strong if it were not in the list.
        mockWeak.Setup(x => x.ContainsAsync("A1+dB2=hI3_lM4-p!!", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut();

        // Act
        var strength = await sut.EstimateCheckingKnownPasswordsAsync("A1+dB2=hI3_lM4-p!!");

        // Assert
        Assert.True(strength.IsKnownWeak);
        Assert.Equal(PasswordComplexityEstimation.VeryWeak, strength.Estimation);
        Assert.Equal("danger", strength.Tone);
        Assert.Equal("Very weak: it is a commonly used password.", sut.Describe(strength));
    }

    [Fact]
    public async Task GivenAPasswordNotInTheList_WhenEstimatedCheckingKnownPasswords_ThenAsWithoutTheList()
    {
        // Arrange
        mockWeak.Setup(x => x.ContainsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateSut();

        // Act
        var strength = await sut.EstimateCheckingKnownPasswordsAsync("A1+dB2=hI3_lM4-p!!");

        // Assert
        Assert.False(strength.IsKnownWeak);
        Assert.Equal(PasswordComplexityEstimation.VeryStrong, strength.Estimation);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GivenNoPassword_WhenEstimatedCheckingKnownPasswords_ThenNone_AndTheListNotChecked(string? password)
    {
        // Act
        var strength = await CreateSut().EstimateCheckingKnownPasswordsAsync(password);

        // Assert
        Assert.Same(PasswordStrength.None, strength);
        mockWeak.VerifyNoOtherCalls();
    }

    private PasswordStrengthService CreateSut() => new(new StandardWesternPasswordComplexityEstimatorService(mockWeak.Object), mockWeak.Object, text.For<ClypseText>());
}
