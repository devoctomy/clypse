using System.Text;
using System.Text.Json;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.Encryption.Interfaces;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VaultServiceTests
{
    private readonly Mock<ISyncProtectedProfileService> mockProfiles = new();
    private readonly Mock<IE2eProtectedProfileManager> mockManager = new();
    private readonly Mock<ISyncProfileRegistry> mockRegistry = new();
    private readonly Mock<IPasskeyKeysService> mockPasskeys = new();
    private readonly List<SyncProtectedProfile> profiles = [];
    private readonly HashSet<E2eKeyScope> unlocked = [];
    private readonly Dictionary<string, Dictionary<string, byte[]>> stored = [];

    public VaultServiceTests()
    {
        mockProfiles.SetupGet(x => x.Profiles).Returns(() => profiles);
        mockManager.Setup(x => x.IsUnlocked(It.IsAny<E2eKeyScope>())).Returns((E2eKeyScope s) => unlocked.Contains(s));
        mockRegistry.Setup(x => x.GetFileStore(It.IsAny<string>())).Returns((string name) => Store(name));
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("profiles", Assert.Throws<ArgumentNullException>(() => new VaultService(null!, mockManager.Object, mockRegistry.Object, mockPasskeys.Object)).ParamName);
        Assert.Equal("manager", Assert.Throws<ArgumentNullException>(() => new VaultService(mockProfiles.Object, null!, mockRegistry.Object, mockPasskeys.Object)).ParamName);
        Assert.Equal("registry", Assert.Throws<ArgumentNullException>(() => new VaultService(mockProfiles.Object, mockManager.Object, null!, mockPasskeys.Object)).ParamName);
        Assert.Equal("passkeys", Assert.Throws<ArgumentNullException>(() => new VaultService(mockProfiles.Object, mockManager.Object, mockRegistry.Object, null!)).ParamName);
    }

    [Fact]
    public async Task GivenNoProfiles_WhenGettingVaults_ThenNone()
    {
        // Act
        var vaults = await CreateSut().GetVaultsAsync();

        // Assert
        Assert.Empty(vaults);
    }

    [Fact]
    public async Task GivenProfiles_WhenGettingVaults_ThenOnlyThoseCreatedByTheUser_NamedWhenUnlocked()
    {
        // Arrange
        AddProfile("files", isCreatedByUser: false);
        var unlockedId = AddVault();
        var lockedId = AddVault();
        unlocked.Add(new E2eKeyScope(unlockedId));
        await WriteDetailsAsync(unlockedId, "Personal", "Everyday logins");

        // Act
        var vaults = await CreateSut().GetVaultsAsync();

        // Assert
        Assert.Collection(
            vaults,
            v =>
            {
                Assert.Equal(unlockedId, v.Id);
                Assert.Equal($"profiles/{unlockedId}/", v.Root);
                Assert.Equal("Personal", v.Name);
                Assert.Equal("Everyday logins", v.Description);
                Assert.True(v.IsUnlocked);
            },
            v =>
            {
                Assert.Equal(lockedId, v.Name);
                Assert.Null(v.Description);
                Assert.False(v.IsUnlocked);
            });
        mockProfiles.Verify(x => x.SetLabelAsync(new E2eKeyScope(unlockedId), "Personal"), Times.Once);
    }

    [Fact]
    public async Task GivenALockedVaultLabelledOnThisDevice_WhenFound_ThenItsLabelIsShown()
    {
        // Arrange
        var id = AddVault();
        profiles[^1].Label = "Work";

        // Act
        var vault = await CreateSut().FindAsync(id);

        // Assert
        Assert.Equal("Work", vault!.Name);
        mockProfiles.Verify(x => x.SetLabelAsync(It.IsAny<E2eKeyScope>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GivenAnUnlockedVaultAlreadyLabelled_WhenFound_ThenNotLabelledAgain()
    {
        // Arrange
        var id = AddVault();
        profiles[^1].Label = "Work";
        unlocked.Add(new E2eKeyScope(id));
        await WriteDetailsAsync(id, "Work", null);

        // Act
        var vault = await CreateSut().FindAsync(id);

        // Assert
        Assert.Equal("Work", vault!.Name);
        mockProfiles.Verify(x => x.SetLabelAsync(It.IsAny<E2eKeyScope>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GivenAnUnlockedVaultWithoutItsDetailsYet_WhenFound_ThenItsIdIsShown()
    {
        // Arrange
        var id = AddVault();
        unlocked.Add(new E2eKeyScope(id));

        // Act
        var vault = await CreateSut().FindAsync(id);

        // Assert
        Assert.Equal(id, vault!.Name);
    }

    [Fact]
    public async Task GivenUnreadableDetails_WhenFound_ThenItsIdIsShown()
    {
        // Arrange
        var id = AddVault();
        unlocked.Add(new E2eKeyScope(id));
        await Store(id).WriteAsync($"profiles/{id}/.vault", Encoding.UTF8.GetBytes("not json"));

        // Act
        var vault = await CreateSut().FindAsync(id);

        // Assert
        Assert.Equal(id, vault!.Name);
    }

    [Fact]
    public async Task GivenAnIdThatIsNotAVault_WhenFound_ThenNull()
    {
        // Arrange
        AddProfile("files", isCreatedByUser: false);

        // Act & Assert
        Assert.Null(await CreateSut().FindAsync("files"));
        Assert.Null(await CreateSut().FindAsync(Guid.NewGuid().ToString("D")));
    }

    [Fact]
    public async Task GivenANamePassphraseAndDescription_WhenCreated_ThenAProtectedProfileWithTheDetailsInside()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("D");
        var progress = new Progress<E2eProgress>();
        mockProfiles.Setup(x => x.CreateAsync("correct horse battery", progress, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                AddProfile(id, isCreatedByUser: true);
                unlocked.Add(new E2eKeyScope(id));
            })
            .ReturnsAsync(new E2eKeyScope(id));

        // Act
        var vault = await CreateSut().CreateAsync("  Personal  ", "  Everyday logins ", "correct horse battery", progress);

        // Assert
        Assert.Equal(id, vault.Id);
        Assert.Equal("Personal", vault.Name);
        Assert.Equal("Everyday logins", vault.Description);
        var details = ReadDetails(id);
        Assert.Equal("Personal", details.Name);
        Assert.Equal("Everyday logins", details.Description);
        mockProfiles.Verify(x => x.SetLabelAsync(new E2eKeyScope(id), "Personal"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GivenABlankDescription_WhenCreated_ThenNoDescriptionIsKept()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("D");
        mockProfiles.Setup(x => x.CreateAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                AddProfile(id, isCreatedByUser: true);
                unlocked.Add(new E2eKeyScope(id));
            })
            .ReturnsAsync(new E2eKeyScope(id));

        // Act
        var vault = await CreateSut().CreateAsync("Personal", "   ", "correct horse battery");

        // Assert
        Assert.Null(vault.Description);
        Assert.Null(ReadDetails(id).Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenNoName_WhenCreated_ThenNothingIsCreated(string name)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().CreateAsync(name, null, "correct horse battery"));

        // Assert
        Assert.Equal(ClypseErrorCodes.VaultNameRequired, ex.Code);
        mockProfiles.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<IProgress<E2eProgress>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenAVaultThatIsCreatedButNotListed_WhenCreated_ThenThrowsInvalidOperationException()
    {
        // Arrange
        mockProfiles.Setup(x => x.CreateAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new E2eKeyScope(Guid.NewGuid().ToString("D")));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().CreateAsync("Personal", null, "correct horse battery"));
    }

    [Fact]
    public async Task GivenAnUnlockedVault_WhenUpdated_ThenItsDetailsAndLabelChange()
    {
        // Arrange
        var id = AddVault();
        unlocked.Add(new E2eKeyScope(id));
        await WriteDetailsAsync(id, "Personal", null);

        // Act
        var vault = await CreateSut().UpdateAsync(id, " Home ", "Shared at home");

        // Assert
        Assert.Equal("Home", vault.Name);
        Assert.Equal("Shared at home", vault.Description);
        Assert.Equal("Home", ReadDetails(id).Name);
        mockProfiles.Verify(x => x.SetLabelAsync(new E2eKeyScope(id), "Home"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GivenNoVault_WhenUpdated_ThenThrowsVaultNotFound()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().UpdateAsync(Guid.NewGuid().ToString("D"), "Home", null));

        // Assert
        Assert.Equal(ClypseErrorCodes.VaultNotFound, ex.Code);
    }

    [Fact]
    public async Task GivenAVault_WhenUpdatedWithNoName_ThenThrowsVaultNameRequired()
    {
        // Arrange
        var id = AddVault();

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().UpdateAsync(id, " ", null));

        // Assert
        Assert.Equal(ClypseErrorCodes.VaultNameRequired, ex.Code);
    }

    [Fact]
    public async Task GivenAVault_WhenDeleted_ThenItsProfileIsDeleted()
    {
        // Arrange
        var id = AddVault();

        // Act
        await CreateSut().DeleteAsync(id);

        // Assert
        mockProfiles.Verify(x => x.DeleteAsync(new E2eKeyScope(id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenNoVault_WhenDeleted_ThenThrowsVaultNotFound()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().DeleteAsync(Guid.NewGuid().ToString("D")));

        // Assert
        Assert.Equal(ClypseErrorCodes.VaultNotFound, ex.Code);
    }

    [Fact]
    public void GivenAVault_WhenLocked_ThenItsScopeIsLocked()
    {
        // Arrange
        var id = AddVault();

        // Act
        CreateSut().Lock(id);

        // Assert
        mockManager.Verify(x => x.Lock(new E2eKeyScope(id)), Times.Once);
    }

    [Fact]
    public void GivenNoVault_WhenLocked_ThenThrowsVaultNotFound()
    {
        // Act
        var ex = Assert.Throws<ClypseException>(() => CreateSut().Lock(Guid.NewGuid().ToString("D")));

        // Assert
        Assert.Equal(ClypseErrorCodes.VaultNotFound, ex.Code);
    }

    [Fact]
    public async Task GivenALockedVaultWithBiometricsOnHere_AndAPasskey_WhenListed_ThenItCanBeUnlockedWithThem()
    {
        // Arrange
        var id = AddVault();
        var other = AddVault();
        mockProfiles.Setup(x => x.HasDeviceUnlockAsync(new E2eKeyScope(id))).ReturnsAsync(true);
        mockPasskeys.Setup(x => x.IsAvailableAsync()).ReturnsAsync(true);

        // Act
        var vaults = await CreateSut().GetVaultsAsync();

        // Assert
        Assert.True(vaults.Single(v => v.Id == id).CanUnlockWithBiometrics);
        Assert.False(vaults.Single(v => v.Id == other).CanUnlockWithBiometrics);
    }

    [Fact]
    public async Task GivenBiometricsOnHere_ButNoPasskey_WhenListed_ThenNotOffered()
    {
        // Arrange
        var id = AddVault();
        mockProfiles.Setup(x => x.HasDeviceUnlockAsync(new E2eKeyScope(id))).ReturnsAsync(true);
        mockPasskeys.Setup(x => x.IsAvailableAsync()).ReturnsAsync(false);

        // Act
        var vault = await CreateSut().FindAsync(id);

        // Assert
        Assert.False(vault!.CanUnlockWithBiometrics);
    }

    [Fact]
    public async Task GivenThePasskeysKey_WhenUnlockedWithBiometrics_ThenTheProfileServiceUnlocksIt()
    {
        // Arrange
        var id = AddVault();
        mockPasskeys.Setup(x => x.EnsureKeyAsync("e2e-kek")).ReturnsAsync(true);

        // Act
        var result = await CreateSut().UnlockWithBiometricsAsync(id);

        // Assert
        Assert.True(result);
        mockProfiles.Verify(x => x.UnlockWithDeviceAsync(new E2eKeyScope(id)), Times.Once);
    }

    [Fact]
    public async Task GivenThePasskeyCancelledOrUnableToUnlock_WhenUnlockedWithBiometrics_ThenFalse()
    {
        // Arrange
        var id = AddVault();
        var sut = CreateSut();

        // Act & Assert
        mockPasskeys.Setup(x => x.EnsureKeyAsync("e2e-kek")).ReturnsAsync(false);
        Assert.False(await sut.UnlockWithBiometricsAsync(id));
        mockProfiles.Verify(x => x.UnlockWithDeviceAsync(It.IsAny<E2eKeyScope>()), Times.Never);
        mockPasskeys.Setup(x => x.EnsureKeyAsync("e2e-kek")).ReturnsAsync(true);
        mockProfiles.Setup(x => x.UnlockWithDeviceAsync(It.IsAny<E2eKeyScope>())).ThrowsAsync(new InvalidOperationException("another passkey"));
        Assert.False(await sut.UnlockWithBiometricsAsync(id));
    }

    [Fact]
    public async Task GivenAVault_WhenBiometricsForgotten_ThenTheProfileServiceTurnsThemOff()
    {
        // Arrange
        var id = AddVault();

        // Act
        await CreateSut().ForgetBiometricsAsync(id);

        // Assert
        mockProfiles.Verify(x => x.DisableDeviceUnlockAsync(new E2eKeyScope(id)), Times.Once);
    }

    [Fact]
    public async Task GivenThisDevicesKeysCannotBeRead_WhenListed_ThenBiometricsAreNotOffered()
    {
        // Arrange
        var id = AddVault();
        mockProfiles.Setup(x => x.HasDeviceUnlockAsync(new E2eKeyScope(id))).ThrowsAsync(new InvalidOperationException("unreadable"));

        // Act
        var vault = await CreateSut().FindAsync(id);

        // Assert
        Assert.False(vault!.CanUnlockWithBiometrics);
    }

    [Fact]
    public async Task GivenAnUnlockedVault_WhenFoundAndCancelled_ThenTheCancellationIsThrown()
    {
        // Arrange
        var id = AddVault();
        unlocked.Add(new E2eKeyScope(id));
        var store = new Mock<ISyncFileStore>();
        store.Setup(x => x.ReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());
        mockRegistry.Setup(x => x.GetFileStore(id)).Returns(store.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateSut().FindAsync(id));
    }

    [Fact]
    public async Task GivenNoVault_WhenUnlockedWithBiometrics_ThenThrowsVaultNotFound()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().UnlockWithBiometricsAsync("missing"));
        Assert.Equal(ClypseErrorCodes.VaultNotFound, exception.Code);
    }

    [Fact]
    public async Task GivenNoVault_WhenBiometricsForgotten_ThenThrowsVaultNotFound()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().ForgetBiometricsAsync("missing"));
        Assert.Equal(ClypseErrorCodes.VaultNotFound, exception.Code);
    }

    private VaultService CreateSut() => new(mockProfiles.Object, mockManager.Object, mockRegistry.Object, mockPasskeys.Object);

    private string AddVault() => AddProfile(Guid.NewGuid().ToString("D"), isCreatedByUser: true);

    private string AddProfile(string name, bool isCreatedByUser)
    {
        var id = isCreatedByUser ? name : Guid.NewGuid().ToString("D");
        profiles.Add(new SyncProtectedProfile { Name = name, Scope = new E2eKeyScope(id), IsCreatedByUser = isCreatedByUser, IsSetUp = true });
        return id;
    }

    private Task WriteDetailsAsync(string id, string name, string? description)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(new VaultDetails { Name = name, Description = description });
        return Store(id).WriteAsync($"profiles/{id}/.vault", json);
    }

    private VaultDetails ReadDetails(string id)
    {
        return JsonSerializer.Deserialize<VaultDetails>(stored[id][$"profiles/{id}/.vault"])!;
    }

    private ISyncFileStore Store(string name)
    {
        if (!stored.TryGetValue(name, out var files))
        {
            files = [];
            stored[name] = files;
        }

        var store = new Mock<ISyncFileStore>();
        store.Setup(x => x.ReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) => files.TryGetValue(key, out var content) ? new SyncFile { Key = key, Content = content } : null);
        store.Setup(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DateTimeOffset?, CancellationToken>((key, content, _, _) => files[key] = content)
            .Returns(Task.CompletedTask);
        return store.Object;
    }
}
