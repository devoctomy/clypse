using Clypse.Core.Enums;
using Clypse.Portal.Models.Vaults;

namespace Clypse.Portal.Application.UnitTests.Models;

public class PasswordStrengthTests
{
    [Theory]
    [InlineData(PasswordComplexityEstimation.None, 0, "secondary")]
    [InlineData(PasswordComplexityEstimation.Unknown, 0, "secondary")]
    [InlineData(PasswordComplexityEstimation.VeryWeak, 15, "danger")]
    [InlineData(PasswordComplexityEstimation.Weak, 35, "danger")]
    [InlineData(PasswordComplexityEstimation.Medium, 60, "warning")]
    [InlineData(PasswordComplexityEstimation.Strong, 80, "success")]
    [InlineData(PasswordComplexityEstimation.VeryStrong, 100, "success")]
    public void GivenAnEstimation_WhenShown_ThenTheMeterIsThatFullInThatTone(PasswordComplexityEstimation estimation, int percent, string tone)
    {
        // Arrange
        var sut = new PasswordStrength { Estimation = estimation };

        // Act
        var actualPercent = sut.Percent;
        var actualTone = sut.Tone;

        // Assert
        Assert.Equal(percent, actualPercent);
        Assert.Equal(tone, actualTone);
    }
}
