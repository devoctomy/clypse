using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.Encryption.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;
using Devoctomy.Blazwiz.Testing;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class VaultSettingsViewModelTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultService> mockVaults = new();
    private readonly Mock<ISyncProtectedProfileService> mockProfiles = new();
    private readonly Mock<INavigationService> mockNavigation = new();
    private readonly Mock<IToastService> mockToasts = new();
    private readonly Vault vault = new() { Id = Guid.NewGuid().ToString("D"), Name = "Personal", Description = "Logins", IsUnlocked = true };
    private TimeSpan? idleTimeout = TimeSpan.FromMinutes(5);

    public VaultSettingsViewModelTests()
    {
        mockProfiles.SetupGet(x => x.Profiles).Returns(() =>
        [
            new SyncProtectedProfile
            {
                Name = vault.Id,
                Scope = new E2eKeyScope(vault.Id),
                IsCreatedByUser = true,
                Settings = new E2eProfileSettings { IdleTimeout = idleTimeout },
            },
        ]);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("vaultService", Assert.Throws<ArgumentNullException>(() => new VaultSettingsViewModel(null!, mockProfiles.Object, mockNavigation.Object, mockToasts.Object, text.Errors, text.Plurals, t)).ParamName);
        Assert.Equal("profiles", Assert.Throws<ArgumentNullException>(() => new VaultSettingsViewModel(mockVaults.Object, null!, mockNavigation.Object, mockToasts.Object, text.Errors, text.Plurals, t)).ParamName);
        Assert.Equal("navigationService", Assert.Throws<ArgumentNullException>(() => new VaultSettingsViewModel(mockVaults.Object, mockProfiles.Object, null!, mockToasts.Object, text.Errors, text.Plurals, t)).ParamName);
        Assert.Equal("toasts", Assert.Throws<ArgumentNullException>(() => new VaultSettingsViewModel(mockVaults.Object, mockProfiles.Object, mockNavigation.Object, null!, text.Errors, text.Plurals, t)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new VaultSettingsViewModel(mockVaults.Object, mockProfiles.Object, mockNavigation.Object, mockToasts.Object, null!, text.Plurals, t)).ParamName);
        Assert.Equal("plurals", Assert.Throws<ArgumentNullException>(() => new VaultSettingsViewModel(mockVaults.Object, mockProfiles.Object, mockNavigation.Object, mockToasts.Object, text.Errors, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new VaultSettingsViewModel(mockVaults.Object, mockProfiles.Object, mockNavigation.Object, mockToasts.Object, text.Errors, text.Plurals, null!)).ParamName);
    }

    [Fact]
    public void GivenAVault_WhenLoaded_ThenItsNameDescriptionAndAutoLockAreShown()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Load(vault);

        // Assert
        Assert.Equal("Personal", sut.Name);
        Assert.Equal("Logins", sut.Description);
        Assert.Equal(5, sut.AutoLock);
        Assert.Equal(VaultSettingsViewModel.AutoLockMinutes, sut.AutoLockOptions);
        Assert.Equal("5 minutes", sut.GetAutoLockText(5));
        Assert.Equal("1 minute", sut.GetAutoLockText(1));
        Assert.Equal("Never", sut.GetAutoLockText(null));
    }

    [Fact]
    public void GivenAVaultThatNeverLocks_WhenLoaded_ThenNeverIsChosen()
    {
        // Arrange
        idleTimeout = null;
        var sut = CreateSut();

        // Act
        sut.Load(vault);

        // Assert
        Assert.Null(sut.AutoLock);
    }

    [Fact]
    public void GivenAVaultWithAnAutoLockNotOffered_WhenLoaded_ThenItIsOfferedInOrder()
    {
        // Arrange
        idleTimeout = TimeSpan.FromMinutes(10);
        var sut = CreateSut();

        // Act
        sut.Load(vault);

        // Assert
        Assert.Equal(10, sut.AutoLock);
        Assert.Equal([1, 5, 10, 15, 30, 60, null], sut.AutoLockOptions);
    }

    [Fact]
    public async Task GivenANewNameAndAutoLock_WhenSaved_ThenBothAreKept_TheVaultUpdated_AndTheToastShown()
    {
        // Arrange
        mockVaults.Setup(x => x.UpdateAsync(vault.Id, "Home", "Shared", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Vault { Id = vault.Id, Name = "Home", Description = "Shared" });
        var sut = CreateSut();
        sut.Load(vault);
        sut.Name = "Home";
        sut.Description = "Shared";
        sut.AutoLock = 15;

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.True(saved);
        Assert.Equal("Home", vault.Name);
        Assert.Equal("Shared", vault.Description);
        mockProfiles.Verify(x => x.SetIdleTimeoutAsync(new E2eKeyScope(vault.Id), TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()), Times.Once);
        mockToasts.Verify(x => x.Show("Vault settings saved.", ToastTone.Success, null), Times.Once);
    }

    [Fact]
    public async Task GivenNothingChanged_WhenSaved_ThenNothingIsWritten()
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(vault);

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.True(saved);
        mockVaults.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        mockProfiles.Verify(x => x.SetIdleTimeoutAsync(It.IsAny<E2eKeyScope>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNeverChosen_WhenSaved_ThenTheVaultNeverLocksForBeingIdle()
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(vault);
        sut.AutoLock = null;

        // Act
        await sut.SaveAsync();

        // Assert
        mockProfiles.Verify(x => x.SetIdleTimeoutAsync(new E2eKeyScope(vault.Id), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenSavingFails_WhenSaved_ThenTheErrorIsShown()
    {
        // Arrange
        mockVaults.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClypseException(ClypseErrorCodes.VaultNameRequired));
        var sut = CreateSut();
        sut.Load(vault);
        sut.Name = " ";

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.False(saved);
        Assert.Equal("Give the vault a name.", sut.Error);
        Assert.False(sut.IsWorking);
    }

    [Fact]
    public async Task GivenNoVault_WhenSavedOrDeleted_ThenNothingHappens()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        Assert.False(await sut.SaveAsync());
        Assert.False(await sut.DeleteAsync());
        Assert.Throws<ArgumentNullException>(() => sut.Load(null!));
    }

    [Theory]
    [InlineData("personal")]
    [InlineData("Persona")]
    [InlineData("")]
    public async Task GivenTheNameNotTypedExactly_WhenDeleted_ThenItSaysSoAndNothingIsDeleted(string typed)
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(vault);
        sut.DeleteConfirmation = typed;

        // Act
        var deleted = await sut.DeleteAsync();

        // Assert
        Assert.False(deleted);
        Assert.Equal("Type the vault's name exactly to delete it.", sut.Error);
        mockVaults.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenTheNameTyped_WhenDeleted_ThenTheVaultIsDeleted_AndTheVaultsAreShown()
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(vault);
        sut.DeleteConfirmation = " Personal ";

        // Act
        var deleted = await sut.DeleteAsync();

        // Assert
        Assert.True(deleted);
        mockVaults.Verify(x => x.DeleteAsync(vault.Id, It.IsAny<CancellationToken>()), Times.Once);
        mockToasts.Verify(x => x.Show("Personal deleted.", ToastTone.Success, null), Times.Once);
        mockNavigation.Verify(x => x.NavigateTo("vaults"), Times.Once);
    }

    [Fact]
    public async Task GivenDeletingFails_WhenDeleted_ThenTheErrorIsShownAndTheVaultStays()
    {
        // Arrange
        mockVaults.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new ClypseException(ClypseErrorCodes.VaultNotFound));
        mockVaults.Setup(x => x.FindAsync(vault.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vault);
        var sut = CreateSut();
        sut.Load(vault);
        sut.DeleteConfirmation = "Personal";

        // Act
        var deleted = await sut.DeleteAsync();

        // Assert
        Assert.False(deleted);
        Assert.Equal("That vault no longer exists.", sut.Error);
        mockNavigation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GivenDeletingFailsAfterTheVaultIsGone_WhenDeleted_ThenTheVaultsAreShown_AndItSaysWhatFailed()
    {
        // Arrange: its keys were deleted, but not all of its files.
        mockVaults.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new ClypseException(ClypseErrorCodes.VaultNotFound));
        mockVaults.Setup(x => x.FindAsync(vault.Id, It.IsAny<CancellationToken>())).ReturnsAsync((Vault?)null);
        var sut = CreateSut();
        sut.Load(vault);
        sut.DeleteConfirmation = "Personal";

        // Act
        var deleted = await sut.DeleteAsync();

        // Assert
        Assert.True(deleted);
        Assert.Null(sut.Error);
        mockToasts.Verify(x => x.Show("Personal deleted, but some of its files could not be removed: That vault no longer exists.", ToastTone.Warning, null), Times.Once);
        mockNavigation.Verify(x => x.NavigateTo("vaults"), Times.Once);
    }

    [Fact]
    public void GivenTheFields_WhenSetToNull_ThenTheyAreEmpty()
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(vault);

        // Act
        sut.Name = null!;
        sut.Description = null!;
        sut.DeleteConfirmation = null!;

        // Assert
        Assert.Empty(sut.Name);
        Assert.Empty(sut.Description);
        Assert.Empty(sut.DeleteConfirmation);
    }

    [Fact]
    public void GivenAVaultWithNoDescriptionOrProfileHere_WhenLoaded_ThenTheDescriptionIsEmptyAndItNeverLocks()
    {
        // Arrange
        var other = new Vault { Id = Guid.NewGuid().ToString("D"), Name = "Elsewhere" };
        var sut = CreateSut();

        // Act
        sut.Load(other);

        // Assert
        Assert.Empty(sut.Description);
        Assert.Null(sut.AutoLock);
    }

    [Fact]
    public async Task GivenTheDescriptionCleared_WhenSaved_ThenTheVaultHasNoDescription()
    {
        // Arrange
        mockVaults.Setup(x => x.UpdateAsync(vault.Id, "Personal", "  ", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Vault { Id = vault.Id, Name = "Personal" });
        var sut = CreateSut();
        sut.Load(vault);
        sut.Description = "  ";

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.True(saved);
        Assert.Null(vault.Description);
        mockVaults.Verify(x => x.UpdateAsync(vault.Id, "Personal", "  ", It.IsAny<CancellationToken>()), Times.Once);
    }

    private VaultSettingsViewModel CreateSut() => new(
        mockVaults.Object,
        mockProfiles.Object,
        mockNavigation.Object,
        mockToasts.Object,
        text.Errors,
        text.Plurals,
        text.For<ClypseText>());
}
