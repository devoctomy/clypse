using clypse.portal.Application.ViewModels;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class ConfirmDialogViewModelTests
{
    private ConfirmDialogViewModel CreateSut() => new();

    [Fact]
    public void GivenNewInstance_WhenCheckingDefaults_ThenDefaultValuesAreCorrect()
    {
        // Act
        var sut = this.CreateSut();

        // Assert
        Assert.False(sut.IsProcessing);
        Assert.Equal("Are you sure you want to delete this item?", sut.Message);
        Assert.Null(sut.OnConfirmCallback);
        Assert.Null(sut.OnCancelCallback);
    }

    [Fact]
    public void GivenInstance_WhenSettingMessage_ThenMessageIsUpdated()
    {
        // Arrange
        var sut = this.CreateSut();

        // Act
        sut.Message = "Delete vault 'MyVault'?";

        // Assert
        Assert.Equal("Delete vault 'MyVault'?", sut.Message);
    }

    [Fact]
    public void GivenInstance_WhenSettingIsProcessing_ThenIsProcessingIsUpdated()
    {
        // Arrange
        var sut = this.CreateSut();

        // Act
        sut.IsProcessing = true;

        // Assert
        Assert.True(sut.IsProcessing);
    }

    [Fact]
    public async Task GivenNotProcessing_AndCancelCallback_WhenHandleBackdropClick_ThenCallbackIsInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        var called = false;
        sut.OnCancelCallback = () => { called = true; return Task.CompletedTask; };

        // Act
        await sut.HandleBackdropClickCommand.ExecuteAsync(null);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task GivenIsProcessing_WhenHandleBackdropClick_ThenCallbackIsNotInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        sut.IsProcessing = true;
        var called = false;
        sut.OnCancelCallback = () => { called = true; return Task.CompletedTask; };

        // Act
        await sut.HandleBackdropClickCommand.ExecuteAsync(null);

        // Assert
        Assert.False(called);
    }

    [Fact]
    public async Task GivenNoCancelCallback_WhenHandleBackdropClick_ThenNoExceptionIsThrown()
    {
        // Arrange
        var sut = this.CreateSut();
        sut.OnCancelCallback = null;

        // Act & Assert (no exception)
        await sut.HandleBackdropClickCommand.ExecuteAsync(null);
    }

    [Fact]
    public void GivenInstance_WhenSettingOnConfirmCallback_ThenCallbackIsStored()
    {
        // Arrange
        var sut = this.CreateSut();
        Func<Task> callback = () => Task.CompletedTask;

        // Act
        sut.OnConfirmCallback = callback;

        // Assert
        Assert.Equal(callback, sut.OnConfirmCallback);
    }
}
