using clypse.core.Secrets;
using clypse.core.Vault;
using clypse.portal.Application.Services;
using clypse.portal.Application.ViewModels;
using clypse.portal.Application.ViewModels.Messages;
using clypse.portal.Models.Enums;
using clypse.portal.Models.Import;
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

    // --- Constructor ---

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
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new CredentialsViewModel(null!, this.messenger));
    }

    [Fact]
    public void GivenNullMessenger_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new CredentialsViewModel(this.vaultStateService, null!));
    }

    // --- Initial state ---

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.False(sut.ShowImportDialog);
        Assert.False(sut.ShowSecretDialog);
        Assert.False(sut.ShowDeleteConfirmation);
        Assert.Empty(sut.SearchTerm);
        Assert.Empty(sut.FilteredEntries);
        Assert.Null(sut.CurrentSecret);
    }

    // --- ShowCreateDialog ---

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

    // --- CloseSecretDialog ---

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

    // --- ShowImportDialogInternal / CloseImportDialog ---

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

    // --- ShowDeleteConfirmationFor / CancelDeleteConfirmation ---

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

    // --- Messages ---

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

    // --- Search ---

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
                new VaultIndexEntry("id3", "Amazon AWS", "Cloud provider", null),
            ],
        };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);
        await sut.OnInitializedAsync();

        // Act
        sut.SearchTerm = "google";

        // Assert
        Assert.Single(sut.FilteredEntries);
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
                new VaultIndexEntry("id2", "GitHub Account", null, null),
            ],
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
    public void GivenVaultWithNullIndexEntries_WhenHandleSearch_ThenFilteredEntriesIsEmpty()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var vault = new VaultMetadata { Id = "v1", IndexEntries = null! };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        sut.SearchTerm = "anything";

        // Assert
        Assert.Empty(sut.FilteredEntries);
    }

    // --- VaultStateChanged ---

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

    // --- ViewSecretAsync / EditSecretAsync (ViewOrUpdateSecret) ---

    [Fact]
    public async Task GivenNoVaultState_WhenViewSecretAsync_ThenNothingHappens()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.ViewSecretCommand.ExecuteAsync("any-id");

        // Assert
        Assert.False(sut.ShowSecretDialog);
        Assert.False(sut.IsLoadingSecret);
    }

    [Fact]
    public async Task GivenVaultStateAndExistingSecret_WhenViewSecretAsync_ThenDialogShownInViewMode()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var secret = new WebSecret { Name = "test" };
        mockManager
            .Setup(m => m.GetSecretAsync(It.IsAny<IVault>(), "s1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secret);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        await sut.ViewSecretCommand.ExecuteAsync("s1");

        // Assert
        Assert.True(sut.ShowSecretDialog);
        Assert.Equal(CrudDialogMode.View, sut.SecretDialogMode);
        Assert.Equal(secret, sut.CurrentSecret);
        Assert.False(sut.IsLoadingSecret);
    }

    [Fact]
    public async Task GivenVaultStateAndExistingSecret_WhenEditSecretAsync_ThenDialogShownInUpdateMode()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var secret = new WebSecret { Name = "test" };
        mockManager
            .Setup(m => m.GetSecretAsync(It.IsAny<IVault>(), "s1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secret);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        await sut.EditSecretCommand.ExecuteAsync("s1");

        // Assert
        Assert.True(sut.ShowSecretDialog);
        Assert.Equal(CrudDialogMode.Update, sut.SecretDialogMode);
        Assert.False(sut.IsLoadingSecret);
    }

    [Fact]
    public async Task GivenVaultStateAndNullSecret_WhenViewSecretAsync_ThenDialogIsNotShown()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        mockManager
            .Setup(m => m.GetSecretAsync(It.IsAny<IVault>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Secret?)null);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        await sut.ViewSecretCommand.ExecuteAsync("missing");

        // Assert
        Assert.False(sut.ShowSecretDialog);
        Assert.False(sut.IsLoadingSecret);
    }

    [Fact]
    public async Task GivenManagerThrows_WhenViewSecretAsync_ThenIsLoadingSecretResetsToFalse()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        mockManager
            .Setup(m => m.GetSecretAsync(It.IsAny<IVault>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("storage error"));
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        await sut.ViewSecretCommand.ExecuteAsync("s1");

        // Assert
        Assert.False(sut.IsLoadingSecret);
    }

    // --- HandleSecretDialogSaveAsync ---

    [Fact]
    public async Task GivenViewMode_WhenHandleSecretDialogSave_ThenDialogIsClosed()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var secret = new WebSecret { Name = "view-me" };
        mockManager
            .Setup(m => m.GetSecretAsync(It.IsAny<IVault>(), "s1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secret);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);
        await sut.ViewSecretCommand.ExecuteAsync("s1");

        // Act
        await sut.HandleSecretDialogSaveCommand.ExecuteAsync(secret);

        // Assert
        Assert.False(sut.ShowSecretDialog);
    }

    [Fact]
    public async Task GivenNoVaultState_AndCreateMode_WhenHandleSecretDialogSave_ThenNothingHappens()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowCreateDialog();

        // Act
        await sut.HandleSecretDialogSaveCommand.ExecuteAsync(new WebSecret());

        // Assert
        Assert.False(sut.IsSavingSecret);
    }

    [Fact]
    public async Task GivenVaultState_AndCreateMode_WhenHandleSecretDialogSave_ThenSecretIsSavedAndDialogClosed()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var reloadedVault = new Vault(new VaultManifest(), new VaultInfo("v", "d"), new VaultIndex());
        mockManager
            .Setup(m => m.SaveAsync(It.IsAny<IVault>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VaultSaveResults());
        mockManager
            .Setup(m => m.LoadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reloadedVault);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);
        sut.ShowCreateDialog();

        // Act
        await sut.HandleSecretDialogSaveCommand.ExecuteAsync(new WebSecret { Name = "New" });

        // Assert
        Assert.False(sut.ShowSecretDialog);
        Assert.False(sut.IsSavingSecret);
    }

    [Fact]
    public async Task GivenVaultState_AndUpdateMode_WhenHandleSecretDialogSave_ThenSecretIsUpdatedAndDialogClosed()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var existingSecret = new WebSecret { Name = "Existing" };
        var reloadedVault = new Vault(new VaultManifest(), new VaultInfo("v", "d"), new VaultIndex());
        mockManager
            .Setup(m => m.GetSecretAsync(It.IsAny<IVault>(), "s1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSecret);
        mockManager
            .Setup(m => m.SaveAsync(It.IsAny<IVault>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VaultSaveResults());
        mockManager
            .Setup(m => m.LoadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reloadedVault);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);
        await sut.EditSecretCommand.ExecuteAsync("s1");

        // Act
        await sut.HandleSecretDialogSaveCommand.ExecuteAsync(existingSecret);

        // Assert
        Assert.False(sut.ShowSecretDialog);
        Assert.False(sut.IsSavingSecret);
    }

    // --- HandleImportSecretsAsync ---

    [Fact]
    public async Task GivenNoVaultState_WhenHandleImportSecrets_ThenNothingHappens()
    {
        // Arrange
        var sut = CreateSut();
        var result = new ImportResult { MappedSecrets = new List<Dictionary<string, string>>() };

        // Act
        await sut.HandleImportSecretsCommand.ExecuteAsync(result);

        // Assert
        Assert.False(sut.ShowImportDialog);
    }

    [Fact]
    public async Task GivenVaultStateAndSuccessfulImport_WhenHandleImportSecrets_ThenImportDialogIsClosed()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var reloadedVault = new Vault(new VaultManifest(), new VaultInfo("v", "d"), new VaultIndex());
        mockVault.Setup(v => v.AddRawSecrets(It.IsAny<IList<Dictionary<string, string>>>(), It.IsAny<clypse.core.Enums.SecretType>()))
            .Returns(true);
        mockManager
            .Setup(m => m.SaveAsync(It.IsAny<IVault>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VaultSaveResults());
        mockManager
            .Setup(m => m.LoadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reloadedVault);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);
        sut.ShowImportDialogInternal();
        var result = new ImportResult { MappedSecrets = new List<Dictionary<string, string>> { new() } };

        // Act
        await sut.HandleImportSecretsCommand.ExecuteAsync(result);

        // Assert
        Assert.False(sut.ShowImportDialog);
    }

    // --- HandleDeleteSecretAsync ---

    [Fact]
    public async Task GivenNoVaultState_WhenHandleDeleteSecret_ThenConfirmationIsCancelled()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowDeleteConfirmationFor("s1", "MySecret");

        // Act
        await sut.HandleDeleteSecretCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.ShowDeleteConfirmation);
        Assert.False(sut.IsDeletingSecret);
    }

    [Fact]
    public async Task GivenVaultStateAndSuccessfulDelete_WhenHandleDeleteSecret_ThenSecretIsDeletedAndConfirmationHidden()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var reloadedVault = new Vault(new VaultManifest(), new VaultInfo("v", "d"), new VaultIndex());
        mockVault.Setup(v => v.DeleteSecret("s1")).Returns(true);
        mockManager
            .Setup(m => m.SaveAsync(It.IsAny<IVault>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VaultSaveResults());
        mockManager
            .Setup(m => m.LoadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reloadedVault);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);
        sut.ShowDeleteConfirmationFor("s1", "Secret");

        // Act
        await sut.HandleDeleteSecretCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.ShowDeleteConfirmation);
        Assert.False(sut.IsDeletingSecret);
    }

    [Fact]
    public async Task GivenVaultStateAndDeleteNotFound_WhenHandleDeleteSecret_ThenIsDeletingSecretResetsToFalse()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        mockVault.Setup(v => v.DeleteSecret(It.IsAny<string>())).Returns(false);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);
        sut.ShowDeleteConfirmationFor("missing", "Not Found");

        // Act
        await sut.HandleDeleteSecretCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.IsDeletingSecret);
    }

    // --- Dispose ---

    [Fact]
    public void GivenInstance_WhenDisposed_ThenNoExceptionIsThrown()
    {
        // Arrange
        var sut = CreateSut();

        // Act / Assert
        sut.Dispose();
    }
}
