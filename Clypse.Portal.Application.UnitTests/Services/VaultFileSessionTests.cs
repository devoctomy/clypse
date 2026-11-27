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
using Devoctomy.Blazwiz.Sync.InMemory;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VaultFileSessionTests
{
    private readonly Mock<IVaultFileServiceFactory> mockFactory = new();
    private readonly Mock<IE2eProtectedProfileManager> mockManager = new();
    private readonly Mock<ISyncService> mockSync = new();
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
        Assert.Equal("fileServiceFactory", Assert.Throws<ArgumentNullException>(() => new VaultFileSession(null!, tree, mockManager.Object, mockSync.Object)).ParamName);
        Assert.Equal("tree", Assert.Throws<ArgumentNullException>(() => new VaultFileSession(mockFactory.Object, null!, mockManager.Object, mockSync.Object)).ParamName);
        Assert.Equal("manager", Assert.Throws<ArgumentNullException>(() => new VaultFileSession(mockFactory.Object, tree, null!, mockSync.Object)).ParamName);
        Assert.Equal("syncService", Assert.Throws<ArgumentNullException>(() => new VaultFileSession(mockFactory.Object, tree, mockManager.Object, null!)).ParamName);
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
    public async Task GivenAnEmptyFolder_WhenAFileGoesIntoIt_ThenItsRecordIsRemoved()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        await sut.CreateFolderAsync("Travel");
        Assert.Equal(VaultFileKind.Folder, Assert.Single(sut.Entries).Kind);

        // Act
        var entry = await sut.AddFileAsync("Passport.jpg", "Travel", "image/jpeg", [1]);

        // Assert
        Assert.Same(entry, Assert.Single(sut.Entries));
        Assert.Equal([entry.Id], (await Service(vault).ListAsync()).EntryIds);
        Assert.True(tree.Exists(sut.Entries, "Travel"));
    }

    [Fact]
    public async Task GivenAnEmptyFolderInsideAnother_WhenAFileGoesIntoTheInnerOne_ThenNoRecordIsLeftAboveIt()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        await sut.CreateFolderAsync("Travel");
        await sut.CreateFolderAsync("Travel/Passports");

        // Act
        var entry = await sut.AddFileAsync("Passport.jpg", "Travel/Passports", "image/jpeg", [1]);

        // Assert
        Assert.Same(entry, Assert.Single(sut.Entries));
        Assert.Equal(["Travel", "Travel/Passports"], tree.GetFolders(sut.Entries));
    }

    [Fact]
    public async Task GivenAFolderMadeInsideAnEmptyOne_WhenCreated_ThenOnlyTheInnerOneKeepsARecord()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        await sut.CreateFolderAsync("Travel");

        // Act
        await sut.CreateFolderAsync("Travel/Passports");

        // Assert
        Assert.Equal("Travel/Passports", Assert.Single(sut.Entries).Folder);
        Assert.Equal(["Travel", "Travel/Passports"], tree.GetFolders(sut.Entries));
    }

    [Fact]
    public async Task GivenTheLastFileInAFolder_WhenDeleted_ThenARecordKeepsTheFolder()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var entry = await sut.AddFileAsync("Passport.jpg", "Travel/Passports", "image/jpeg", [1]);

        // Act
        await sut.DeleteFileAsync(entry.Id);

        // Assert
        var record = Assert.Single(sut.Entries);
        Assert.Equal(VaultFileKind.Folder, record.Kind);
        Assert.Equal("Travel/Passports", record.Folder);
        Assert.False(sut.HasContent(entry.Id));
        var listing = await Service(vault).ListAsync();
        Assert.Equal([record.Id], listing.EntryIds);
        Assert.Empty(listing.ContentIds);
    }

    [Fact]
    public async Task GivenOneOfTwoFilesInAFolder_WhenDeleted_ThenNoRecordIsWritten()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var first = await sut.AddFileAsync("One.jpg", "Travel", "image/jpeg", [1]);
        var second = await sut.AddFileAsync("Two.jpg", "Travel", "image/jpeg", [2]);

        // Act
        await sut.DeleteFileAsync(first.Id);

        // Assert
        Assert.Same(second, Assert.Single(sut.Entries));
    }

    [Fact]
    public async Task GivenAFileAtTheTopLevel_WhenDeleted_ThenNoRecordIsWritten()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var entry = await sut.AddFileAsync("One.jpg", null, "image/jpeg", [1]);

        // Act
        await sut.DeleteFileAsync(entry.Id);

        // Assert
        Assert.Empty(sut.Entries);
        Assert.Empty(await Store(vault.Id).ListAsync());
    }

    [Fact]
    public async Task GivenAFile_WhenItsDetailsChange_ThenItIsSaved_AndItsContentStaysHere()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var entry = await sut.AddFileAsync("Passport.jpg", null, "image/jpeg", [1, 2]);

        // Act
        var saved = await sut.UpdateFileAsync(entry.Id, "Old passport.jpg", null, "Expired");

        // Assert
        Assert.Same(saved, Assert.Single(sut.Entries));
        Assert.Equal("Old passport.jpg", saved.Name);
        Assert.Equal("Expired", saved.Notes);
        Assert.Equal("Passport.jpg", entry.Name);
        Assert.True(sut.HasContent(entry.Id));
        Assert.Equal("Old passport.jpg", (await Service(vault).GetAsync(entry.Id))!.Name);
    }

    [Fact]
    public async Task GivenTheLastFileInAFolder_WhenMovedToAnEmptyFolder_ThenTheOldFolderGetsARecord_AndTheNewOneLosesIts()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var entry = await sut.AddFileAsync("Passport.jpg", "Travel", "image/jpeg", [1]);
        await sut.CreateFolderAsync("Archive");

        // Act
        var saved = await sut.UpdateFileAsync(entry.Id, "Passport.jpg", "Archive", null);

        // Assert
        Assert.Equal("Archive", saved.Folder);
        Assert.Equal(2, sut.Entries.Count);
        var record = Assert.Single(sut.Entries, e => e.Kind == VaultFileKind.Folder);
        Assert.Equal("Travel", record.Folder);
        Assert.Equal(["Archive", "Travel"], tree.GetFolders(sut.Entries));
    }

    [Fact]
    public async Task GivenNoSuchFile_WhenItsDetailsChange_ThenThrowsFileNotFound()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        await sut.CreateFolderAsync("Travel");
        var recordId = Assert.Single(sut.Entries).Id;

        // Act
        var missing = await Assert.ThrowsAsync<ClypseException>(() => sut.UpdateFileAsync(Guid.NewGuid().ToString("D"), "a.jpg", null, null));
        var folder = await Assert.ThrowsAsync<ClypseException>(() => sut.UpdateFileAsync(recordId, "a.jpg", null, null));

        // Assert
        Assert.Equal(ClypseErrorCodes.FileNotFound, missing.Code);
        Assert.Equal(ClypseErrorCodes.FileNotFound, folder.Code);
    }

    [Fact]
    public async Task GivenAFolder_WhenMade_ThenItsRecordIsWritten()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);

        // Act
        await sut.CreateFolderAsync(" Travel / Visas ");

        // Assert
        var record = Assert.Single(sut.Entries);
        Assert.Equal(VaultFileKind.Folder, record.Kind);
        Assert.Equal("Travel/Visas", record.Folder);
        Assert.Equal([record.Id], (await Service(vault).ListAsync()).EntryIds);
    }

    [Fact]
    public async Task GivenAFolderThatExists_WhenMadeAgain_ThenThrowsFolderExists()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        await sut.AddFileAsync("Passport.jpg", "Travel/Passports", "image/jpeg", [1]);

        // Act
        var named = await Assert.ThrowsAsync<ClypseException>(() => sut.CreateFolderAsync("Travel/Passports"));
        var above = await Assert.ThrowsAsync<ClypseException>(() => sut.CreateFolderAsync("Travel"));
        var top = await Assert.ThrowsAsync<ClypseException>(() => sut.CreateFolderAsync(" "));

        // Assert
        Assert.Equal(ClypseErrorCodes.FolderExists, named.Code);
        Assert.Equal(ClypseErrorCodes.FolderExists, above.Code);
        Assert.Equal(ClypseErrorCodes.FolderNameInvalid, top.Code);
        Assert.Single(sut.Entries);
    }

    [Fact]
    public async Task GivenAFolder_WhenRenamed_ThenThePathIsRewrittenInEverythingUnderIt()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var inside = await sut.AddFileAsync("a.jpg", "Travel", "image/jpeg", [1]);
        var deeper = await sut.AddFileAsync("b.jpg", "Travel/Passports", "image/jpeg", [2]);
        await sut.CreateFolderAsync("Travel/Visas");
        var outside = await sut.AddFileAsync("c.jpg", "Work", "image/jpeg", [3]);

        // Act
        await sut.MoveFolderAsync("Travel", "Trips");

        // Assert
        Assert.Equal(["Trips", "Trips/Passports", "Trips/Visas", "Work"], tree.GetFolders(sut.Entries));
        var stored = Service(vault);
        Assert.Equal("Trips", (await stored.GetAsync(inside.Id))!.Folder);
        Assert.Equal("Trips/Passports", (await stored.GetAsync(deeper.Id))!.Folder);
        Assert.Equal("Work", (await stored.GetAsync(outside.Id))!.Folder);
        Assert.Equal(new byte[] { 2 }, await stored.ReadContentAsync(deeper.Id));
        Assert.True(sut.HasContent(deeper.Id));
    }

    [Fact]
    public async Task GivenTheOnlyFolderInAnother_WhenMovedOut_ThenTheOneItLeftGetsARecord()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        await sut.AddFileAsync("a.jpg", "Travel/Passports", "image/jpeg", [1]);
        await sut.CreateFolderAsync("Archive");

        // Act
        await sut.MoveFolderAsync("Travel/Passports", "Archive/Passports");

        // Assert
        Assert.Equal(["Archive", "Archive/Passports", "Travel"], tree.GetFolders(sut.Entries));
        var record = Assert.Single(sut.Entries, e => e.Kind == VaultFileKind.Folder);
        Assert.Equal("Travel", record.Folder);
    }

    [Fact]
    public async Task GivenAFolder_WhenMovedSomewhereItCannotGo_ThenItIsRefused_AndNothingChanges()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var first = await sut.AddFileAsync("a.jpg", "Travel", "image/jpeg", [1]);
        await sut.AddFileAsync("b.jpg", "Work", "image/jpeg", [2]);

        // Act
        var intoItself = await Assert.ThrowsAsync<ClypseException>(() => sut.MoveFolderAsync("Travel", "Travel/Inside"));
        var ontoAnother = await Assert.ThrowsAsync<ClypseException>(() => sut.MoveFolderAsync("Travel", "Work"));
        var toTheTop = await Assert.ThrowsAsync<ClypseException>(() => sut.MoveFolderAsync("Travel", " "));
        await sut.MoveFolderAsync("Travel", "Travel");
        await sut.MoveFolderAsync("Gone", "Elsewhere");

        // Assert
        Assert.Equal(ClypseErrorCodes.FolderMoveInvalid, intoItself.Code);
        Assert.Equal(ClypseErrorCodes.FolderExists, ontoAnother.Code);
        Assert.Equal(ClypseErrorCodes.FolderNameInvalid, toTheTop.Code);
        Assert.Equal(["Travel", "Work"], tree.GetFolders(sut.Entries));
        Assert.Equal("Travel", (await Service(vault).GetAsync(first.Id))!.Folder);
    }

    [Fact]
    public async Task GivenAFolder_WhenDeleted_ThenTheFilesAndRecordsUnderItAreDeleted()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        var inside = await sut.AddFileAsync("a.jpg", "Travel", "image/jpeg", [1]);
        await sut.AddFileAsync("b.jpg", "Travel/Passports", "image/jpeg", [2]);
        await sut.CreateFolderAsync("Travel/Visas");
        var outside = await sut.AddFileAsync("c.jpg", "Work", "image/jpeg", [3]);

        // Act
        await sut.DeleteFolderAsync("Travel");

        // Assert
        Assert.Same(outside, Assert.Single(sut.Entries));
        Assert.False(sut.HasContent(inside.Id));
        var listing = await Service(vault).ListAsync();
        Assert.Equal([outside.Id], listing.EntryIds);
        Assert.Equal([outside.Id], listing.ContentIds);
    }

    [Fact]
    public async Task GivenTheOnlyFolderInAnother_WhenDeleted_ThenTheOneItWasInGetsARecord()
    {
        // Arrange
        var vault = NewVault(isUnlocked: true);
        using var sut = CreateSut();
        await sut.OpenAsync(vault);
        await sut.AddFileAsync("a.jpg", "Travel/Passports", "image/jpeg", [1]);

        // Act
        await sut.DeleteFolderAsync("Travel/Passports");
        await sut.DeleteFolderAsync("Gone");
        var top = await Assert.ThrowsAsync<ClypseException>(() => sut.DeleteFolderAsync(" "));

        // Assert
        var record = Assert.Single(sut.Entries);
        Assert.Equal(VaultFileKind.Folder, record.Kind);
        Assert.Equal("Travel", record.Folder);
        Assert.Equal(ClypseErrorCodes.FolderNameInvalid, top.Code);
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

    private VaultFileSession CreateSut() => new(mockFactory.Object, tree, mockManager.Object, mockSync.Object);

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
