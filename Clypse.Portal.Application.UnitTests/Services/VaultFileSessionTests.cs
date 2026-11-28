using System.Text;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Files;
using Clypse.Portal.Models.Settings;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Encryption.Exceptions;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.Encryption.Interfaces;
using Devoctomy.Blazwiz.Sync.InMemory;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public partial class VaultFileSessionTests
{
    private readonly Mock<IVaultFileServiceFactory> mockFactory = new();
    private readonly Mock<IE2eProtectedProfileManager> mockManager = new();
    private readonly Mock<ISyncService> mockSync = new();
    private readonly Mock<IE2eIdleLock> mockIdle = new();
    private readonly Mock<ISyncUserContext> mockUser = new();
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero));
    private readonly VaultFileTreeService tree = new();
    private readonly ClypseSettings settings = new();
    private readonly HashSet<E2eKeyScope> unlocked = [];
    private readonly Dictionary<string, InMemorySyncFileStore> stores = [];

    public VaultFileSessionTests()
    {
        mockUser.Setup(x => x.GetUserIdAsync()).ReturnsAsync("bob");
        mockManager.Setup(x => x.IsUnlocked(It.IsAny<E2eKeyScope>())).Returns((E2eKeyScope s) => unlocked.Contains(s));
        mockFactory.Setup(x => x.Create(It.IsAny<Vault>())).Returns((Vault v) => Service(v));
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("fileServiceFactory", Assert.Throws<ArgumentNullException>(() => new VaultFileSession(null!, tree, mockManager.Object, mockSync.Object, mockIdle.Object)).ParamName);
        Assert.Equal("tree", Assert.Throws<ArgumentNullException>(() => new VaultFileSession(mockFactory.Object, null!, mockManager.Object, mockSync.Object, mockIdle.Object)).ParamName);
        Assert.Equal("manager", Assert.Throws<ArgumentNullException>(() => new VaultFileSession(mockFactory.Object, tree, null!, mockSync.Object, mockIdle.Object)).ParamName);
        Assert.Equal("syncService", Assert.Throws<ArgumentNullException>(() => new VaultFileSession(mockFactory.Object, tree, mockManager.Object, null!, mockIdle.Object)).ParamName);
        Assert.Equal("idleLock", Assert.Throws<ArgumentNullException>(() => new VaultFileSession(mockFactory.Object, tree, mockManager.Object, mockSync.Object, null!)).ParamName);
    }

    [Fact]
    public async Task GivenATextFile_WhenItsContentIsSaved_ThenItIsStored_AndItsMetadataFollows()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var entry = await sut.AddFileAsync("Notes.md", "Work", "text/markdown", Encoding.UTF8.GetBytes("one"));
        clock.Advance(TimeSpan.FromMinutes(5));
        var changes = 0;
        sut.Changed += (_, _) => changes++;

        // Act
        var saved = await sut.SaveContentAsync(entry.Id, Encoding.UTF8.GetBytes("one two three"));

        // Assert
        Assert.Same(saved, Assert.Single(sut.Entries));
        Assert.Equal(13, saved.Size);
        Assert.Equal(entry.LastUpdatedAt.AddMinutes(5), saved.LastUpdatedAt);
        Assert.Equal("Work", saved.Folder);
        Assert.True(sut.HasContent(entry.Id));
        Assert.Equal(1, changes);
        Assert.Equal("one two three", Encoding.UTF8.GetString((await sut.ReadContentAsync(entry.Id))!));
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenKeptUnlocked_ThenItsIdleTimerIsRestarted_AndNothingHappensWithNoneOpen()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();

        // Act
        sut.KeepUnlocked();
        var withNoneOpen = mockIdle.Invocations.Count;
        await sut.OpenAsync(vault);
        sut.KeepUnlocked();

        // Assert
        Assert.Equal(0, withNoneOpen);
        mockIdle.Verify(x => x.Touch(new E2eKeyScope(vault.Id)), Times.Once);
    }

    [Fact]
    public async Task GivenAVault_WhenAskedWhetherItIsUnlocked_ThenItFollowsTheVaultsLock()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        var withNoneOpen = sut.IsUnlocked;
        await sut.OpenAsync(vault);
        var whileUnlocked = sut.IsUnlocked;

        // Act
        unlocked.Clear();

        // Assert
        Assert.False(withNoneOpen);
        Assert.True(whileUnlocked);
        Assert.False(sut.IsUnlocked);
    }

    [Fact]
    public async Task GivenAnUnlockedVault_WhenOpened_ThenItsFilesMetadataIsRead_AndNoContent()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        var reading = new Mock<IVaultFileService>();
        var service = Service(vault);
        var file = await service.AddFileAsync("Passport.jpg", "Travel", "image/jpeg", [1, 2, 3]);
        var record = await service.AddFolderAsync("Work");
        reading.Setup(x => x.ListAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken ct) => service.ListAsync(ct));
        reading.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string id, CancellationToken ct) => service.GetAsync(id, ct));
        mockFactory.Setup(x => x.Create(vault)).Returns(reading.Object);
        using var sut = CreateSut();
        var changes = 0;
        sut.Changed += (_, _) => changes++;

        // Act
        await sut.OpenAsync(vault);

        // Assert
        Assert.Same(vault, sut.Vault);
        Assert.Equal(new[] { file.Id, record.Id }.Order(), sut.Entries.Select(e => e.Id).Order());
        Assert.Equal(2, sut.TotalCount);
        Assert.Equal(2, sut.LoadedCount);
        Assert.Equal(0, sut.UnreadableCount);
        Assert.False(sut.IsLoading);
        Assert.True(sut.HasContent(file.Id));
        Assert.False(sut.HasContent(record.Id));
        Assert.True(changes >= 2);
        reading.Verify(x => x.ReadContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenMetadataThatCannotBeRead_WhenOpened_ThenItIsCountedAsUnreadable()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await Service(vault).AddFileAsync("Good.jpg", null, "image/jpeg", [1]);
        await Store(vault.Id).WriteAsync($"{vault.Root}files/meta/{Guid.NewGuid():D}.json", Encoding.UTF8.GetBytes("not json"));
        using var sut = CreateSut();

        // Act
        await sut.OpenAsync(vault);

        // Assert
        Assert.Equal("Good.jpg", Assert.Single(sut.Entries).Name);
        Assert.Equal(2, sut.LoadedCount);
        Assert.Equal(1, sut.UnreadableCount);
    }

    [Fact]
    public async Task GivenALockedVault_WhenOpened_ThenNothingIsRead_UntilItIsUnlocked()
    {
        // Arrange
        var vault = NewVault(isUnlocked: false);
        var service = Service(vault);
        await service.AddFileAsync("One.jpg", null, "image/jpeg", [1]);
        await service.AddFileAsync("Two.jpg", null, "image/jpeg", [1]);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        Assert.Empty(sut.Entries);

        // Act
        unlocked.Add(new E2eKeyScope(vault.Id));
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(vault.Id));
        await WaitForAsync(() => sut.Entries.Count == 2);

        // Assert
        Assert.Equal(2, sut.Entries.Count);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenItIsLocked_ThenItsMetadataIsForgotten()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        var file = await Service(vault).AddFileAsync("One.jpg", "Travel", "image/jpeg", [1]);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var changes = 0;
        sut.Changed += (_, _) => changes++;

        // Act
        unlocked.Clear();
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(vault.Id));

        // Assert
        Assert.Empty(sut.Entries);
        Assert.False(sut.HasContent(file.Id));
        Assert.Equal(0, sut.TotalCount);
        Assert.Equal(1, changes);
        await Assert.ThrowsAsync<ClypseException>(() => sut.AddFileAsync("Two.jpg", null, "image/jpeg", [1]));
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenAnotherVaultIsLocked_ThenNothingChanges()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await Service(vault).AddFileAsync("One.jpg", null, "image/jpeg", [1]);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(Guid.NewGuid().ToString("D")));

        // Assert
        Assert.Single(sut.Entries);
    }

    [Fact]
    public async Task GivenAVaultLockedPartWayThroughReading_WhenOpened_ThenItStopsQuietly()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        var locked = new Mock<IVaultFileService>();
        locked.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VaultFileListing { EntryIds = [Guid.NewGuid().ToString("D")] });
        locked.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new E2eLockedException());
        mockFactory.Setup(x => x.Create(vault)).Returns(locked.Object);
        using var sut = CreateSut();

        // Act
        await sut.OpenAsync(vault);

        // Assert
        Assert.Empty(sut.Entries);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenAVaultStillBeingRead_WhenClosed_ThenTheReadIsAbandonedQuietly()
    {
        // Arrange: the list of files is still being read, and is cancelled like any other read.
        var vault = NewVault(isUnlocked: true);
        var slow = new Mock<IVaultFileService>();
        slow.Setup(x => x.ListAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken ct) =>
        {
            var listing = new TaskCompletionSource<VaultFileListing>();
            ct.Register(() => listing.TrySetCanceled(ct));
            return listing.Task;
        });
        mockFactory.Setup(x => x.Create(vault)).Returns(slow.Object);
        using var sut = CreateSut();
        var opening = sut.OpenAsync(vault);
        var whileReading = sut.IsLoading;

        // Act
        sut.Close();
        await opening;

        // Assert
        Assert.True(whileReading);
        Assert.False(sut.IsLoading);
        Assert.Empty(sut.Entries);
        Assert.Null(sut.Vault);
    }

    [Fact]
    public async Task GivenALockedVault_WhenItIsUnlockedButItsFilesCannotBeListed_ThenNothingIsThrown()
    {
        // Arrange
        var vault = NewVault(isUnlocked: false);
        var broken = new Mock<IVaultFileService>();
        broken.Setup(x => x.ListAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        mockFactory.Setup(x => x.Create(vault)).Returns(broken.Object);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        unlocked.Add(new E2eKeyScope(vault.Id));
        var ex = Record.Exception(() => mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(vault.Id)));
        await WaitForAsync(() => broken.Invocations.Count > 0 && !sut.IsLoading);

        // Assert
        Assert.Null(ex);
        broken.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(sut.IsLoading);
        Assert.Empty(sut.Entries);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenSyncBringsMetadataThatCannotBeFetched_ThenNothingIsThrown_OrRaised()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        var id = Guid.NewGuid().ToString("D");
        var key = $"{vault.Root}files/meta/{id}.json";
        var broken = new Mock<IVaultFileService>();
        broken.Setup(x => x.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new VaultFileListing());
        broken.Setup(x => x.GetEntryId(key)).Returns(id);
        broken.Setup(x => x.GetAsync(id, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        mockFactory.Setup(x => x.Create(vault)).Returns(broken.Object);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var changes = 0;
        sut.Changed += (_, _) => changes++;

        // Act
        var ex = Record.Exception(() => mockSync.Raise(x => x.FilesChanged += null, mockSync.Object, new SyncFilesChangedEventArgs(vault.Id, [key])));
        await WaitForAsync(() => broken.Invocations.Any(i => i.Method.Name == nameof(IVaultFileService.GetAsync)));

        // Assert
        Assert.Null(ex);
        broken.Verify(x => x.GetAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(0, changes);
        Assert.Empty(sut.Entries);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenClosed_ThenItsMetadataIsForgotten_AndTheVaultIsNotLocked()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await Service(vault).AddFileAsync("One.jpg", null, "image/jpeg", [1]);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        sut.Close();

        // Assert
        Assert.Null(sut.Vault);
        Assert.Empty(sut.Entries);
        mockManager.Verify(x => x.Lock(It.IsAny<E2eKeyScope>()), Times.Never);
        var ex = await Assert.ThrowsAsync<ClypseException>(() => sut.CreateFolderAsync("Travel"));
        Assert.Equal(ClypseErrorCodes.NoVaultOpen, ex.Code);
    }

    [Fact]
    public async Task GivenNoVault_WhenOpened_ThenThrowsArgumentNullException()
    {
        // Arrange
        using var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.OpenAsync(null!));
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenAFileIsAdded_ThenItIsListed_WithItsContentHere()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        var entry = await sut.AddFileAsync("Passport.jpg", "Travel", "image/jpeg", [1, 2, 3]);

        // Assert
        Assert.Same(entry, Assert.Single(sut.Entries));
        Assert.True(sut.HasContent(entry.Id));
        Assert.Equal(new byte[] { 1, 2, 3 }, await Service(vault).ReadContentAsync(entry.Id));
    }

    [Fact]
    public async Task GivenAFile_WhenItsContentIsRead_ThenItIsDecryptedEachTime_AndTheSessionKeepsNoneOfIt()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var entry = await sut.AddFileAsync("Passport.jpg", null, "image/jpeg", [1, 2, 3]);

        // Act
        var first = await sut.ReadContentAsync(entry.Id);
        var second = await sut.ReadContentAsync(entry.Id);

        // Assert
        Assert.Equal(new byte[] { 1, 2, 3 }, first);
        Assert.Equal(new byte[] { 1, 2, 3 }, second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task GivenAFileWhoseContentIsNotHere_WhenItsContentIsRead_ThenNull()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var entry = await sut.AddFileAsync("Passport.jpg", null, "image/jpeg", [1, 2, 3]);
        await Store(vault.Id).DeleteAsync($"{vault.Root}files/blobs/{entry.Id}.bin");

        // Act
        var content = await sut.ReadContentAsync(entry.Id);

        // Assert
        Assert.Null(content);
    }

    [Fact]
    public async Task GivenALockedVault_WhenAFilesContentIsRead_ThenThrowsNoVaultOpen()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var entry = await sut.AddFileAsync("Passport.jpg", null, "image/jpeg", [1, 2, 3]);
        unlocked.Clear();

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => sut.ReadContentAsync(entry.Id));

        // Assert
        Assert.Equal(ClypseErrorCodes.NoVaultOpen, ex.Code);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenSyncBringsChangedMetadata_ThenItIsReadAgain()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        var other = Service(vault);
        var changed = await other.AddFileAsync("Old name.jpg", null, "image/jpeg", [1]);
        var deleted = await other.AddFileAsync("Deleted.jpg", null, "image/jpeg", [1]);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        changed.Name = "New name.jpg";
        await other.UpdateAsync(changed);
        await other.DeleteAsync(deleted.Id);
        var added = await other.AddFileAsync("Added.jpg", "Travel", "image/jpeg", [1]);
        var changes = 0;
        sut.Changed += (_, _) => changes++;

        // Act
        mockSync.Raise(
            x => x.FilesChanged += null,
            mockSync.Object,
            new SyncFilesChangedEventArgs(
                vault.Id,
                [
                    $"{vault.Root}files/meta/{changed.Id}.json",
                    $"{vault.Root}files/meta/{deleted.Id}.json",
                    $"{vault.Root}files/blobs/{deleted.Id}.bin",
                    $"{vault.Root}files/meta/{added.Id}.json",
                    $"{vault.Root}files/blobs/{added.Id}.bin",
                    $"{vault.Root}secrets/{Guid.NewGuid():D}.json",
                ]));
        await WaitForAsync(() => changes > 0);

        // Assert
        Assert.Equal(["Added.jpg", "New name.jpg"], sut.Entries.Select(e => e.Name).Order());
        Assert.True(sut.HasContent(added.Id));
        Assert.False(sut.HasContent(deleted.Id));
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task GivenMetadataWhoseContentIsNotHereYet_WhenSyncBringsTheContent_ThenItIsHere()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        var other = Service(vault);
        var entry = await other.AddFileAsync("Passport.jpg", null, "image/jpeg", [1, 2, 3]);
        var contentKey = $"{vault.Root}files/blobs/{entry.Id}.bin";
        var content = (await Store(vault.Id).ReadAsync(contentKey))!.Content;
        await Store(vault.Id).DeleteAsync(contentKey);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        Assert.False(sut.HasContent(entry.Id));
        var changes = 0;
        sut.Changed += (_, _) => changes++;

        // Act
        await Store(vault.Id).WriteAsync(contentKey, content);
        mockSync.Raise(x => x.FilesChanged += null, mockSync.Object, new SyncFilesChangedEventArgs(vault.Id, [contentKey]));
        await WaitForAsync(() => changes > 0);

        // Assert
        Assert.True(sut.HasContent(entry.Id));
        Assert.Single(sut.Entries);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenSyncChangesOnlyOtherFiles_ThenNothingIsRaised()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        await Service(vault).AddFileAsync("One.jpg", null, "image/jpeg", [1]);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var changes = 0;
        sut.Changed += (_, _) => changes++;

        // Act
        mockSync.Raise(x => x.FilesChanged += null, mockSync.Object, new SyncFilesChangedEventArgs(vault.Id, [$"{vault.Root}secrets/{Guid.NewGuid():D}.json"]));
        mockSync.Raise(x => x.FilesChanged += null, mockSync.Object, new SyncFilesChangedEventArgs(Guid.NewGuid().ToString("D"), [$"{vault.Root}files/meta/{Guid.NewGuid():D}.json"]));
        await Task.Delay(20);

        // Assert
        Assert.Equal(0, changes);
        Assert.Single(sut.Entries);
    }

    [Fact]
    public async Task GivenADisposedSession_WhenTheVaultIsUnlocked_ThenNothingIsRead()
    {
        // Arrange
        var vault = NewVault(isUnlocked: false);
        await Service(vault).AddFileAsync("One.jpg", null, "image/jpeg", [1]);
        var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        sut.Dispose();
        unlocked.Add(new E2eKeyScope(vault.Id));
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(vault.Id));
        await Task.Delay(20);

        // Assert
        Assert.Empty(sut.Entries);
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
    public async Task GivenAnOpenUnlockedVault_WhenAFilesContentKeyIsAsked_ThenItIsTheVaults_AndNamesTheFileAgain()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        var id = Guid.NewGuid().ToString("D");
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        var key = sut.GetContentKey(id);

        // Assert
        Assert.Equal($"{vault.Root}files/blobs/{id}.bin", key);
        Assert.Equal(id, sut.GetContentId(key!));
        Assert.Null(sut.GetContentId($"{vault.Root}files/meta/{id}.json"));
    }

    [Fact]
    public async Task GivenALockedVault_OrNone_WhenAFilesContentKeyIsAsked_ThenThereIsNone()
    {
        // Arrange
        var vault = NewVault(isUnlocked: false);
        var id = Guid.NewGuid().ToString("D");
        using var none = CreateSut();
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act & Assert
        Assert.Null(none.GetContentKey(id));
        Assert.Null(none.GetContentId($"{vault.Root}files/blobs/{id}.bin"));
        Assert.Null(sut.GetContentKey(id));
        Assert.Null(sut.GetContentId($"{vault.Root}files/blobs/{id}.bin"));
    }

    private VaultFileSession CreateSut() => new(mockFactory.Object, tree, mockManager.Object, mockSync.Object, mockIdle.Object);

    private VaultFileService Service(Vault vault) => new(vault, Store(vault.Id), tree, settings, clock);

    private Vault NewVault(bool isUnlocked)
    {
        var id = Guid.NewGuid().ToString("D");
        if (isUnlocked)
        {
            unlocked.Add(new E2eKeyScope(id));
        }

        return new Vault { Id = id, Root = $"profiles/{id}/", Name = id, IsUnlocked = isUnlocked };
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
