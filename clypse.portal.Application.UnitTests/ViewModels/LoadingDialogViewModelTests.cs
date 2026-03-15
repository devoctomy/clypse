using clypse.portal.Application.ViewModels;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class LoadingDialogViewModelTests
{
    private static LoadingDialogViewModel CreateSut() => new();

    [Fact]
    public void GivenNewInstance_WhenCheckingDefaults_ThenDefaultMessageIsLoading()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.Equal("Loading...", sut.Message);
    }

    [Fact]
    public void GivenInstance_WhenSettingMessage_ThenMessageIsUpdated()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Message = "Saving vault...";

        // Assert
        Assert.Equal("Saving vault...", sut.Message);
    }

    [Fact]
    public void GivenInstance_WhenSettingMessageToEmpty_ThenMessageIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Message = string.Empty;

        // Assert
        Assert.Equal(string.Empty, sut.Message);
    }

    [Fact]
    public void GivenInstance_WhenSettingMessageTwice_ThenLastValueIsKept()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Message = "First";
        sut.Message = "Second";

        // Assert
        Assert.Equal("Second", sut.Message);
    }
}
