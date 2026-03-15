using System.Text.Json;
using clypse.core.Vault;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels;
using clypse.portal.Application.ViewModels.Messages;
using clypse.portal.Models.Aws;
using clypse.portal.Models.Vault;
using CommunityToolkit.Mvvm.Messaging;
using Moq;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class VaultsViewModelTests : IDisposable
{
    private readonly Mock<IVaultStorageService> mockVaultStorage;
    private readonly Mock<IVaultManagerBootstrapperFactoryService> mockBootstrapperFactory;
    private readonly Mock<ILocalStorageService> mockLocalStorage;
    private readonly Mock<IVaultStateService> mockVaultStateService;
    private readonly Mock<IJsS3InvokerProvider> mockJsS3InvokerProvider;
    private readonly AwsS3Config awsS3Config;
    private readonly IMessenger messenger;

    public VaultsViewModelTests()
    {
        this.mockVaultStorage = new Mock<IVaultStorageService>();
        this.mockBootstrapperFactory = new Mock<IVaultManagerBootstrapperFactoryService>();
        this.mockLocalStorage = new Mock<ILocalStorageService>();
        this.mockVaultStateService = new Mock<IVaultStateService>();
        this.mockJsS3InvokerProvider = new Mock<IJsS3InvokerProvider>();
        this.awsS3Config = new AwsS3Config { Region = "us-east-1", BucketName = "test-bucket" };
        this.messenger = new WeakReferenceMessenger();
    }

    private VaultsViewModel CreateSut()
    {
        return new VaultsViewModel(
            this.mockVaultStorage.Object,
            this.mockBootstrapperFactory.Object,
            this.mockLocalStorage.Object,
            this.mockVaultStateService.Object,
            this.mockJsS3InvokerProvider.Object,
            this.awsS3Config,
            this.messenger);
    }

    // ─── Constructor ──────────────────────────────────────────────────────────

    [Fact]
    public void GivenValidParameters_WhenConstructing_ThenCreatesInstance()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public void GivenNullVaultStorage_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(
            null!,
            this.mockBootstrapperFactory.Object,
            this.mockLocalStorage.Object,
            this.mockVaultStateService.Object,
            this.mockJsS3InvokerProvider.Object,
            this.awsS3Config,
            this.messenger));
    }

    [Fact]
    public void GivenNullBootstrapperFactory_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(
            this.mockVaultStorage.Object,
            null!,
            this.mockLocalStorage.Object,
            this.mockVaultStateService.Object,
            this.mockJsS3InvokerProvider.Object,
            this.awsS3Config,
            this.messenger));
    }

    [Fact]
    public void GivenNullLocalStorageService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(
            this.mockVaultStorage.Object,
            this.mockBootstrapperFactory.Object,
            null!,
            this.mockVaultStateService.Object,
            this.mockJsS3InvokerProvider.Object,
            this.awsS3Config,
            this.messenger));
    }

    [Fact]
    public void GivenNullVaultStateService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(
            this.mockVaultStorage.Object,
            this.mockBootstrapperFactory.Object,
            this.mockLocalStorage.Object,
            null!,
            this.mockJsS3InvokerProvider.Object,
            this.awsS3Config,
            this.messenger));
    }

    [Fact]
    public void GivenNullJsS3InvokerProvider_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(
            this.mockVaultStorage.Object,
            this.mockBootstrapperFactory.Object,
            this.mockLocalStorage.Object,
            this.mockVaultStateService.Object,
            null!,
            this.awsS3Config,
            this.messenger));
    }

    [Fact]
    public void GivenNullMessenger_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(
            this.mockVaultStorage.Object,
            this.mockBootstrapperFactory.Object,
            this.mockLocalStorage.Object,
            this.mockVaultStateService.Object,
            this.mockJsS3InvokerProvider.Object,
            this.awsS3Config,
            null!));
    }

    // ─── Initial state ────────────────────────────────────────────────────────

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.True(sut.IsLoading);
        Assert.Empty(sut.Vaults);
        Assert.Empty(sut.VaultListings);
        Assert.False(sut.ShowPassphrasePanel);
        Assert.False(sut.ShowVaultDetailsPanel);
        Assert.False(sut.IsUnlocking);
        Assert.Null(sut.SelectedVault);
        Assert.Null(sut.SelectedVaultListing);
        Assert.Null(sut.ErrorMessage);
    }

    // ─── ShowPassphrasePanelFor / HidePassphrasePanel ─────────────────────────

    [Fact]
    public void GivenVaultMetadata_WhenShowPassphrasePanelFor_ThenPanelIsShownWithVault()
    {
        // Arrange
        var sut = CreateSut();
        var vault = new VaultMetadata { Id = "vault-1", Name = "Test Vault" };

        // Act
        sut.ShowPassphrasePanelForCommand.Execute(vault);

        // Assert
        Assert.True(sut.ShowPassphrasePanel);
        Assert.Equal(vault, sut.SelectedVault);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public void GivenPassphrasePanelShown_WhenHidePassphrasePanel_ThenPanelIsHidden()
    {
        // Arrange
        var sut = CreateSut();
        var vault = new VaultMetadata { Id = "vault-1", Name = "Test Vault" };
        sut.ShowPassphrasePanelForCommand.Execute(vault);

        // Act
        sut.HidePassphrasePanelCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowPassphrasePanel);
        Assert.Null(sut.SelectedVault);
        Assert.Null(sut.ErrorMessage);
    }

    // ─── ShowVaultDetailsPanelFor / HideVaultDetailsPanel ────────────────────

    [Fact]
    public void GivenVaultMetadata_WhenShowVaultDetailsPanelFor_ThenPanelIsShownWithVault()
    {
        // Arrange
        var sut = CreateSut();
        var vault = new VaultMetadata { Id = "vault-2", Name = "Details Vault" };

        // Act
        sut.ShowVaultDetailsPanelForCommand.Execute(vault);

        // Assert
        Assert.True(sut.ShowVaultDetailsPanel);
        Assert.Equal(vault, sut.SelectedVault);
    }

    [Fact]
    public void GivenDetailsPanelShown_WhenHideVaultDetailsPanel_ThenPanelIsHidden()
    {
        // Arrange
        var sut = CreateSut();
        var vault = new VaultMetadata { Id = "vault-2" };
        sut.ShowVaultDetailsPanelForCommand.Execute(vault);

        // Act
        sut.HideVaultDetailsPanelCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowVaultDetailsPanel);
        Assert.Null(sut.SelectedVault);
        Assert.Null(sut.SelectedVaultListing);
    }

    // ─── HandleUnlockVaultAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GivenNoSelectedVault_WhenHandleUnlockVault_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.HandleUnlockVaultCommand.ExecuteAsync("passphrase");

        // Assert
        Assert.Equal("Please enter a passphrase", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenSelectedVault_AndEmptyPassphrase_WhenHandleUnlockVault_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowPassphrasePanelForCommand.Execute(new VaultMetadata { Id = "vault-1" });

        // Act
        await sut.HandleUnlockVaultCommand.ExecuteAsync(string.Empty);

        // Assert
        Assert.Equal("Please enter a passphrase", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenSelectedVault_AndBootstrapperNotAvailable_WhenHandleUnlockVault_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowPassphrasePanelForCommand.Execute(new VaultMetadata { Id = "vault-1" });

        // bootstrapperService is null (no credentials in localStorage)
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync((string?)null);

        // Act
        await sut.HandleUnlockVaultCommand.ExecuteAsync("passphrase");

        // Assert
        Assert.Equal("Bootstrapper service not available", sut.ErrorMessage);
    }

    // ─── LoadVaultsAsync — no credentials path ────────────────────────────────

    [Fact]
    public async Task GivenNoCredentials_WhenLoadVaults_ThenVaultsAreEmptyAndLoadingIsFalse()
    {
        // Arrange
        var sut = CreateSut();
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync((string?)null);

        // Act
        await sut.LoadVaultsAsync();

        // Assert
        Assert.Empty(sut.Vaults);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenValidCredentials_WhenLoadVaults_ThenVaultsArePopulated()
    {
        // Arrange
        var sut = CreateSut();
        SetupValidCredentials();

        var mockBootstrapper = new Mock<IVaultManagerBootstrapperService>();
        mockBootstrapper
            .Setup(x => x.ListVaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new VaultListing { Id = "vault-1" },
                new VaultListing { Id = "vault-2" },
            ]);

        this.mockBootstrapperFactory
            .Setup(x => x.CreateForBlazor(
                It.IsAny<clypse.core.Cloud.Aws.S3.IJavaScriptS3Invoker>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mockBootstrapper.Object);

        this.mockVaultStorage
            .Setup(x => x.GetVaultsAsync())
            .ReturnsAsync([]);

        // Act
        await sut.LoadVaultsAsync();

        // Assert
        Assert.False(sut.IsLoading);
        // In DEBUG mode 20 dummy vaults are appended; check that the real 2 are present
        Assert.Contains(sut.Vaults, v => v.Id == "vault-1");
        Assert.Contains(sut.Vaults, v => v.Id == "vault-2");
    }

    [Fact]
    public async Task GivenStoredVaultNames_WhenLoadVaults_ThenNamesArePopulatedFromStorage()
    {
        // Arrange
        var sut = CreateSut();
        SetupValidCredentials();

        var mockBootstrapper = new Mock<IVaultManagerBootstrapperService>();
        mockBootstrapper
            .Setup(x => x.ListVaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new VaultListing { Id = "vault-1" }]);

        this.mockBootstrapperFactory
            .Setup(x => x.CreateForBlazor(
                It.IsAny<clypse.core.Cloud.Aws.S3.IJavaScriptS3Invoker>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(mockBootstrapper.Object);

        this.mockVaultStorage
            .Setup(x => x.GetVaultsAsync())
            .ReturnsAsync([new VaultMetadata { Id = "vault-1", Name = "My Vault", Description = "Test desc" }]);

        // Act
        await sut.LoadVaultsAsync();

        // Assert
        var vault = sut.Vaults.First(v => v.Id == "vault-1");
        Assert.Equal("My Vault", vault.Name);
        Assert.Equal("Test desc", vault.Description);
    }

    // ─── RefreshVaultsMessage ─────────────────────────────────────────────────

    [Fact]
    public async Task GivenRefreshVaultsMessage_WhenReceived_ThenLoadVaultsIsInvoked()
    {
        // Arrange
        var sut = CreateSut();
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync((string?)null);

        // Act — send message and give async void Receive() time to complete
        this.messenger.Send(new RefreshVaultsMessage());
        await Task.Delay(100);

        // Assert — LoadVaultsAsync was called, which set IsLoading=false on the no-credentials path
        Assert.False(sut.IsLoading);
    }

    // ─── ErrorMessage property ────────────────────────────────────────────────

    [Fact]
    public void GivenInstance_WhenSettingErrorMessage_ThenErrorMessageIsUpdated()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ErrorMessage = "Something went wrong";

        // Assert
        Assert.Equal("Something went wrong", sut.ErrorMessage);
    }

    // ─── Dispose / IDisposable ────────────────────────────────────────────────

    [Fact]
    public void GivenInstance_WhenDisposed_ThenNoExceptionIsThrown()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert (no exception)
        sut.Dispose();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void SetupValidCredentials()
    {
        var credentials = new StoredCredentials
        {
            AwsCredentials = new AwsCredentials
            {
                AccessKeyId = "access-key",
                SecretAccessKey = "secret-key",
                SessionToken = "session-token",
                IdentityId = "identity-id",
            },
        };
        var json = JsonSerializer.Serialize(credentials);
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync(json);

        this.mockJsS3InvokerProvider
            .Setup(x => x.GetInvoker())
            .Returns(new Mock<clypse.core.Cloud.Aws.S3.IJavaScriptS3Invoker>().Object);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.messenger.UnregisterAll(this);
    }
}
