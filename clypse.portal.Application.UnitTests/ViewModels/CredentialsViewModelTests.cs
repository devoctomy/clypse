using clypse.core.Vault;
using clypse.portal.Application.Services;
using clypse.portal.Application.ViewModels;
using clypse.portal.Application.ViewModels.Messages;
using clypse.portal.Models.Enums;
using clypse.portal.Models.Vault;
using CommunityToolkit.Mvvm.Messaging;
using Moq;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class CredentialsViewModelTests
{
    private readonly VaultStateService vaultStateService;
    private readonly IMessenger messenger;

    public CredentialsViewModelTests()
    {
        this.vaultStateService = new VaultStateService();
        this.messenger = new WeakReferenceMessenger();
    }

    private CredentialsViewModel CreateSut()
    {
        return new CredentialsViewModel(this.vaultStateService, this.messenger);
    }

    [Fact]
    public void GivenValidParameters_WhenConstructing_ThenCreatesInstance()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public void GivenNullVaultStateService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CredentialsViewModel(null!, this.messenger));
    }

    [Fact]
    public void GivenNullMessenger_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CredentialsViewModel(this.vaultStateService, null!));
    }

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.False(sut.ShowImportDialog);
        Assert.False(sut.ShowSecretDialog);
        Assert.False(sut.ShowDeleteConfirmation);
        Assert.Empty(sut.SearchTerm);
        Assert.Empty(sut.FilteredEntries);
        Assert.Null(sut.CurrentSecret);
    }

    [Fact]
    public void GivenNewInstance_WhenShowCreateDialog_ThenDialogIsShown()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ShowCreateDialog();

        // Assert
        Assert.True(sut.ShowSecretDialog);
        Assert.NotNull(sut.CurrentSecret);
        Assert.Equal(CrudDialogMode.Create, sut.SecretDialogMode);
    }

    [Fact]
    public void GivenOpenDialog_WhenCloseSecretDialog_ThenDialogIsClosed()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowCreateDialog();

        // Act
        sut.CloseSecretDialogCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowSecretDialog);
        Assert.Null(sut.CurrentSecret);
    }

    [Fact]
    public void GivenNewInstance_WhenShowImportDialogInternal_ThenDialogIsShown()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ShowImportDialogInternal();

        // Assert
        Assert.True(sut.ShowImportDialog);
    }

    [Fact]
    public void GivenOpenImportDialog_WhenCloseImportDialog_ThenDialogIsClosed()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowImportDialogInternal();

        // Act
        sut.CloseImportDialogCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowImportDialog);
    }

    [Fact]
    public void GivenSecretId_WhenShowDeleteConfirmationFor_ThenConfirmationIsShown()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ShowDeleteConfirmationFor("secret-id", "My Secret");

        // Assert
        Assert.True(sut.ShowDeleteConfirmation);
        Assert.Contains("My Secret", sut.DeleteConfirmationMessage);
    }

    [Fact]
    public void GivenActiveConfirmation_WhenCancelDeleteConfirmation_ThenConfirmationIsHidden()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowDeleteConfirmationFor("secret-id", "My Secret");

        // Act
        sut.CancelDeleteConfirmationCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowDeleteConfirmation);
        Assert.Empty(sut.DeleteConfirmationMessage);
    }

    [Fact]
    public void GivenShowCreateCredentialMessage_WhenReceived_ThenDialogIsShown()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        this.messenger.Send(new ShowCreateCredentialMessage());

        // Assert
        Assert.True(sut.ShowSecretDialog);
        Assert.Equal(CrudDialogMode.Create, sut.SecretDialogMode);
    }

    [Fact]
    public void GivenShowImportMessage_WhenReceived_ThenImportDialogIsShown()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        this.messenger.Send(new ShowImportMessage());

        // Assert
        Assert.True(sut.ShowImportDialog);
    }

    [Fact]
    public async Task GivenVaultWithEntries_WhenSearchTermIsSet_ThenEntriesAreFiltered()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var vault = new VaultMetadata
        {
            Id = "test-id",
            Name = "Test",
            IndexEntries =
            [
                new VaultIndexEntry("id1", "Google Account", "My Google login", "google,email"),
                new VaultIndexEntry("id2", "GitHub Account", "Developer account", null),
                new VaultIndexEntry("id3", "Amazon AWS", "Cloud provider", null)
            ]
        };

        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Need to initialize the VM
        await sut.OnInitializedAsync();

        // Act
        sut.SearchTerm = "google";

        // Assert
        Assert.Single(sut.FilteredEntries); // Only "Google Account" matches by name and tags
    }

    [Fact]
    public async Task GivenFilteredSearch_WhenClearSearch_ThenAllEntriesAreShown()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var vault = new VaultMetadata
        {
            Id = "test-id",
            Name = "Test",
            IndexEntries =
            [
                new VaultIndexEntry("id1", "Google Account", null, null),
                new VaultIndexEntry("id2", "GitHub Account", null, null)
            ]
        };

        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);
        await sut.OnInitializedAsync();
        sut.SearchTerm = "google";

        // Act
        sut.ClearSearchCommand.Execute(null);

        // Assert
        Assert.Empty(sut.SearchTerm);
        Assert.Equal(2, sut.FilteredEntries.Count);
    }

    [Fact]
    public void GivenVaultStateChange_WhenVaultIsSet_ThenCurrentVaultIsUpdated()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var vault = new VaultMetadata { Id = "test-id", Name = "Test Vault" };

        // Act
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Assert
        Assert.Equal("test-id", sut.CurrentVault?.Id);
    }
}
