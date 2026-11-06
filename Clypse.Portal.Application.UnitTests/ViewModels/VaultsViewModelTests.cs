using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Devoctomy.Blazwiz.Encryption.Exceptions;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.Encryption.Interfaces;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class VaultsViewModelTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultService> mockVaults = new();
    private readonly Mock<ISyncProtectedProfileService> mockProfiles = new();
    private readonly Mock<IE2eProtectedProfileManager> mockManager = new();
    private readonly Mock<INavigationService> mockNavigation = new();
    private List<Vault> vaults = [];

    public VaultsViewModelTests()
    {
        mockVaults.Setup(x => x.GetVaultsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => vaults);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("vaultService", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(null!, mockProfiles.Object, mockManager.Object, mockNavigation.Object, text.Errors, t)).ParamName);
        Assert.Equal("profiles", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, null!, mockManager.Object, mockNavigation.Object, text.Errors, t)).ParamName);
        Assert.Equal("manager", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, mockProfiles.Object, null!, mockNavigation.Object, text.Errors, t)).ParamName);
        Assert.Equal("navigationService", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, mockProfiles.Object, mockManager.Object, null!, text.Errors, t)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, mockProfiles.Object, mockManager.Object, mockNavigation.Object, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, mockProfiles.Object, mockManager.Object, mockNavigation.Object, text.Errors, null!)).ParamName);
    }

    [Fact]
    public async Task GivenVaults_WhenInitialized_ThenNamedOnesComeFirstByName_ThenThoseShownById()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("D");
        vaults = [Vault("Work"), new Vault { Id = id, Name = id }, Vault("bank"), Vault("Personal")];
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.False(sut.IsLoading);
        Assert.Equal(["bank", "Personal", "Work", id], sut.Vaults.Select(v => v.Name));
        Assert.True(VaultsViewModel.IsShownById(sut.Vaults[^1]));
        Assert.False(VaultsViewModel.IsShownById(sut.Vaults[0]));
    }

    [Fact]
    public async Task GivenAVaultCreatedElsewhere_WhenProfilesChange_ThenItIsListed()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        vaults = [Vault("Work")];

        // Act
        mockProfiles.Raise(x => x.ProfilesChanged += null, EventArgs.Empty);
        await WaitForAsync(() => sut.Vaults.Count == 1);

        // Assert
        Assert.Single(sut.Vaults);
    }

    [Fact]
    public async Task GivenAVaultUnlocked_WhenLockStateChanges_ThenTheListIsRefreshed()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        vaults = [Vault("Work")];

        // Act
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(vaults[0].Id));
        await WaitForAsync(() => sut.Vaults.Count == 1);

        // Assert
        Assert.Single(sut.Vaults);
    }

    [Fact]
    public async Task GivenRefreshingFails_WhenProfilesChange_ThenNothingIsThrown()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        mockVaults.Setup(x => x.GetVaultsAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var ex = Record.Exception(() => mockProfiles.Raise(x => x.ProfilesChanged += null, EventArgs.Empty));

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public void GivenAVault_WhenOpenedOrLocked_ThenItsPageIsShownOrItIsLocked()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.OpenCommand.Execute("abc");
        sut.LockCommand.Execute("abc");

        // Assert
        mockNavigation.Verify(x => x.NavigateTo("vaults/abc"), Times.Once);
        mockVaults.Verify(x => x.Lock("abc"), Times.Once);
    }

    [Fact]
    public void GivenTheDialogWasUsed_WhenShownAgain_ThenItsFieldsAreEmpty()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        Fill(sut);

        // Act
        sut.CloseCreateDialogCommand.Execute(null);
        sut.ShowCreateDialogCommand.Execute(null);

        // Assert
        Assert.True(sut.IsCreateDialogOpen);
        Assert.Equal(string.Empty, sut.Name);
        Assert.Equal(string.Empty, sut.Description);
        Assert.Equal(string.Empty, sut.Passphrase);
        Assert.Equal(string.Empty, sut.ConfirmPassphrase);
        Assert.False(sut.AcknowledgedNoRecovery);
        Assert.Null(sut.DialogError);
    }

    [Theory]
    [InlineData(" ", "correct horse", "correct horse", true, "Give the vault a name.")]
    [InlineData("Personal", "", "", true, "Enter a passphrase.")]
    [InlineData("Personal", "correct horse", "correct house", true, "The passphrases do not match.")]
    [InlineData("Personal", "correct horse", "correct horse", false, "Confirm you understand there is no passphrase recovery.")]
    public async Task GivenAnIncompleteDialog_WhenCreating_ThenItSaysWhatIsWrongAndCreatesNothing(
        string name, string passphrase, string confirm, bool acknowledged, string expected)
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        sut.Name = name;
        sut.Passphrase = passphrase;
        sut.ConfirmPassphrase = confirm;
        sut.AcknowledgedNoRecovery = acknowledged;

        // Act
        await sut.CreateCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(expected, sut.DialogError);
        Assert.True(sut.IsCreateDialogOpen);
        mockVaults.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<IProgress<E2eProgress>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenACompleteDialog_WhenCreating_ThenTheVaultIsCreatedAndOpened_AndThePassphraseCleared()
    {
        // Arrange
        mockVaults.Setup(x => x.CreateAsync("Personal", "Logins", "correct horse", null, It.IsAny<CancellationToken>())).ReturnsAsync(Vault("Personal", "v1"));
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        Fill(sut);

        // Act
        await sut.CreateCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.IsCreateDialogOpen);
        Assert.False(sut.IsWorking);
        Assert.Equal(string.Empty, sut.Passphrase);
        Assert.Equal(string.Empty, sut.ConfirmPassphrase);
        mockNavigation.Verify(x => x.NavigateTo("vaults/v1"), Times.Once);
    }

    [Fact]
    public async Task GivenCreatingFails_WhenCreating_ThenTheErrorIsDescribedAndTheDialogStaysOpen()
    {
        // Arrange
        mockVaults.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClypseException(ClypseErrorCodes.VaultNameRequired));
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        Fill(sut);

        // Act
        await sut.CreateCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Give the vault a name.", sut.DialogError);
        Assert.True(sut.IsCreateDialogOpen);
        Assert.False(sut.IsWorking);
    }

    [Fact]
    public async Task GivenAFrameworkError_WhenCreating_ThenItIsDescribed()
    {
        // Arrange
        mockVaults.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new E2eLockedException());
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        Fill(sut);

        // Act
        await sut.CreateCommand.ExecuteAsync(null);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(sut.DialogError));
    }

    [Fact]
    public async Task GivenCreating_WhenTheDialogIsClosedOrCreateRunsAgain_ThenNeitherHappens()
    {
        // Arrange
        var release = new TaskCompletionSource<Vault>();
        mockVaults.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>())).Returns(release.Task);
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        Fill(sut);
        var creating = sut.CreateAsync();

        // Act
        sut.CloseCreateDialogCommand.Execute(null);
        await sut.CreateAsync();

        // Assert
        Assert.True(sut.IsWorking);
        Assert.True(sut.IsCreateDialogOpen);
        release.SetResult(Vault("Personal", "v1"));
        await creating;
        mockVaults.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenDisposed_WhenProfilesChange_ThenNothingIsRefreshed()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        mockVaults.Invocations.Clear();

        // Act
        sut.Dispose();
        mockProfiles.Raise(x => x.ProfilesChanged += null, EventArgs.Empty);
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope("x"));

        // Assert
        mockVaults.VerifyNoOtherCalls();
    }

    private static Vault Vault(string name, string? id = null) => new() { Id = id ?? Guid.NewGuid().ToString("D"), Name = name };

    private static void Fill(VaultsViewModel sut)
    {
        sut.Name = "Personal";
        sut.Description = "Logins";
        sut.Passphrase = "correct horse";
        sut.ConfirmPassphrase = "correct horse";
        sut.AcknowledgedNoRecovery = true;
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5);
        }
    }

    private VaultsViewModel CreateSut() => new(
        mockVaults.Object,
        mockProfiles.Object,
        mockManager.Object,
        mockNavigation.Object,
        text.Errors,
        text.For<ClypseText>());
}
