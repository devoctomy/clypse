using clypse.core.Enums;
using clypse.core.Secrets;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Enums;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class SecretDialogViewModelTests
{
    private SecretDialogViewModel CreateSut() => new();

    [Fact]
    public void GivenNewInstance_WhenCheckingDefaults_ThenDefaultValuesAreCorrect()
    {
        // Act
        var sut = this.CreateSut();

        // Assert
        Assert.Null(sut.EditableSecret);
        Assert.Null(sut.SecretFields);
        Assert.False(sut.IsSaving);
        Assert.Equal(CrudDialogMode.Create, sut.Mode);
        Assert.Null(sut.OnSaveCallback);
        Assert.Null(sut.OnCancelCallback);
    }

    [Fact]
    public void GivenNewInstance_WhenGetModeIcon_ThenReturnsPersonBadge()
    {
        // Act
        var icon = SecretDialogViewModel.GetModeIcon();

        // Assert
        Assert.Equal("person-badge", icon);
    }

    [Theory]
    [InlineData(CrudDialogMode.Create, "Create Secret")]
    [InlineData(CrudDialogMode.Update, "Update Secret")]
    [InlineData(CrudDialogMode.View, "View Secret")]
    public void GivenMode_WhenGetModeTitle_ThenReturnsExpectedTitle(CrudDialogMode mode, string expectedTitle)
    {
        // Arrange
        var sut = this.CreateSut();
        sut.Mode = mode;

        // Act
        var title = sut.GetModeTitle();

        // Assert
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void GivenUnknownMode_WhenGetModeTitle_ThenReturnsFallback()
    {
        // Arrange
        var sut = this.CreateSut();
        sut.Mode = (CrudDialogMode)99;

        // Act
        var title = sut.GetModeTitle();

        // Assert
        Assert.Equal("Secret", title);
    }

    [Fact]
    public void GivenWebSecret_WhenInitializeForSecret_ThenEditableSecretAndFieldsAreSet()
    {
        // Arrange
        var sut = this.CreateSut();
        var secret = new WebSecret { Name = "MySecret" };

        // Act
        sut.InitializeForSecret(secret, CrudDialogMode.Update);

        // Assert
        Assert.NotNull(sut.EditableSecret);
        Assert.Equal("MySecret", sut.EditableSecret.Name);
        Assert.Equal(CrudDialogMode.Update, sut.Mode);
        Assert.NotNull(sut.SecretFields);
    }

    [Fact]
    public void GivenInitializedDialog_WhenClear_ThenStateIsReset()
    {
        // Arrange
        var sut = this.CreateSut();
        sut.InitializeForSecret(new WebSecret { Name = "MySecret" }, CrudDialogMode.Update);

        // Act
        sut.Clear();

        // Assert
        Assert.Null(sut.EditableSecret);
        Assert.Null(sut.SecretFields);
        Assert.False(sut.IsSaving);
    }

    [Fact]
    public void GivenInitializedWithWebSecret_WhenOnSecretTypeChangedToSameType_ThenSecretFieldsAreStillPopulated()
    {
        // Arrange
        var sut = this.CreateSut();
        sut.InitializeForSecret(new WebSecret(), CrudDialogMode.Create);

        // Act
        sut.OnSecretTypeChanged(SecretType.Web);

        // Assert
        Assert.NotNull(sut.EditableSecret);
        Assert.NotNull(sut.SecretFields);
    }

    [Fact]
    public void GivenNoEditableSecret_WhenOnSecretTypeChanged_ThenNoExceptionIsThrown()
    {
        // Arrange
        var sut = this.CreateSut();

        // Act & Assert (no exception)
        sut.OnSecretTypeChanged(SecretType.Web);
        Assert.Null(sut.EditableSecret);
    }

    [Fact]
    public async Task GivenCreateMode_AndCallback_WhenHandleSave_ThenCallbackIsInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        sut.InitializeForSecret(new WebSecret { Name = "TestSecret" }, CrudDialogMode.Create);
        Secret? savedSecret = null;
        sut.OnSaveCallback = s => { savedSecret = s; return Task.CompletedTask; };

        // Act
        await sut.HandleSaveCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(savedSecret);
        Assert.Equal("TestSecret", savedSecret.Name);
    }

    [Fact]
    public async Task GivenViewMode_WhenHandleSave_ThenCallbackIsNotInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        sut.InitializeForSecret(new WebSecret { Name = "TestSecret" }, CrudDialogMode.View);
        var called = false;
        sut.OnSaveCallback = _ => { called = true; return Task.CompletedTask; };

        // Act
        await sut.HandleSaveCommand.ExecuteAsync(null);

        // Assert
        Assert.False(called);
    }

    [Fact]
    public async Task GivenNoEditableSecret_WhenHandleSave_ThenCallbackIsNotInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        var called = false;
        sut.OnSaveCallback = _ => { called = true; return Task.CompletedTask; };

        // Act
        await sut.HandleSaveCommand.ExecuteAsync(null);

        // Assert
        Assert.False(called);
    }

    [Fact]
    public async Task GivenCancelCallback_WhenHandleCancel_ThenCallbackIsInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        var called = false;
        sut.OnCancelCallback = () => { called = true; return Task.CompletedTask; };

        // Act
        await sut.HandleCancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task GivenNoCancelCallback_WhenHandleCancel_ThenNoExceptionIsThrown()
    {
        // Arrange
        var sut = this.CreateSut();

        // Act & Assert (no exception)
        await sut.HandleCancelCommand.ExecuteAsync(null);
    }

    [Fact]
    public async Task GivenHandleSave_WhenCallbackIsExecuting_ThenIsSavingIsTrueAndResetAfter()
    {
        // Arrange
        var sut = this.CreateSut();
        sut.InitializeForSecret(new WebSecret(), CrudDialogMode.Create);
        var wasSavingDuringCallback = false;
        sut.OnSaveCallback = _ =>
        {
            wasSavingDuringCallback = sut.IsSaving;
            return Task.CompletedTask;
        };

        // Act
        await sut.HandleSaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(wasSavingDuringCallback);
        Assert.False(sut.IsSaving);
    }
}
