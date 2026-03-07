using clypse.portal.Application.ViewModels;
using clypse.core.Password;
using clypse.core.Enums;
using Moq;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class PasswordFieldViewModelTests
{
    private readonly Mock<IPasswordComplexityEstimatorService> mockEstimator;

    public PasswordFieldViewModelTests()
    {
        this.mockEstimator = new Mock<IPasswordComplexityEstimatorService>();
        this.mockEstimator
            .Setup(e => e.EstimateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordComplexityEstimatorResults { ComplexityEstimation = PasswordComplexityEstimation.Strong });
    }

    private PasswordFieldViewModel CreateSut() => new(this.mockEstimator.Object);

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var sut = this.CreateSut();
        Assert.False(sut.ShowPassword);
        Assert.False(sut.ShowPasswordGenerator);
        Assert.Null(sut.Value);
    }

    [Fact]
    public void TogglePasswordVisibilityCommand_TogglesShowPassword()
    {
        var sut = this.CreateSut();
        Assert.False(sut.ShowPassword);

        sut.TogglePasswordVisibilityCommand.Execute(null);
        Assert.True(sut.ShowPassword);

        sut.TogglePasswordVisibilityCommand.Execute(null);
        Assert.False(sut.ShowPassword);
    }

    [Fact]
    public void ShowGeneratorCommand_SetsShowPasswordGeneratorTrue()
    {
        var sut = this.CreateSut();
        sut.ShowGeneratorCommand.Execute(null);
        Assert.True(sut.ShowPasswordGenerator);
    }

    [Fact]
    public void HideGeneratorCommand_SetsShowPasswordGeneratorFalse()
    {
        var sut = this.CreateSut();
        sut.ShowGeneratorCommand.Execute(null);

        sut.HideGeneratorCommand.Execute(null);

        Assert.False(sut.ShowPasswordGenerator);
    }

    [Fact]
    public async Task HandlePasswordGeneratedAsync_SetsValueAndHidesGenerator()
    {
        var sut = this.CreateSut();
        sut.ShowGeneratorCommand.Execute(null);
        string? receivedValue = null;
        sut.ValueChangedCallback = v => { receivedValue = v; return Task.CompletedTask; };

        await sut.HandlePasswordGeneratedAsync("newpassword");

        Assert.Equal("newpassword", sut.Value);
        Assert.Equal("newpassword", receivedValue);
        Assert.False(sut.ShowPasswordGenerator);
    }

    [Theory]
    [InlineData(PasswordComplexityEstimation.VeryWeak, "text-danger")]
    [InlineData(PasswordComplexityEstimation.Weak, "text-warning")]
    [InlineData(PasswordComplexityEstimation.Strong, "text-success")]
    [InlineData(PasswordComplexityEstimation.VeryStrong, "text-primary")]
    public async Task ComplexityColorClass_ReturnsCorrectClass(PasswordComplexityEstimation estimation, string expected)
    {
        this.mockEstimator
            .Setup(e => e.EstimateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordComplexityEstimatorResults { ComplexityEstimation = estimation });
        var sut = this.CreateSut();
        sut.Value = "TestPassword";
        await sut.OnPasswordChangedAsync();

        Assert.Equal(expected, sut.ComplexityColorClass);
    }

    [Theory]
    [InlineData(PasswordComplexityEstimation.VeryWeak, 20)]
    [InlineData(PasswordComplexityEstimation.Weak, 40)]
    [InlineData(PasswordComplexityEstimation.Medium, 60)]
    [InlineData(PasswordComplexityEstimation.Strong, 80)]
    [InlineData(PasswordComplexityEstimation.VeryStrong, 100)]
    public async Task ComplexityPercentage_ReturnsCorrectPercentage(PasswordComplexityEstimation estimation, int expected)
    {
        this.mockEstimator
            .Setup(e => e.EstimateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordComplexityEstimatorResults { ComplexityEstimation = estimation });
        var sut = this.CreateSut();
        sut.Value = "TestPassword";
        await sut.OnPasswordChangedAsync();

        Assert.Equal(expected, sut.ComplexityPercentage);
    }
}
