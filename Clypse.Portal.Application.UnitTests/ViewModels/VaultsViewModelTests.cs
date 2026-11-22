using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Devoctomy.Blazwiz.Cryptography.Configuration;
using Devoctomy.Blazwiz.Cryptography.Interfaces;
using Devoctomy.Blazwiz.Cryptography.Models;
using Devoctomy.Blazwiz.Encryption.Exceptions;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.Encryption.Interfaces;
using Devoctomy.Blazwiz.Testing;
using Devoctomy.Blazwiz.UI.Sync.Models;
using Devoctomy.Blazwiz.UI.Sync.Text;
using Devoctomy.Blazwiz.UI.Sync.ViewModels;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class VaultsViewModelTests
{
    private readonly FrameworkText text = new(typeof(ClypseText), typeof(SyncUiText));
    private readonly Mock<IArgon2idKeyDerivationService> mockArgon2 = new();
    private readonly E2ePassphraseFormViewModel form;
    private readonly Mock<IVaultService> mockVaults = new();
    private readonly Mock<ISyncProtectedProfileService> mockProfiles = new();
    private readonly Mock<IE2eProtectedProfileManager> mockManager = new();
    private readonly Mock<INavigationService> mockNavigation = new();
    private readonly List<string> calls = [];
    private List<Vault> vaults = [];

    public VaultsViewModelTests()
    {
        mockVaults.Setup(x => x.GetVaultsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => vaults);
        mockProfiles.Setup(x => x.GetArgon2ParametersAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(() => AppCosts());
        mockArgon2.Setup(x => x.DeriveKeyAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<Argon2idParameters>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => new byte[32]);
        var syncText = text.For<SyncUiText>();
        var benchmark = new Argon2idBenchmarkViewModel(mockArgon2.Object, new Argon2idOptions { Parameters = AppCosts() }, TimeProvider.System, syncText, text.Errors);
        form = new E2ePassphraseFormViewModel(mockProfiles.Object, benchmark, syncText);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("vaultService", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(null!, mockProfiles.Object, mockManager.Object, mockNavigation.Object, text.Errors, t, form)).ParamName);
        Assert.Equal("profiles", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, null!, mockManager.Object, mockNavigation.Object, text.Errors, t, form)).ParamName);
        Assert.Equal("manager", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, mockProfiles.Object, null!, mockNavigation.Object, text.Errors, t, form)).ParamName);
        Assert.Equal("navigationService", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, mockProfiles.Object, mockManager.Object, null!, text.Errors, t, form)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, mockProfiles.Object, mockManager.Object, mockNavigation.Object, null!, t, form)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, mockProfiles.Object, mockManager.Object, mockNavigation.Object, text.Errors, null!, form)).ParamName);
        Assert.Equal("passphraseForm", Assert.Throws<ArgumentNullException>(() => new VaultsViewModel(mockVaults.Object, mockProfiles.Object, mockManager.Object, mockNavigation.Object, text.Errors, t, null!)).ParamName);
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
    public async Task GivenAVaultBeingDeleted_WhenOpened_ThenNothingHappens()
    {
        // Arrange
        vaults = [new Vault { Id = "abc", Name = "Personal", IsDeleting = true }];
        var sut = CreateSut();
        await sut.OnInitializedAsync();

        // Act
        await sut.OpenCommand.ExecuteAsync("abc");

        // Assert
        mockNavigation.Verify(x => x.NavigateTo(It.IsAny<string>()), Times.Never);
        mockVaults.Verify(x => x.UnlockWithBiometricsAsync(It.IsAny<string>()), Times.Never);
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
        Assert.Equal(string.Empty, sut.PassphraseForm.Passphrase);
        Assert.Equal(string.Empty, sut.PassphraseForm.ConfirmPassphrase);
        Assert.False(sut.PassphraseForm.AcknowledgedNoRecovery);
        Assert.Null(sut.DialogError);
    }

    [Theory]
    [InlineData(" ", "correct horse", "correct horse", true, "Give the vault a name.")]
    [InlineData("Personal", "", "", true, "Enter your passphrase.")]
    [InlineData("Personal", "correct horse", "correct house", true, "The passphrases do not match.")]
    [InlineData("Personal", "correct horse", "correct horse", false, "Confirm you understand there is no passphrase recovery.")]
    public async Task GivenAnIncompleteDialog_WhenCreating_ThenItSaysWhatIsWrongAndCreatesNothing(
        string name, string passphrase, string confirm, bool acknowledged, string expected)
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        sut.Name = name;
        sut.PassphraseForm.Passphrase = passphrase;
        sut.PassphraseForm.ConfirmPassphrase = confirm;
        sut.PassphraseForm.AcknowledgedNoRecovery = acknowledged;

        // Act
        await sut.CreateCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(expected, sut.DialogError);
        Assert.True(sut.IsCreateDialogOpen);
        mockVaults.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<Argon2idParameters?>(), It.IsAny<IProgress<E2eProgress>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenACompleteDialog_WhenCreating_ThenTheVaultIsCreatedAndOpened_AndThePassphraseCleared()
    {
        // Arrange
        mockVaults.Setup(x => x.CreateAsync("Personal", "Logins", "correct horse", It.IsAny<Argon2idParameters?>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(Vault("Personal", "v1"));
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        Fill(sut);

        // Act
        await sut.CreateCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.IsCreateDialogOpen);
        Assert.False(sut.IsWorking);
        Assert.Equal(string.Empty, sut.PassphraseForm.Passphrase);
        Assert.Equal(string.Empty, sut.PassphraseForm.ConfirmPassphrase);
        mockNavigation.Verify(x => x.NavigateTo("vaults/v1"), Times.Once);
    }

    [Fact]
    public async Task GivenArgon2SettingsChanged_WhenCreating_ThenRefusedUntilTheyAreBenchmarked_ThenCreatedWithThem()
    {
        // Arrange
        Argon2idParameters? used = null;
        mockVaults.Setup(x => x.CreateAsync("Personal", "Logins", "correct horse", It.IsAny<Argon2idParameters?>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string?, string, Argon2idParameters?, IProgress<E2eProgress>?, CancellationToken>((_, _, _, argon2, _, _) => used = argon2)
            .ReturnsAsync(Vault("Personal", "v1"));
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        Fill(sut);
        var startedAt = sut.PassphraseForm.Argon2.MemoryMib;
        sut.PassphraseForm.Argon2.MemoryMib = 4096;
        sut.PassphraseForm.Argon2.Iterations = 250;

        // Act
        var canCreateBefore = sut.CanCreate;
        await sut.CreateCommand.ExecuteAsync(null);
        var refused = sut.DialogError;
        var tab = sut.PassphraseForm.ActiveTab;
        var createdBeforeBenchmark = used;
        await sut.PassphraseForm.Argon2.BenchmarkCommand.ExecuteAsync(null);
        var canCreateAfter = sut.CanCreate;
        await sut.CreateCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(19, startedAt);
        Assert.False(canCreateBefore);
        Assert.Equal("Benchmark these settings on this device before they can be used.", refused);
        Assert.Equal(E2ePassphraseFormTab.Advanced, tab);
        Assert.Null(createdBeforeBenchmark);
        Assert.True(canCreateAfter);
        Assert.NotNull(used);
        Assert.Equal(4096 * 1024, used.MemoryKib);
        Assert.Equal(250, used.Iterations);
        Assert.Equal(32, used.KeyLength);
        mockNavigation.Verify(x => x.NavigateTo("vaults/v1"), Times.Once);
    }

    [Fact]
    public void GivenTheCreateDialog_WhenThePassphraseFormChanges_ThenThePageIsToldItsFormChanged()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        // Act
        sut.PassphraseForm.ShowTabCommand.Execute(E2ePassphraseFormTab.Advanced);
        var afterTab = changed.Count(p => p == nameof(VaultsViewModel.PassphraseForm));
        sut.PassphraseForm.Argon2.MemoryMib = 4096;

        // Assert
        Assert.True(afterTab > 0);
        Assert.True(changed.Count(p => p == nameof(VaultsViewModel.PassphraseForm)) > afterTab);
        Assert.Contains(nameof(VaultsViewModel.CanCreate), changed);
    }

    [Fact]
    public async Task GivenNoNameAndTheAdvancedTabShown_WhenCreating_ThenTheFirstTabIsShownWithTheError()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowCreateDialogCommand.Execute(null);
        sut.PassphraseForm.ShowTabCommand.Execute(E2ePassphraseFormTab.Advanced);

        // Act
        await sut.CreateCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Give the vault a name.", sut.DialogError);
        Assert.Equal(E2ePassphraseFormTab.Passphrase, sut.PassphraseForm.ActiveTab);
    }

    [Fact]
    public async Task GivenCreatingFails_WhenCreating_ThenTheErrorIsDescribedAndTheDialogStaysOpen()
    {
        // Arrange
        mockVaults.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<Argon2idParameters?>(), null, It.IsAny<CancellationToken>()))
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
        mockVaults.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<Argon2idParameters?>(), null, It.IsAny<CancellationToken>()))
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
        mockVaults.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<Argon2idParameters?>(), null, It.IsAny<CancellationToken>())).Returns(release.Task);
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
        mockVaults.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<Argon2idParameters?>(), null, It.IsAny<CancellationToken>()), Times.Once);
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

    private static Argon2idParameters AppCosts() => new() { MemoryKib = 19 * 1024, Iterations = 2, Parallelism = 1, KeyLength = 32 };

    private static Vault Vault(string name, string? id = null) => new() { Id = id ?? Guid.NewGuid().ToString("D"), Name = name };

    private static void Fill(VaultsViewModel sut)
    {
        sut.Name = "Personal";
        sut.Description = "Logins";
        sut.PassphraseForm.Passphrase = "correct horse";
        sut.PassphraseForm.ConfirmPassphrase = "correct horse";
        sut.PassphraseForm.AcknowledgedNoRecovery = true;
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5);
        }
    }

    [Fact]
    public async Task GivenTheVaultsLoaded_WhenThePageOpens_ThenTheListIsReadAgain()
    {
        // Arrange: e.g. one created or deleted on another device since sign in.
        mockProfiles.SetupGet(x => x.IsLoaded).Returns(true);
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        mockProfiles.Verify(x => x.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenTheVaultsStillBeingRead_WhenThePageOpens_ThenTheyAreNotReadAgain()
    {
        // Arrange
        mockProfiles.SetupGet(x => x.IsLoaded).Returns(false);
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        mockProfiles.Verify(x => x.LoadAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenTheListCannotBeRead_WhenThePageOpens_ThenTheVaultsKnownStay()
    {
        // Arrange
        mockProfiles.SetupGet(x => x.IsLoaded).Returns(true);
        mockProfiles.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("offline"));
        vaults.Add(new Vault { Id = "v1", Name = "Personal" });
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.Equal(["Personal"], sut.Vaults.Select(v => v.Name));
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenALockedVaultWithBiometrics_WhenOpened_ThenItIsUnlockedWithThem_ThenItsPageOpens()
    {
        // Arrange
        vaults = [new Vault { Id = "v1", Name = "Personal", CanUnlockWithBiometrics = true }];
        mockVaults.Setup(x => x.UnlockWithBiometricsAsync("v1")).Callback(() => calls.Add("unlock")).ReturnsAsync(true);
        mockNavigation.Setup(x => x.NavigateTo(It.IsAny<string>())).Callback<string>(url => calls.Add("open " + url));
        var sut = CreateSut();
        await sut.OnInitializedAsync();

        // Act
        await sut.OpenCommand.ExecuteAsync("v1");

        // Assert
        Assert.Equal(["unlock", "open vaults/v1"], calls);
    }

    [Fact]
    public async Task GivenBiometricsCancelled_WhenOpened_ThenItsPageStillOpens_ForThePassphrase()
    {
        // Arrange
        vaults = [new Vault { Id = "v1", Name = "Personal", CanUnlockWithBiometrics = true }];
        mockVaults.Setup(x => x.UnlockWithBiometricsAsync("v1")).ReturnsAsync(false);
        var sut = CreateSut();
        await sut.OnInitializedAsync();

        // Act
        await sut.OpenCommand.ExecuteAsync("v1");

        // Assert
        mockNavigation.Verify(x => x.NavigateTo("vaults/v1"), Times.Once);
    }

    [Fact]
    public async Task GivenAVaultWithoutBiometrics_WhenOpened_ThenItsPageOpens_WithoutUnlocking()
    {
        // Arrange
        vaults = [new Vault { Id = "v1", Name = "Personal" }, new Vault { Id = "v2", Name = "Work", IsUnlocked = true, CanUnlockWithBiometrics = true }];
        var sut = CreateSut();
        await sut.OnInitializedAsync();

        // Act
        await sut.OpenCommand.ExecuteAsync("v1");
        await sut.OpenCommand.ExecuteAsync("v2");

        // Assert
        mockVaults.Verify(x => x.UnlockWithBiometricsAsync(It.IsAny<string>()), Times.Never);
        mockNavigation.Verify(x => x.NavigateTo("vaults/v1"), Times.Once);
        mockNavigation.Verify(x => x.NavigateTo("vaults/v2"), Times.Once);
    }

    [Fact]
    public async Task GivenAVaultWithBiometrics_WhenTheyAreForgotten_ThenTheServiceForgetsThem_AndTheListIsRefreshed()
    {
        // Arrange
        vaults = [new Vault { Id = "v1", Name = "Personal", CanUnlockWithBiometrics = true }];
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        mockVaults.Setup(x => x.ForgetBiometricsAsync("v1")).Callback(() => vaults = [new Vault { Id = "v1", Name = "Personal" }]).Returns(Task.CompletedTask);

        // Act
        await sut.ForgetBiometricsCommand.ExecuteAsync("v1");

        // Assert
        mockVaults.Verify(x => x.ForgetBiometricsAsync("v1"), Times.Once);
        Assert.False(sut.Vaults.Single().CanUnlockWithBiometrics);
    }

    private VaultsViewModel CreateSut() => new(
        mockVaults.Object,
        mockProfiles.Object,
        mockManager.Object,
        mockNavigation.Object,
        text.Errors,
        text.For<ClypseText>(),
        form);

    [Fact]
    public void GivenTheDialog_WhenItsFieldsAreSetToNull_ThenTheyAreEmpty()
    {
        // Arrange
        var sut = CreateSut();
        Fill(sut);

        // Act
        sut.Name = null!;
        sut.Description = null!;
        sut.PassphraseForm.Passphrase = null!;
        sut.PassphraseForm.ConfirmPassphrase = null!;

        // Assert
        Assert.Empty(sut.Name);
        Assert.Empty(sut.Description);
        Assert.Empty(sut.PassphraseForm.Passphrase);
        Assert.Empty(sut.PassphraseForm.ConfirmPassphrase);
    }
}
