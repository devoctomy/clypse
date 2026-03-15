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

    // --- Constructor ---

    [Fact]
    public void GivenValidParameters_WhenConstructing_ThenCreatesInstance()
    {
        var sut = CreateSut();
        Assert.NotNull(sut);
    }

    [Fact]
    public void GivenNullVaultStorage_WhenConstructing_ThenThrowsArgumentNullException()
    {
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
    public void GivenNullAwsS3Config_WhenConstructing_ThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(
            this.mockVaultStorage.Object,
            this.mockBootstrapperFactory.Object,
            this.mockLocalStorage.Object,
            this.mockVaultStateService.Object,
            this.mockJsS3InvokerProvider.Object,
            null!,
            this.messenger));
    }

    [Fact]
    public void GivenNullMessenger_WhenConstructing_ThenThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(
            this.mockVaultStorage.Object,
            this.mockBootstrapperFactory.Object,
            this.mockLocalStorage.Object,
            this.mockVaultStateService.Object,
            this.mockJsS3InvokerProvider.Object,
            this.awsS3Config,
            null!));
    }

    // --- Initial state ---

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        var sut = CreateSut();

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

    // --- OnInitializedAsync ---

    [Fact]
    public async Task GivenNoCredentials_WhenOnInitializedAsync_ThenLoadVaultsIsInvoked()
    {
        // Arrange
        var sut = CreateSut();
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync((string?)null);

        // Act
        await sut.OnInitializedAsync();

        // Assert - LoadVaultsAsync ran: IsLoading=false, Vaults empty
        Assert.False(sut.IsLoading);
        Assert.Empty(sut.Vaults);
    }

    // --- OnAfterRenderAsync ---

    [Fact]
    public async Task GivenNoCredentials_WhenOnAfterRenderAsync_WithFirstRender_ThenLoadVaultsIsInvoked()
    {
        // Arrange
        var sut = CreateSut();
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync((string?)null);

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);

        // Assert - LoadVaultsAsync ran: IsLoading=false
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenNoCredentials_WhenOnAfterRenderAsync_NotFirstRender_ThenLoadVaultsIsNotInvoked()
    {
        // Arrange
        var sut = CreateSut();

        // Act - firstRender=false so LoadVaultsAsync is NOT called
        await sut.OnAfterRenderAsync(firstRender: false);

        // Assert - IsLoading stays at its initial true value
        Assert.True(sut.IsLoading);
        this.mockLocalStorage.Verify(x => x.GetItemAsync(It.IsAny<string>()), Times.Never);
    }

    // --- ShowPassphrasePanelFor / HidePassphrasePanel ---

    [Fact]
    public void GivenVaultMetadata_WhenShowPassphrasePanelFor_ThenPanelIsShownWithVault()
    {
        var sut = CreateSut();
        var vault = new VaultMetadata { Id = "vault-1", Name = "Test Vault" };

        sut.ShowPassphrasePanelForCommand.Execute(vault);

        Assert.True(sut.ShowPassphrasePanel);
        Assert.Equal(vault, sut.SelectedVault);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public void GivenPassphrasePanelShown_WhenHidePassphrasePanel_ThenPanelIsHidden()
    {
        var sut = CreateSut();
        sut.ShowPassphrasePanelForCommand.Execute(new VaultMetadata { Id = "vault-1" });

        sut.HidePassphrasePanelCommand.Execute(null);

        Assert.False(sut.ShowPassphrasePanel);
        Assert.Null(sut.SelectedVault);
        Assert.Null(sut.ErrorMessage);
    }

    // --- ShowVaultDetailsPanelFor / HideVaultDetailsPanel ---

    [Fact]
    public void GivenVaultMetadata_WhenShowVaultDetailsPanelFor_ThenPanelIsShownWithVault()
    {
        var sut = CreateSut();
        var vault = new VaultMetadata { Id = "vault-2", Name = "Details Vault" };

        sut.ShowVaultDetailsPanelForCommand.Execute(vault);

        Assert.True(sut.ShowVaultDetailsPanel);
        Assert.Equal(vault, sut.SelectedVault);
    }

    [Fact]
    public void GivenDetailsPanelShown_WhenHideVaultDetailsPanel_ThenPanelIsHidden()
    {
        var sut = CreateSut();
        sut.ShowVaultDetailsPanelForCommand.Execute(new VaultMetadata { Id = "vault-2" });

        sut.HideVaultDetailsPanelCommand.Execute(null);

        Assert.False(sut.ShowVaultDetailsPanel);
        Assert.Null(sut.SelectedVault);
        Assert.Null(sut.SelectedVaultListing);
    }

    [Fact]
    public async Task GivenMatchingVaultListing_WhenShowVaultDetailsPanelFor_ThenSelectedVaultListingIsSet()
    {
        // Arrange - load vaults first so the VaultListings backing field gets populated
        var sut = CreateSut();
        SetupValidCredentials();
        var mockBootstrapper = CreateMockBootstrapper(
            new List<VaultListing> { new VaultListing { Id = "vault-1", Manifest = new VaultManifest() } });
        SetupBootstrapperFactory(mockBootstrapper);
        this.mockVaultStorage.Setup(x => x.GetVaultsAsync()).ReturnsAsync(new List<VaultMetadata>());
        await sut.LoadVaultsAsync();

        var vault = new VaultMetadata { Id = "vault-1" };

        // Act
        sut.ShowVaultDetailsPanelForCommand.Execute(vault);

        // Assert - SelectedVaultListing was found from the loaded listings
        Assert.NotNull(sut.SelectedVaultListing);
        Assert.Equal("vault-1", sut.SelectedVaultListing.Id);
    }

    // --- LoadVaultsAsync ---

    [Fact]
    public async Task GivenNoCredentials_WhenLoadVaults_ThenVaultsAreEmptyAndLoadingIsFalse()
    {
        var sut = CreateSut();
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync((string?)null);

        await sut.LoadVaultsAsync();

        Assert.Empty(sut.Vaults);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenValidCredentials_WhenLoadVaults_ThenVaultsArePopulated()
    {
        var sut = CreateSut();
        SetupValidCredentials();
        var mockBootstrapper = CreateMockBootstrapper(
            new List<VaultListing>
            {
                new VaultListing { Id = "vault-1" },
                new VaultListing { Id = "vault-2" },
            });
        SetupBootstrapperFactory(mockBootstrapper);
        this.mockVaultStorage.Setup(x => x.GetVaultsAsync()).ReturnsAsync(new List<VaultMetadata>());

        await sut.LoadVaultsAsync();

        Assert.False(sut.IsLoading);
        Assert.Contains(sut.Vaults, v => v.Id == "vault-1");
        Assert.Contains(sut.Vaults, v => v.Id == "vault-2");
    }

    [Fact]
    public async Task GivenStoredVaultNames_WhenLoadVaults_ThenNamesArePopulatedFromStorage()
    {
        var sut = CreateSut();
        SetupValidCredentials();
        var mockBootstrapper = CreateMockBootstrapper(
            new List<VaultListing> { new VaultListing { Id = "vault-1" } });
        SetupBootstrapperFactory(mockBootstrapper);
        this.mockVaultStorage
            .Setup(x => x.GetVaultsAsync())
            .ReturnsAsync(new List<VaultMetadata>
            {
                new VaultMetadata { Id = "vault-1", Name = "My Vault", Description = "Test desc" },
            });

        await sut.LoadVaultsAsync();

        var vault = sut.Vaults.First(v => v.Id == "vault-1");
        Assert.Equal("My Vault", vault.Name);
        Assert.Equal("Test desc", vault.Description);
    }

    [Fact]
    public async Task GivenListingWithNullId_WhenLoadVaults_ThenVaultIdFallsBackToEmpty()
    {
        // Arrange - exercises the `listing.Id ?? string.Empty` null-coalescing branch
        var sut = CreateSut();
        SetupValidCredentials();
        var mockBootstrapper = CreateMockBootstrapper(
            new List<VaultListing> { new VaultListing { Id = null } });
        SetupBootstrapperFactory(mockBootstrapper);
        this.mockVaultStorage.Setup(x => x.GetVaultsAsync()).ReturnsAsync(new List<VaultMetadata>());

        await sut.LoadVaultsAsync();

        Assert.Contains(sut.Vaults, v => v.Id == string.Empty);
    }

    [Fact]
    public async Task GivenBootstrapperThrowsDuringListing_WhenLoadVaults_ThenVaultsAreEmptyAndLoadingIsFalse()
    {
        // Arrange - ListVaultsAsync throws, exercising the catch block in LoadVaultsAsync
        var sut = CreateSut();
        SetupValidCredentials();
        var mockBootstrapper = new Mock<IVaultManagerBootstrapperService>();
        mockBootstrapper
            .Setup(x => x.ListVaultsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("S3 unavailable"));
        SetupBootstrapperFactory(mockBootstrapper);

        await sut.LoadVaultsAsync();

        Assert.Empty(sut.Vaults);
        Assert.False(sut.IsLoading);
    }

    // --- CreateBootstrapperServiceAsync private paths ---

    [Fact]
    public async Task GivenCredentialsWithNullAwsCredentials_WhenLoadVaults_ThenBootstrapperIsNotCreated()
    {
        // Arrange - exercises the `credentials?.AwsCredentials == null` branch
        var sut = CreateSut();
        var credentials = new StoredCredentials { AwsCredentials = null };
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync(JsonSerializer.Serialize(credentials));

        await sut.LoadVaultsAsync();

        Assert.Empty(sut.Vaults);
        Assert.False(sut.IsLoading);
        this.mockBootstrapperFactory.Verify(
            x => x.CreateForBlazor(
                It.IsAny<clypse.core.Cloud.Aws.S3.IJavaScriptS3Invoker>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }


    [Fact]
    public async Task GivenJsonDeserializesToNull_WhenLoadVaults_ThenBootstrapperIsNotCreated()
    {
        // Arrange - exercises the `credentials == null` sub-branch of `credentials?.AwsCredentials == null`
        // When the stored JSON is the literal "null", Deserialize returns null
        var sut = CreateSut();
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync("null");

        await sut.LoadVaultsAsync();

        Assert.Empty(sut.Vaults);
        Assert.False(sut.IsLoading);
        this.mockBootstrapperFactory.Verify(
            x => x.CreateForBlazor(
                It.IsAny<clypse.core.Cloud.Aws.S3.IJavaScriptS3Invoker>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenCredentialsWithEmptyIdentityId_WhenLoadVaults_ThenBootstrapperIsNotCreated()
    {
        // Arrange - exercises the `string.IsNullOrEmpty(credentials.AwsCredentials.IdentityId)` branch
        var sut = CreateSut();
        var credentials = new StoredCredentials
        {
            AwsCredentials = new AwsCredentials
            {
                AccessKeyId = "key",
                SecretAccessKey = "secret",
                SessionToken = "token",
                IdentityId = string.Empty,
            },
        };
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync(JsonSerializer.Serialize(credentials));

        await sut.LoadVaultsAsync();

        Assert.Empty(sut.Vaults);
        Assert.False(sut.IsLoading);
        this.mockBootstrapperFactory.Verify(
            x => x.CreateForBlazor(
                It.IsAny<clypse.core.Cloud.Aws.S3.IJavaScriptS3Invoker>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenGetItemAsyncThrows_WhenLoadVaults_ThenBootstrapperIsNotCreated()
    {
        // Arrange - exercises the catch block in CreateBootstrapperServiceAsync
        var sut = CreateSut();
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ThrowsAsync(new Exception("storage error"));

        await sut.LoadVaultsAsync();

        Assert.Empty(sut.Vaults);
        Assert.False(sut.IsLoading);
    }

    // --- HandleUnlockVaultAsync ---

    [Fact]
    public async Task GivenNoSelectedVault_WhenHandleUnlockVault_ThenErrorMessageIsSet()
    {
        var sut = CreateSut();

        await sut.HandleUnlockVaultCommand.ExecuteAsync("passphrase");

        Assert.Equal("Please enter a passphrase", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenSelectedVault_AndEmptyPassphrase_WhenHandleUnlockVault_ThenErrorMessageIsSet()
    {
        var sut = CreateSut();
        sut.ShowPassphrasePanelForCommand.Execute(new VaultMetadata { Id = "vault-1" });

        await sut.HandleUnlockVaultCommand.ExecuteAsync(string.Empty);

        Assert.Equal("Please enter a passphrase", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenSelectedVault_AndBootstrapperNotAvailable_WhenHandleUnlockVault_ThenErrorIsSet()
    {
        // Arrange - bootstrapperService stays null (no credentials loaded)
        var sut = CreateSut();
        sut.ShowPassphrasePanelForCommand.Execute(new VaultMetadata { Id = "vault-1" });
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync((string?)null);

        await sut.HandleUnlockVaultCommand.ExecuteAsync("passphrase");

        Assert.Equal("Bootstrapper service not available", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenBootstrapperReturnsNullManager_WhenHandleUnlockVault_ThenErrorIsSet()
    {
        // Arrange - initialise the bootstrapper via LoadVaultsAsync
        var sut = CreateSut();
        SetupValidCredentials();
        var mockBootstrapper = CreateMockBootstrapper(
            new List<VaultListing> { new VaultListing { Id = "vault-1" } });
        mockBootstrapper
            .Setup(x => x.CreateVaultManagerForVaultAsync("vault-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IVaultManager?)null);
        SetupBootstrapperFactory(mockBootstrapper);
        this.mockVaultStorage.Setup(x => x.GetVaultsAsync()).ReturnsAsync(new List<VaultMetadata>());
        await sut.LoadVaultsAsync();

        sut.ShowPassphrasePanelForCommand.Execute(new VaultMetadata { Id = "vault-1" });

        await sut.HandleUnlockVaultCommand.ExecuteAsync("passphrase");

        Assert.Equal("Failed to create vault-specific manager", sut.ErrorMessage);
        Assert.False(sut.IsUnlocking);
    }

    [Fact]
    public async Task GivenFullyConfiguredBootstrapper_WhenHandleUnlockVault_ThenVaultIsUnlockedAndStateIsSet()
    {
        // Arrange
        var sut = CreateSut();
        SetupValidCredentials();

        var vaultInfo = new VaultInfo("Unlocked Vault", "Desc");
        var vaultIndex = new VaultIndex
        {
            Entries = new List<VaultIndexEntry>
            {
                new VaultIndexEntry("e1", "Secret1", null, null),
            },
        };
        var concreteVault = new clypse.core.Vault.Vault(new VaultManifest(), vaultInfo, vaultIndex);

        var mockManager = new Mock<IVaultManager>();
        mockManager
            .Setup(x => x.DeriveKeyFromPassphraseAsync("vault-1", "mypassphrase"))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 });
        mockManager
            .Setup(x => x.LoadAsync("vault-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(concreteVault);

        var mockBootstrapper = CreateMockBootstrapper(
            new List<VaultListing> { new VaultListing { Id = "vault-1" } });
        mockBootstrapper
            .Setup(x => x.CreateVaultManagerForVaultAsync("vault-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockManager.Object);
        SetupBootstrapperFactory(mockBootstrapper);
        this.mockVaultStorage.Setup(x => x.GetVaultsAsync()).ReturnsAsync(new List<VaultMetadata>());
        this.mockVaultStorage
            .Setup(x => x.UpdateVaultAsync(It.IsAny<VaultMetadata>()))
            .Returns(Task.CompletedTask);

        await sut.LoadVaultsAsync();
        sut.ShowPassphrasePanelForCommand.Execute(new VaultMetadata { Id = "vault-1" });

        // Act
        await sut.HandleUnlockVaultCommand.ExecuteAsync("mypassphrase");

        // Assert
        Assert.False(sut.IsUnlocking);
        Assert.False(sut.ShowPassphrasePanel);
        this.mockVaultStateService.Verify(
            x => x.SetVaultState(
                It.Is<VaultMetadata>(v => v.Id == "vault-1"),
                It.IsAny<string>(),
                concreteVault,
                mockManager.Object),
            Times.Once);
    }

    [Fact]
    public async Task GivenManagerThrowsDuringUnlock_WhenHandleUnlockVault_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = CreateSut();
        SetupValidCredentials();

        var mockManager = new Mock<IVaultManager>();
        mockManager
            .Setup(x => x.DeriveKeyFromPassphraseAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("wrong passphrase"));

        var mockBootstrapper = CreateMockBootstrapper(
            new List<VaultListing> { new VaultListing { Id = "vault-1" } });
        mockBootstrapper
            .Setup(x => x.CreateVaultManagerForVaultAsync("vault-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockManager.Object);
        SetupBootstrapperFactory(mockBootstrapper);
        this.mockVaultStorage.Setup(x => x.GetVaultsAsync()).ReturnsAsync(new List<VaultMetadata>());

        await sut.LoadVaultsAsync();
        sut.ShowPassphrasePanelForCommand.Execute(new VaultMetadata { Id = "vault-1" });

        // Act
        await sut.HandleUnlockVaultCommand.ExecuteAsync("badpassphrase");

        // Assert
        Assert.NotNull(sut.ErrorMessage);
        Assert.Contains("Failed to unlock vault:", sut.ErrorMessage);
        Assert.False(sut.IsUnlocking);
    }

    // --- RefreshVaultsMessage ---

    [Fact]
    public async Task GivenRefreshVaultsMessage_WhenReceived_ThenLoadVaultsIsInvoked()
    {
        var sut = CreateSut();
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync((string?)null);

        this.messenger.Send(new RefreshVaultsMessage());
        await Task.Delay(100);

        Assert.False(sut.IsLoading);
    }

    // --- ErrorMessage property ---

    [Fact]
    public void GivenInstance_WhenSettingErrorMessage_ThenErrorMessageIsUpdated()
    {
        var sut = CreateSut();

        sut.ErrorMessage = "Something went wrong";

        Assert.Equal("Something went wrong", sut.ErrorMessage);
    }

    // --- Dispose ---

    [Fact]
    public void GivenInstance_WhenDisposed_ThenNoExceptionIsThrown()
    {
        var sut = CreateSut();
        sut.Dispose();
    }

    // --- Helpers ---

    private static Mock<IVaultManagerBootstrapperService> CreateMockBootstrapper(List<VaultListing> listings)
    {
        var mock = new Mock<IVaultManagerBootstrapperService>();
        mock.Setup(x => x.ListVaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(listings);
        return mock;
    }

    private void SetupBootstrapperFactory(Mock<IVaultManagerBootstrapperService> mockBootstrapper)
    {
        this.mockBootstrapperFactory
            .Setup(x => x.CreateForBlazor(
                It.IsAny<clypse.core.Cloud.Aws.S3.IJavaScriptS3Invoker>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(mockBootstrapper.Object);
    }

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
        this.mockLocalStorage
            .Setup(x => x.GetItemAsync("clypse_credentials"))
            .ReturnsAsync(JsonSerializer.Serialize(credentials));
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
