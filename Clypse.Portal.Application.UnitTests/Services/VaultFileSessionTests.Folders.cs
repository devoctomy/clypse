using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Models.Files;
using Clypse.Portal.Models.Vaults;

namespace Clypse.Portal.Application.UnitTests.Services;

// Folders: when a folder keeps a record of its own, and moving, renaming and deleting folders and the files in them.
public partial class VaultFileSessionTests
{
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
}
