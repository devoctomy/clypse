using System.Text;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Encryption.Exceptions;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.InMemory;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VaultSessionTests
{
    private readonly Mock<ISecretServiceFactory> mockFactory = new();
    private readonly Mock<IE2eProtectedProfileManager> mockManager = new();
    private readonly Mock<ISyncService> mockSync = new();
    private readonly Mock<ISyncUserContext> mockUser = new();
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly HashSet<E2eKeyScope> unlocked = [];
    private readonly Dictionary<string, InMemorySyncFileStore> stores = [];

    public VaultSessionTests()
    {
        mockUser.Setup(x => x.GetUserIdAsync()).ReturnsAsync("bob");
        mockManager.Setup(x => x.IsUnlocked(It.IsAny<E2eKeyScope>())).Returns((E2eKeyScope s) => unlocked.Contains(s));
        mockFactory.Setup(x => x.Create(It.IsAny<Vault>())).Returns((Vault v) => new SecretService(v, Store(v.Id), clock));
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("secretServiceFactory", Assert.Throws<ArgumentNullException>(() => new VaultSession(null!, mockManager.Object, mockSync.Object)).ParamName);
        Assert.Equal("manager", Assert.Throws<ArgumentNullException>(() => new VaultSession(mockFactory.Object, null!, mockSync.Object)).ParamName);
        Assert.Equal("syncService", Assert.Throws<ArgumentNullException>(() => new VaultSession(mockFactory.Object, mockManager.Object, null!)).ParamName);
    }

    [Fact]
    public async Task GivenAnUnlockedVault_WhenOpened_ThenEverySecretIsRead()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await AddSecretsAsync(vault, "One", "Two", "Three");
        using var sut = CreateSut();
        var changes = 0;
        sut.Changed += (_, _) => changes++;

        // Act
        await sut.OpenAsync(vault);

        // Assert
        Assert.Same(vault, sut.Vault);
        Assert.Equal(["One", "Three", "Two"], sut.Secrets.Select(s => s.Name).Order());
        Assert.Equal(3, sut.TotalCount);
        Assert.Equal(3, sut.LoadedCount);
        Assert.Equal(0, sut.UnreadableCount);
        Assert.False(sut.IsLoading);
        Assert.True(changes >= 2);
    }

    [Fact]
    public async Task GivenManySecrets_WhenOpened_ThenChangesAreBatched()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await AddSecretsAsync(vault, [.. Enumerable.Range(0, 60).Select(i => $"Secret {i}")]);
        using var sut = CreateSut();
        var changes = 0;
        sut.Changed += (_, _) => changes++;

        // Act
        await sut.OpenAsync(vault);

        // Assert
        Assert.Equal(60, sut.Secrets.Count);
        Assert.Equal(1 + 1 + (60 / VaultSession.NotifyEvery) + 1, changes);
    }

    [Fact]
    public async Task GivenAFileThatIsNotASecret_WhenOpened_ThenItIsCountedAsUnreadable()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await AddSecretsAsync(vault, "Good");
        await Store(vault.Id).WriteAsync($"{vault.Root}secrets/{Guid.NewGuid():D}.json", Encoding.UTF8.GetBytes("not json"));
        using var sut = CreateSut();

        // Act
        await sut.OpenAsync(vault);

        // Assert
        Assert.Equal("Good", Assert.Single(sut.Secrets).Name);
        Assert.Equal(2, sut.LoadedCount);
        Assert.Equal(1, sut.UnreadableCount);
    }

    [Fact]
    public async Task GivenALockedVault_WhenOpened_ThenNothingIsRead_UntilItIsUnlocked()
    {
        // Arrange
        var vault = NewVault(isUnlocked: false);
        await AddSecretsAsync(vault, "One", "Two");
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        Assert.Empty(sut.Secrets);

        // Act
        unlocked.Add(new E2eKeyScope(vault.Id));
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(vault.Id));
        await WaitForAsync(() => sut.Secrets.Count == 2);

        // Assert
        Assert.Equal(2, sut.Secrets.Count);
        Assert.True(vault.IsUnlocked);
    }

    [Fact]
    public async Task GivenALoadedVault_WhenLocked_ThenItsSecretsAreForgotten()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await AddSecretsAsync(vault, "One", "Two");
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        unlocked.Clear();
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(vault.Id));

        // Assert
        Assert.Empty(sut.Secrets);
        Assert.Equal(0, sut.TotalCount);
        Assert.Equal(0, sut.LoadedCount);
        Assert.False(vault.IsUnlocked);
        Assert.Same(vault, sut.Vault);
    }

    [Fact]
    public async Task GivenALoadedVault_WhenAnotherVaultIsLocked_ThenNothingChanges()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await AddSecretsAsync(vault, "One");
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(Guid.NewGuid().ToString("D")));

        // Assert
        Assert.Single(sut.Secrets);
    }

    [Fact]
    public async Task GivenAVaultBeingRead_WhenAnotherIsOpened_ThenNothingOfTheFirstIsKept()
    {
        // Arrange
        var first = NewVault(isUnlocked: true);
        var second = NewVault(isUnlocked: true);
        var firstId = Guid.NewGuid().ToString("D");
        var release = new TaskCompletionSource();
        var slow = new Mock<ISecretService>();
        slow.Setup(x => x.ListIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([firstId]);
        slow.Setup(x => x.GetAsync(firstId, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await release.Task;
                return new Secret { Id = firstId, Name = "From the first vault" };
            });
        mockFactory.Setup(x => x.Create(first)).Returns(slow.Object);
        await AddSecretsAsync(second, "From the second vault");
        using var sut = CreateSut();
        var opening = sut.OpenAsync(first);

        // Act
        await sut.OpenAsync(second);
        release.SetResult();
        await opening;

        // Assert
        Assert.Same(second, sut.Vault);
        Assert.Equal("From the second vault", Assert.Single(sut.Secrets).Name);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenAVaultLockedPartWayThroughReading_WhenOpened_ThenItStopsQuietly()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        var locked = new Mock<ISecretService>();
        locked.Setup(x => x.ListIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([Guid.NewGuid().ToString("D")]);
        locked.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new E2eLockedException());
        mockFactory.Setup(x => x.Create(vault)).Returns(locked.Object);
        using var sut = CreateSut();

        // Act
        await sut.OpenAsync(vault);

        // Assert
        Assert.Empty(sut.Secrets);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenSyncBringsAChangedSecret_ThenItIsReadAgain()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        var ids = await AddSecretsAsync(vault, "Old name");
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var other = new SecretService(vault, Store(vault.Id), clock);
        var changed = (await other.GetAsync(ids[0]))!;
        changed.Name = "New name";
        await other.SaveAsync(changed);
        var added = await other.SaveAsync(new Secret { Name = "Added" });

        // Act
        mockSync.Raise(x => x.FilesChanged += null, mockSync.Object, new SyncFilesChangedEventArgs(vault.Id, [$"{vault.Root}secrets/{ids[0]}.json", $"{vault.Root}secrets/{added.Id}.json", $"{vault.Root}.vault"]));
        await WaitForAsync(() => sut.Secrets.Count == 2);

        // Assert
        Assert.Equal(["Added", "New name"], sut.Secrets.Select(s => s.Name).Order());
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenSyncDeletesASecret_ThenItIsRemoved()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        var ids = await AddSecretsAsync(vault, "Keep", "Gone");
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        await Store(vault.Id).DeleteAsync($"{vault.Root}secrets/{ids[1]}.json");

        // Act
        mockSync.Raise(x => x.FilesChanged += null, mockSync.Object, new SyncFilesChangedEventArgs(vault.Id, [$"{vault.Root}secrets/{ids[1]}.json"]));
        await WaitForAsync(() => sut.Secrets.Count == 1);

        // Assert
        Assert.Equal("Keep", Assert.Single(sut.Secrets).Name);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenAnotherProfileChanges_ThenNothingIsRead()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await AddSecretsAsync(vault, "One");
        var service = new Mock<ISecretService>();
        service.Setup(x => x.ListIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        mockFactory.Setup(x => x.Create(vault)).Returns(service.Object);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        mockSync.Raise(x => x.FilesChanged += null, mockSync.Object, new SyncFilesChangedEventArgs(Guid.NewGuid().ToString("D"), ["anything"]));

        // Assert
        service.Verify(x => x.GetSecretId(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenSecretsAreSavedDeletedAndImported_ThenTheListFollows()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        var saved = await sut.SaveAsync(new WebSecret { Name = "Saved" });
        var gone = await sut.SaveAsync(new Secret { Name = "Gone" });
        await sut.DeleteAsync(gone.Id);
        var imported = await sut.ImportAsync([new Secret { Name = "Imported 1" }, new Secret { Name = "Imported 2" }]);

        // Assert
        Assert.Equal(2, imported);
        Assert.Equal(["Imported 1", "Imported 2", "Saved"], sut.Secrets.Select(s => s.Name).Order());
        Assert.Equal(3, (await new SecretService(vault, Store(vault.Id), clock).ListIdsAsync()).Count);
        Assert.Contains(sut.Secrets, s => s.Id == saved.Id);
    }

    [Fact]
    public async Task GivenNoOpenVault_WhenSaving_ThenThrowsNoVaultOpen()
    {
        // Arrange
        using var sut = CreateSut();

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => sut.SaveAsync(new Secret { Name = "One" }));

        // Assert
        Assert.Equal(ClypseErrorCodes.NoVaultOpen, ex.Code);
    }

    [Fact]
    public async Task GivenALockedVault_WhenSavingDeletingOrImporting_ThenThrowsNoVaultOpen()
    {
        // Arrange
        var vault = NewVault(isUnlocked: false);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act & Assert
        Assert.Equal(ClypseErrorCodes.NoVaultOpen, (await Assert.ThrowsAsync<ClypseException>(() => sut.SaveAsync(new Secret { Name = "One" }))).Code);
        Assert.Equal(ClypseErrorCodes.NoVaultOpen, (await Assert.ThrowsAsync<ClypseException>(() => sut.DeleteAsync(Guid.NewGuid().ToString("D")))).Code);
        Assert.Equal(ClypseErrorCodes.NoVaultOpen, (await Assert.ThrowsAsync<ClypseException>(() => sut.ImportAsync([new Secret { Name = "One" }]))).Code);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenClosed_ThenNothingIsHeld()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await AddSecretsAsync(vault, "One");
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var changed = false;
        sut.Changed += (_, _) => changed = true;

        // Act
        sut.Close();

        // Assert
        Assert.Null(sut.Vault);
        Assert.Empty(sut.Secrets);
        Assert.Equal(0, sut.TotalCount);
        Assert.True(changed);
        mockManager.Verify(x => x.Lock(It.IsAny<E2eKeyScope>()), Times.Never);
    }

    [Fact]
    public async Task GivenDisposed_WhenTheVaultIsLockedOrSynced_ThenNothingChanges()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await AddSecretsAsync(vault, "One");
        var sut = CreateSut();
        await sut.OpenAsync(vault);
        var changed = false;
        sut.Changed += (_, _) => changed = true;

        // Act
        sut.Dispose();
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(vault.Id));
        mockSync.Raise(x => x.FilesChanged += null, mockSync.Object, new SyncFilesChangedEventArgs(vault.Id, ["x"]));

        // Assert
        Assert.False(changed);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5);
        }
    }

    private VaultSession CreateSut() => new(mockFactory.Object, mockManager.Object, mockSync.Object);

    private Vault NewVault(bool isUnlocked)
    {
        var id = Guid.NewGuid().ToString("D");
        if (isUnlocked)
        {
            unlocked.Add(new E2eKeyScope(id));
        }

        return new Vault { Id = id, Root = $"profiles/{id}/", Name = id, IsUnlocked = isUnlocked };
    }

    private async Task<List<string>> AddSecretsAsync(Vault vault, params string[] names)
    {
        var service = new SecretService(vault, Store(vault.Id), clock);
        var ids = new List<string>();
        foreach (var name in names)
        {
            ids.Add((await service.SaveAsync(new Secret { Name = name })).Id);
        }

        return ids;
    }

    private InMemorySyncFileStore Store(string id)
    {
        if (!stores.TryGetValue(id, out var store))
        {
            store = new InMemorySyncFileStore(mockUser.Object, clock);
            stores[id] = store;
        }

        return store;
    }
}
