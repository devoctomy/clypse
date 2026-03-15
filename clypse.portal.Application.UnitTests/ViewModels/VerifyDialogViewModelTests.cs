using clypse.core.Vault;
using clypse.portal.Application.ViewModels;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class VerifyDialogViewModelTests
{
    private static VerifyDialogViewModel CreateSut() => new();

    [Fact]
    public void GivenNewInstance_WhenCheckingDefaults_ThenDefaultValuesAreCorrect()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.Null(sut.Results);
        Assert.Null(sut.OnCloseCallback);
    }

    [Fact]
    public void GivenInstance_WhenSettingResults_ThenResultsAreStored()
    {
        // Arrange
        var sut = CreateSut();
        var results = new VaultVerifyResults
        {
            MissingSecrets = 1,
            MismatchedSecrets = 0,
        };

        // Act
        sut.Results = results;

        // Assert
        Assert.NotNull(sut.Results);
        Assert.Equal(1, sut.Results.MissingSecrets);
    }

    [Fact]
    public void GivenSuccessfulVerification_WhenCheckingResults_ThenSuccessIsTrue()
    {
        // Arrange
        var sut = CreateSut();
        sut.Results = new VaultVerifyResults
        {
            MissingSecrets = 0,
            MismatchedSecrets = 0,
        };

        // Assert
        Assert.True(sut.Results.Success);
    }

    [Fact]
    public void GivenFailedVerification_WhenCheckingResults_ThenSuccessIsFalse()
    {
        // Arrange
        var sut = CreateSut();
        sut.Results = new VaultVerifyResults
        {
            MissingSecrets = 2,
            MismatchedSecrets = 1,
        };

        // Assert
        Assert.False(sut.Results.Success);
    }

    [Fact]
    public void GivenInstance_WhenSettingOnCloseCallback_ThenCallbackIsStored()
    {
        // Arrange
        var sut = CreateSut();
        Func<Task> callback = () => Task.CompletedTask;

        // Act
        sut.OnCloseCallback = callback;

        // Assert
        Assert.Equal(callback, sut.OnCloseCallback);
    }

    [Fact]
    public void GivenInstance_WhenClearingResults_ThenResultsAreNull()
    {
        // Arrange
        var sut = CreateSut();
        sut.Results = new VaultVerifyResults();

        // Act
        sut.Results = null;

        // Assert
        Assert.Null(sut.Results);
    }
}
