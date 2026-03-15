using clypse.core.Vault;
using clypse.portal.Application.Services;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels;
using clypse.portal.Application.ViewModels.Messages;
using clypse.portal.Models.Aws;
using clypse.portal.Models.Vault;
using CommunityToolkit.Mvvm.Messaging;
using Moq;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class HomeViewModelTests : IDisposable
{
    private readonly Mock<IAuthenticationService> mockAuthService;
    private readonly Mock<INavigationService> mockNavigationService;
    private readonly Mock<IVaultManagerFactoryService> mockVaultManagerFactory;
    private readonly Mock<IVaultStorageService> mockVaultStorage;
    private readonly NavigationStateService navigationStateService;
    private readonly VaultStateService vaultStateService;
    private readonly Mock<IJsS3InvokerProvider> mockJsS3InvokerProvider;
    private readonly AwsS3Config awsS3Config;
    private readonly Mock<clypse.core.Cryptography.Interfaces.IKeyDerivationService> mockKeyDerivationService;
    private readonly IMessenger messenger;

    public HomeViewModelTests()
    {
        this.mockAuthService = new Mock<IAuthenticationService>();
        this.mockNavigationService = new Mock<INavigationService>();
        this.mockVaultManagerFactory = new Mock<IVaultManagerFactoryService>();
        this.mockVaultStorage = new Mock<IVaultStorageService>();
        this.navigationStateService = new NavigationStateService();
        this.vaultStateService = new VaultStateService();
        this.mockJsS3InvokerProvider = new Mock<IJsS3InvokerProvider>();
        this.awsS3Config = new AwsS3Config { Region = "us-east-1", BucketName = "test-bucket" };
        this.mockKeyDerivationService = new Mock<clypse.core.Cryptography.Interfaces.IKeyDerivationService>();
        this.messenger = WeakReferenceMessenger.Default;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.messenger.UnregisterAll(this);
    }

    private HomeViewModel CreateSut()
    {
        return new HomeViewModel(
            this.mockAuthService.Object,
            this.mockNavigationService.Object,
            this.mockVaultManagerFactory.Object,
            this.mockVaultStorage.Object,
            this.navigationStateService,
            this.vaultStateService,
            this.mockJsS3InvokerProvider.Object,
            this.awsS3Config,
            this.mockKeyDerivationService.Object,
            this.messenger);
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
    public void GivenNullAuthService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new HomeViewModel(
            null!,
            this.mockNavigationService.Object,
            this.mockVaultManagerFactory.Object,
            this.mockVaultStorage.Object,
            this.navigationStateService,
            this.vaultStateService,
            this.mockJsS3InvokerProvider.Object,
            this.awsS3Config,
            this.mockKeyDerivationService.Object,
            this.messenger));
    }

    // --- Initial state ---

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.Equal("vaults", sut.CurrentPage);
        Assert.False(sut.ShowVerifyDialog);
        Assert.Null(sut.VerifyResults);
        Assert.False(sut.ShowDeleteVaultDialog);
        Assert.Null(sut.VaultToDelete);
        Assert.False(sut.ShowCreateVaultDialog);
    }

    // --- OnAfterRenderAsync ---

    [Fact]
    public async Task GivenFirstRenderAndNotLoggedIn_WhenOnAfterRenderAsync_ThenNavigatesToLogin()
    {
        // Arrange
        var sut = CreateSut();
        this.mockAuthService.Setup(s => s.Initialize()).Returns(Task.CompletedTask);
        this.mockAuthService.Setup(s => s.CheckAuthentication()).ReturnsAsync(false);

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);

        // Assert
        this.mockNavigationService.Verify(n => n.NavigateTo("/login"), Times.Once);
    }

    [Fact]
    public async Task GivenFirstRenderAndLoggedIn_WhenOnAfterRenderAsync_ThenNavigationItemsAreSet()
    {
        // Arrange
        var sut = CreateSut();
        this.mockAuthService.Setup(s => s.Initialize()).Returns(Task.CompletedTask);
        this.mockAuthService.Setup(s => s.CheckAuthentication()).ReturnsAsync(true);

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);

        // Assert
        this.mockNavigationService.Verify(n => n.NavigateTo(It.IsAny<string>()), Times.Never);
        Assert.NotEmpty(this.navigationStateService.NavigationItems);
    }

    [Fact]
    public async Task GivenNotFirstRender_WhenOnAfterRenderAsync_ThenAuthIsNotChecked()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.OnAfterRenderAsync(firstRender: false);

        // Assert
        this.mockAuthService.Verify(s => s.Initialize(), Times.Never);
    }

    // --- HandleLockVaultAsync ---

    [Fact]
    public async Task GivenVaultIsLocked_WhenHandleLockVault_ThenPageChangesToVaults()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.HandleLockVaultCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("vaults", sut.CurrentPage);
    }

    // --- CloseVerifyDialog ---

    [Fact]
    public void GivenShowVerifyDialog_WhenCloseVerifyDialog_ThenDialogIsClosed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.CloseVerifyDialogCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowVerifyDialog);
        Assert.Null(sut.VerifyResults);
    }

    // --- CancelDeleteVault ---

    [Fact]
    public void GivenShowDeleteVaultDialog_WhenCancelDeleteVault_ThenDialogIsClosed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.CancelDeleteVaultCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowDeleteVaultDialog);
        Assert.Null(sut.VaultToDelete);
        Assert.Null(sut.DeleteVaultErrorMessage);
    }

    // --- CancelCreateVault ---

    [Fact]
    public void GivenShowCreateVaultDialog_WhenCancelCreateVault_ThenDialogIsClosed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.CancelCreateVaultCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowCreateVaultDialog);
        Assert.Null(sut.CreateVaultErrorMessage);
    }

    // --- HandleDeleteVaultConfirmAsync ---

    [Fact]
    public async Task GivenNoVaultState_WhenHandleDeleteVaultConfirm_ThenDeleteVaultDialogIsClosed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.HandleDeleteVaultConfirmCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.ShowDeleteVaultDialog);
        Assert.False(sut.IsDeletingVault);
    }

    [Fact]
    public async Task GivenVaultStateAndSuccessfulDelete_WhenHandleDeleteVaultConfirm_ThenPageChangesToVaults()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        mockManager
            .Setup(m => m.DeleteAsync(It.IsAny<IVault>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        this.mockVaultStorage
            .Setup(s => s.RemoveVaultAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        await sut.HandleDeleteVaultConfirmCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("vaults", sut.CurrentPage);
        Assert.False(sut.IsDeletingVault);
    }

    [Fact]
    public async Task GivenVaultStateAndDeleteThrows_WhenHandleDeleteVaultConfirm_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        mockManager
            .Setup(m => m.DeleteAsync(It.IsAny<IVault>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("delete failed"));
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        await sut.HandleDeleteVaultConfirmCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.DeleteVaultErrorMessage);
        Assert.False(sut.IsDeletingVault);
    }

    // --- HandleCreateVaultFromDialogAsync ---

    [Fact]
    public async Task GivenNullStoredCredentials_WhenHandleCreateVaultFromDialog_ThenDialogIsClosed()
    {
        // Arrange
        var sut = CreateSut();
        this.mockAuthService
            .Setup(s => s.GetStoredCredentials())
            .ReturnsAsync((Models.Aws.StoredCredentials?)null);
        var request = new VaultCreationRequest { Name = "v", Description = "d", Passphrase = "p" };

        // Act
        await sut.HandleCreateVaultFromDialogCommand.ExecuteAsync(request);

        // Assert
        Assert.False(sut.ShowCreateVaultDialog);
        Assert.False(sut.IsCreatingVault);
    }

    [Fact]
    public async Task GivenGetStoredCredentialsThrows_WhenHandleCreateVaultFromDialog_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = CreateSut();
        this.mockAuthService
            .Setup(s => s.GetStoredCredentials())
            .ThrowsAsync(new Exception("credentials error"));
        var request = new VaultCreationRequest { Name = "v", Description = "d", Passphrase = "p" };

        // Act
        await sut.HandleCreateVaultFromDialogCommand.ExecuteAsync(request);

        // Assert
        Assert.NotNull(sut.CreateVaultErrorMessage);
        Assert.False(sut.IsCreatingVault);
    }

    // --- HandleVerifyAsync (via "verify" navigation action) ---

    [Fact]
    public async Task GivenNoVaultState_WhenVerifyAction_ThenShowVerifyDialogRemainsHidden()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        this.navigationStateService.RequestNavigationAction("verify");
        await Task.Delay(100);

        // Assert
        Assert.False(sut.ShowVerifyDialog);
    }

    [Fact]
    public async Task GivenVaultStateAndSuccessfulVerify_WhenVerifyAction_ThenVerifyResultsAreSet()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var verifyResults = new VaultVerifyResults { MissingSecrets = 0, MismatchedSecrets = 0 };
        mockManager
            .Setup(m => m.VerifyAsync(It.IsAny<IVault>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(verifyResults);
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        this.navigationStateService.RequestNavigationAction("verify");
        await Task.Delay(100);

        // Assert
        Assert.NotNull(sut.VerifyResults);
        Assert.True(sut.VerifyResults.Success);
    }

    [Fact]
    public async Task GivenVaultStateAndVerifyThrows_WhenVerifyAction_ThenShowVerifyDialogIsFalse()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        mockManager
            .Setup(m => m.VerifyAsync(It.IsAny<IVault>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("verify failed"));
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        this.navigationStateService.RequestNavigationAction("verify");
        await Task.Delay(100);

        // Assert
        Assert.False(sut.ShowVerifyDialog);
    }

    // --- ShowDeleteVaultDialogInternal (via "delete-vault" action) ---

    [Fact]
    public async Task GivenNoCurrentVault_WhenDeleteVaultAction_ThenShowDeleteVaultDialogIsFalse()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        this.navigationStateService.RequestNavigationAction("delete-vault");
        await Task.Delay(100);

        // Assert
        Assert.False(sut.ShowDeleteVaultDialog);
    }

    [Fact]
    public async Task GivenCurrentVault_WhenDeleteVaultAction_ThenShowDeleteVaultDialogIsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        this.navigationStateService.RequestNavigationAction("delete-vault");
        await Task.Delay(100);

        // Assert
        Assert.True(sut.ShowDeleteVaultDialog);
        Assert.Equal(vault, sut.VaultToDelete);
    }

    // --- ShowCreateVaultDialogInternal (via "create-vault" action) ---

    [Fact]
    public async Task GivenNewInstance_WhenCreateVaultAction_ThenShowCreateVaultDialogIsTrue()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        this.navigationStateService.RequestNavigationAction("create-vault");
        await Task.Delay(100);

        // Assert
        Assert.True(sut.ShowCreateVaultDialog);
    }

    // --- GetNavigationItems for credentials page ---

    [Fact]
    public async Task GivenVaultStateSet_WhenVaultStateChanged_ThenPageChangesToCredentials()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var vault = new VaultMetadata { Id = "v1" };

        // Act
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);
        await Task.Delay(50);

        // Assert
        Assert.Equal("credentials", sut.CurrentPage);
        Assert.Contains(this.navigationStateService.NavigationItems, i => i.Action == "create-credential");
    }

    [Fact]
    public async Task GivenCredentialsPageAndVaultCleared_WhenVaultStateChanged_ThenPageChangesToVaults()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        var vault = new VaultMetadata { Id = "v1" };
        this.vaultStateService.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);
        await Task.Delay(50);
        Assert.Equal("credentials", sut.CurrentPage);

        // Act
        this.vaultStateService.ClearVaultState();
        await Task.Delay(50);

        // Assert
        Assert.Equal("vaults", sut.CurrentPage);
        Assert.Contains(this.navigationStateService.NavigationItems, i => i.Action == "create-vault");
    }

    // --- show-vaults navigation action ---

    [Fact]
    public async Task GivenCredentialsPage_WhenShowVaultsAction_ThenPageChangesToVaults()
    {
        // Arrange
        var sut = CreateSut();
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        this.vaultStateService.SetVaultState(new VaultMetadata { Id = "v1" }, "key", mockVault.Object, mockManager.Object);
        await Task.Delay(50);
        Assert.Equal("credentials", sut.CurrentPage);

        // Act
        this.navigationStateService.RequestNavigationAction("show-vaults");
        await Task.Delay(100);

        // Assert
        Assert.Equal("vaults", sut.CurrentPage);
    }

    // --- Refresh ---

    [Fact]
    public async Task GivenRefreshAction_WhenNavigationActionRequested_ThenRefreshVaultsMessageIsSent()
    {
        // Arrange
        var sut = CreateSut();
        var messageReceived = false;
        this.messenger.Register<RefreshVaultsMessage>(this, (_, _) => messageReceived = true);

        // Act
        this.navigationStateService.RequestNavigationAction("refresh");
        await Task.Delay(100);

        // Assert
        Assert.True(messageReceived);
    }

    [Fact]
    public async Task GivenCreateCredentialAction_WhenNavigationActionRequested_ThenMessageIsSent()
    {
        // Arrange
        var sut = CreateSut();
        var messageReceived = false;
        this.messenger.Register<ShowCreateCredentialMessage>(this, (_, _) => messageReceived = true);

        // Act
        this.navigationStateService.RequestNavigationAction("create-credential");
        await Task.Delay(100);

        // Assert
        Assert.True(messageReceived);
    }

    [Fact]
    public async Task GivenImportAction_WhenNavigationActionRequested_ThenMessageIsSent()
    {
        // Arrange
        var sut = CreateSut();
        var messageReceived = false;
        this.messenger.Register<ShowImportMessage>(this, (_, _) => messageReceived = true);

        // Act
        this.navigationStateService.RequestNavigationAction("import");
        await Task.Delay(100);

        // Assert
        Assert.True(messageReceived);
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
