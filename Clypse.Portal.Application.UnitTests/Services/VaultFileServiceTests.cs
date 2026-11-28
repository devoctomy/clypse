using System.Text;
using System.Text.Json;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Models.Files;
using Clypse.Portal.Models.Settings;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Sync.InMemory;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VaultFileServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 10, 15, 0, TimeSpan.Zero);

    private readonly Mock<ISyncUserContext> mockUser = new();
    private readonly FakeTimeProvider clock = new(Now);
    private readonly VaultFileTreeService tree = new();
    private readonly ClypseSettings settings = new() { MaxFileSizeMegabytes = 1 };
    private readonly InMemorySyncFileStore files;
    private readonly Vault vault;

    public VaultFileServiceTests()
    {
        mockUser.Setup(x => x.GetUserIdAsync()).ReturnsAsync("bob");
        files = new InMemorySyncFileStore(mockUser.Object, clock);
        var id = Guid.NewGuid().ToString("D");
        vault = new Vault { Id = id, Root = $"profiles/{id}/", Name = "Personal", IsUnlocked = true };
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("vault", Assert.Throws<ArgumentNullException>(() => new VaultFileService(null!, files, tree, settings, clock)).ParamName);
        Assert.Equal("fileStore", Assert.Throws<ArgumentNullException>(() => new VaultFileService(vault, null!, tree, settings, clock)).ParamName);
        Assert.Equal("tree", Assert.Throws<ArgumentNullException>(() => new VaultFileService(vault, files, null!, settings, clock)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new VaultFileService(vault, files, tree, null!, clock)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() => new VaultFileService(vault, files, tree, settings, null!)).ParamName);
    }

    [Fact]
    public async Task GivenAFile_WhenAdded_ThenItsMetadataAndContentAreTwoFilesNamedOnlyByItsId()
    {
        // Arrange
        byte[] content = [1, 2, 3, 4, 5];

        // Act
        var entry = await CreateSut().AddFileAsync("Passport.jpg", "Travel/Passports", "image/jpeg", content);

        // Assert
        Assert.True(Guid.TryParseExact(entry.Id, "D", out _));
        var keys = (await files.ListAsync()).Select(f => f.Key).Order().ToList();
        Assert.Equal(new[] { $"{vault.Root}files/blobs/{entry.Id}.bin", $"{vault.Root}files/meta/{entry.Id}.json" }, keys);
        Assert.All(keys, key =>
        {
            Assert.DoesNotContain("Passport", key, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("jpg", key, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Travel", key, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(content, (await files.ReadAsync($"{vault.Root}files/blobs/{entry.Id}.bin"))!.Content);
    }

    [Fact]
    public async Task GivenAFile_WhenAdded_ThenItsMetadataSaysWhatItIs()
    {
        // Act
        var entry = await CreateSut().AddFileAsync("  Passport.jpg ", " Travel / Passports ", "image/jpeg", [1, 2, 3]);

        // Assert
        Assert.Equal(VaultFileKind.File, entry.Kind);
        Assert.Equal("Passport.jpg", entry.Name);
        Assert.Equal("Travel/Passports", entry.Folder);
        Assert.Equal("image/jpeg", entry.ContentType);
        Assert.Equal(3, entry.Size);
        Assert.Null(entry.Notes);
        Assert.Equal(Now, entry.CreatedAt);
        Assert.Equal(Now, entry.LastUpdatedAt);

        var file = await files.ReadAsync($"{vault.Root}files/meta/{entry.Id}.json");
        Assert.NotNull(file);
        Assert.Equal(Now, file.ModifiedAt);
        using var json = JsonDocument.Parse(file.Content);
        Assert.Equal(entry.Id, json.RootElement.GetProperty("Id").GetString());
        Assert.Equal("File", json.RootElement.GetProperty("Kind").GetString());
        Assert.Equal("Passport.jpg", json.RootElement.GetProperty("Name").GetString());
        Assert.Equal("Travel/Passports", json.RootElement.GetProperty("Folder").GetString());
        Assert.Equal("image/jpeg", json.RootElement.GetProperty("ContentType").GetString());
        Assert.Equal(3, json.RootElement.GetProperty("Size").GetInt64());
        Assert.False(json.RootElement.TryGetProperty("Notes", out _));
    }

    [Fact]
    public async Task GivenAFile_WhenAdded_ThenItsContentIsWrittenBeforeItsMetadata()
    {
        // Arrange
        var (store, calls) = RecordingStore();
        var sut = new VaultFileService(vault, store.Object, tree, settings, clock);

        // Act
        var entry = await sut.AddFileAsync("Passport.jpg", null, "image/jpeg", [1]);

        // Assert
        Assert.Equal(new[] { $"write {vault.Root}files/blobs/{entry.Id}.bin", $"write {vault.Root}files/meta/{entry.Id}.json" }, calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GivenNoContentType_WhenAdded_ThenTheFileIsOfAnUnknownType(string? contentType)
    {
        // Act
        var entry = await CreateSut().AddFileAsync("thing", null, contentType, [1]);

        // Assert
        Assert.Equal("application/octet-stream", entry.ContentType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenNoName_WhenAdded_ThenThrowsFileNameRequired_AndNothingIsWritten(string? name)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().AddFileAsync(name!, null, "image/jpeg", [1]));

        // Assert
        Assert.Equal(ClypseErrorCodes.FileNameRequired, ex.Code);
        Assert.Empty(await files.ListAsync());
    }

    [Fact]
    public async Task GivenAFileOverTheLimit_WhenAdded_ThenThrowsFileTooLarge_AndNothingIsWritten()
    {
        // Arrange
        var content = new byte[(1024 * 1024) + 1];

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().AddFileAsync("big.bin", null, null, content));

        // Assert
        Assert.Equal(ClypseErrorCodes.FileTooLarge, ex.Code);
        Assert.Equal(1, Assert.Single(ex.Arguments));
        Assert.Empty(await files.ListAsync());
    }

    [Fact]
    public async Task GivenAFileAtTheLimit_WhenAdded_ThenItIsTaken()
    {
        // Arrange
        var content = new byte[1024 * 1024];

        // Act
        var entry = await CreateSut().AddFileAsync("big.bin", null, null, content);

        // Assert
        Assert.Equal(1024 * 1024, entry.Size);
    }

    [Fact]
    public async Task GivenNoContent_WhenAdded_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateSut().AddFileAsync("a.txt", null, null, null!));
    }

    [Fact]
    public async Task GivenAnEmptyFolder_WhenAdded_ThenItIsARecordWithItsPath_AndNoContent()
    {
        // Act
        var entry = await CreateSut().AddFolderAsync("/Travel/ Visas /");

        // Assert
        Assert.Equal(VaultFileKind.Folder, entry.Kind);
        Assert.Equal("Travel/Visas", entry.Folder);
        Assert.Null(entry.Name);
        Assert.Equal(Now, entry.CreatedAt);
        var key = Assert.Single(await files.ListAsync()).Key;
        Assert.Equal($"{vault.Root}files/meta/{entry.Id}.json", key);
        using var json = JsonDocument.Parse((await files.ReadAsync(key))!.Content);
        Assert.Equal("Folder", json.RootElement.GetProperty("Kind").GetString());
        Assert.Equal("Travel/Visas", json.RootElement.GetProperty("Folder").GetString());
        Assert.False(json.RootElement.TryGetProperty("Name", out _));
        Assert.False(json.RootElement.TryGetProperty("Size", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" / ")]
    public async Task GivenTheTopLevel_WhenAddedAsAFolder_ThenThrowsFolderNameInvalid(string folder)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().AddFolderAsync(folder));

        // Assert
        Assert.Equal(ClypseErrorCodes.FolderNameInvalid, ex.Code);
        Assert.Empty(await files.ListAsync());
    }

    [Fact]
    public async Task GivenAFileAndAFolderRecord_WhenRead_ThenTheyComeBackAsTheyWere()
    {
        // Arrange
        var sut = CreateSut();
        var file = await sut.AddFileAsync("Passport.jpg", "Travel", "image/jpeg", [1, 2]);
        var folder = await sut.AddFolderAsync("Work");

        // Act
        var readFile = await sut.GetAsync(file.Id);
        var readFolder = await sut.GetAsync(folder.Id);

        // Assert
        Assert.NotNull(readFile);
        Assert.Equal(VaultFileKind.File, readFile.Kind);
        Assert.Equal("Passport.jpg", readFile.Name);
        Assert.Equal("Travel", readFile.Folder);
        Assert.Equal("image/jpeg", readFile.ContentType);
        Assert.Equal(2, readFile.Size);
        Assert.Equal(Now, readFile.CreatedAt);
        Assert.NotNull(readFolder);
        Assert.Equal(VaultFileKind.Folder, readFolder.Kind);
        Assert.Equal("Work", readFolder.Folder);
    }

    [Fact]
    public async Task GivenNoSuchFile_WhenRead_ThenNull()
    {
        // Act & Assert
        Assert.Null(await CreateSut().GetAsync(Guid.NewGuid().ToString("D")));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{"Id":"{id}","Kind":"Martian","Name":"a.txt"}""")]
    [InlineData("""{"Id":"{id}","Kind":"File"}""")]
    [InlineData("""{"Id":"{id}","Kind":"Folder"}""")]
    [InlineData("""{"Id":"11111111-1111-1111-1111-111111111111","Kind":"File","Name":"a.txt"}""")]
    public async Task GivenMetadataThatCannotBeRead_WhenRead_ThenThrowsFileUnreadable(string content)
    {
        // Arrange
        var id = Guid.NewGuid().ToString("D");
        await files.WriteAsync($"{vault.Root}files/meta/{id}.json", Encoding.UTF8.GetBytes(content.Replace("{id}", id, StringComparison.Ordinal)));

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().GetAsync(id));

        // Assert
        Assert.Equal(ClypseErrorCodes.FileUnreadable, ex.Code);
    }

    [Fact]
    public async Task GivenMetadataWithAnUntidyPath_WhenRead_ThenThePathIsTidied()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("D");
        var content = $$"""{"Id":"{{id}}","Kind":"File","Name":"a.txt","Folder":"/Travel//Old/"}""";
        await files.WriteAsync($"{vault.Root}files/meta/{id}.json", Encoding.UTF8.GetBytes(content));

        // Act
        var entry = await CreateSut().GetAsync(id);

        // Assert
        Assert.Equal("Travel/Old", entry!.Folder);
    }

    [Fact]
    public async Task GivenAFile_WhenUpdated_ThenItsNameFolderAndNotesChange_AndItsContentIsNotTouched()
    {
        // Arrange
        var (store, calls) = RecordingStore();
        var sut = new VaultFileService(vault, store.Object, tree, settings, clock);
        var entry = await sut.AddFileAsync("Passport.jpg", "Travel", "image/jpeg", [1, 2, 3]);
        calls.Clear();
        clock.Advance(TimeSpan.FromHours(1));
        var changed = new VaultFileEntry
        {
            Id = entry.Id,
            Kind = entry.Kind,
            Name = " Old passport.jpg ",
            Folder = "/Archive/",
            ContentType = entry.ContentType,
            Size = entry.Size,
            Notes = "  Expired 2021 ",
            CreatedAt = entry.CreatedAt,
            LastUpdatedAt = entry.LastUpdatedAt,
        };

        // Act
        var saved = await sut.UpdateAsync(changed);

        // Assert
        Assert.NotSame(changed, saved);
        Assert.Equal("Old passport.jpg", saved.Name);
        Assert.Equal("Archive", saved.Folder);
        Assert.Equal("Expired 2021", saved.Notes);
        Assert.Equal(Now, saved.CreatedAt);
        Assert.Equal(Now.AddHours(1), saved.LastUpdatedAt);
        Assert.Equal(3, saved.Size);
        Assert.Equal(" Old passport.jpg ", changed.Name);
        Assert.Equal(new[] { $"write {vault.Root}files/meta/{entry.Id}.json" }, calls);
    }

    [Fact]
    public async Task GivenBlankNotes_WhenUpdated_ThenTheFileHasNone()
    {
        // Arrange
        var sut = CreateSut();
        var entry = await sut.AddFileAsync("a.txt", null, "text/plain", [1]);
        entry.Notes = "   ";

        // Act
        var saved = await sut.UpdateAsync(entry);

        // Assert
        Assert.Null(saved.Notes);
        Assert.Null((await sut.GetAsync(entry.Id))!.Notes);
    }

    [Fact]
    public async Task GivenAFileWithNoName_WhenUpdated_ThenThrowsFileNameRequired()
    {
        // Arrange
        var sut = CreateSut();
        var entry = await sut.AddFileAsync("a.txt", null, "text/plain", [1]);
        entry.Name = " ";

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => sut.UpdateAsync(entry));

        // Assert
        Assert.Equal(ClypseErrorCodes.FileNameRequired, ex.Code);
        Assert.Equal("a.txt", (await sut.GetAsync(entry.Id))!.Name);
    }

    [Fact]
    public async Task GivenAFolderRecord_WhenUpdated_ThenItsPathChanges_AndItCannotBecomeTheTopLevel()
    {
        // Arrange
        var sut = CreateSut();
        var record = await sut.AddFolderAsync("Travel/Visas");
        record.Folder = "Trips/Visas";

        // Act
        var saved = await sut.UpdateAsync(record);
        saved.Folder = " ";
        var ex = await Assert.ThrowsAsync<ClypseException>(() => sut.UpdateAsync(saved));

        // Assert
        Assert.Equal("Trips/Visas", (await sut.GetAsync(record.Id))!.Folder);
        Assert.Equal(ClypseErrorCodes.FolderNameInvalid, ex.Code);
    }

    [Fact]
    public async Task GivenNoEntry_WhenUpdated_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateSut().UpdateAsync(null!));
    }

    [Fact]
    public async Task GivenAFile_WhenDeleted_ThenItsMetadataIsRemovedBeforeItsContent()
    {
        // Arrange
        var (store, calls) = RecordingStore();
        var sut = new VaultFileService(vault, store.Object, tree, settings, clock);
        var entry = await sut.AddFileAsync("Passport.jpg", null, "image/jpeg", [1]);
        calls.Clear();

        // Act
        await sut.DeleteAsync(entry.Id);

        // Assert
        Assert.Equal(new[] { $"delete {vault.Root}files/meta/{entry.Id}.json", $"delete {vault.Root}files/blobs/{entry.Id}.bin" }, calls);
    }

    [Fact]
    public async Task GivenAFolderRecord_WhenDeleted_ThenOnlyItsMetadataIsRemoved()
    {
        // Arrange
        var (store, calls) = RecordingStore();
        var sut = new VaultFileService(vault, store.Object, tree, settings, clock);
        var record = await sut.AddFolderAsync("Travel");
        calls.Clear();

        // Act
        await sut.DeleteAsync(record.Id, VaultFileKind.Folder);

        // Assert
        Assert.Equal(new[] { $"delete {vault.Root}files/meta/{record.Id}.json" }, calls);
        Assert.Empty(await files.ListAsync());
    }

    [Fact]
    public async Task GivenAFile_WhenDeleted_ThenNothingOfItIsLeft()
    {
        // Arrange
        var sut = CreateSut();
        var entry = await sut.AddFileAsync("Passport.jpg", null, "image/jpeg", [1]);

        // Act
        await sut.DeleteAsync(entry.Id);

        // Assert
        Assert.Null(await sut.GetAsync(entry.Id));
        Assert.Null(await sut.ReadContentAsync(entry.Id));
        Assert.Empty(await files.ListAsync());
    }

    [Fact]
    public async Task GivenAFile_WhenItsContentIsRead_ThenItIsWhatWasAdded()
    {
        // Arrange
        var sut = CreateSut();
        byte[] content = [9, 8, 7];
        var entry = await sut.AddFileAsync("Passport.jpg", null, "image/jpeg", content);

        // Act
        var read = await sut.ReadContentAsync(entry.Id);

        // Assert
        Assert.Equal(content, read);
    }

    [Fact]
    public async Task GivenMetadataWhoseContentIsNotHere_WhenItsContentIsRead_ThenNull()
    {
        // Arrange
        var sut = CreateSut();
        var entry = await sut.AddFileAsync("Passport.jpg", null, "image/jpeg", [1]);
        await files.DeleteAsync($"{vault.Root}files/blobs/{entry.Id}.bin");

        // Act
        var read = await sut.ReadContentAsync(entry.Id);

        // Assert
        Assert.Null(read);
    }

    [Fact]
    public async Task GivenATextFile_WhenItsContentIsSaved_ThenTheContentIsWrittenBeforeItsMetadata_WithItsNewSizeAndTime()
    {
        // Arrange
        var (store, calls) = RecordingStore();
        var sut = new VaultFileService(vault, store.Object, tree, settings, clock);
        var entry = await sut.AddFileAsync("Notes.md", "Work", "text/markdown", Encoding.UTF8.GetBytes("one"));
        calls.Clear();
        clock.Advance(TimeSpan.FromMinutes(5));
        var content = Encoding.UTF8.GetBytes("one two three");

        // Act
        var saved = await sut.SaveContentAsync(entry.Id, content);

        // Assert
        Assert.Equal(new[] { $"write {vault.Root}files/blobs/{entry.Id}.bin", $"write {vault.Root}files/meta/{entry.Id}.json" }, calls);
        Assert.Equal(content.Length, saved.Size);
        Assert.Equal(Now, saved.CreatedAt);
        Assert.Equal(Now.AddMinutes(5), saved.LastUpdatedAt);
        Assert.Equal("Notes.md", saved.Name);
        Assert.Equal("Work", saved.Folder);
        Assert.Equal(content, await sut.ReadContentAsync(entry.Id));
        Assert.Equal(content.Length, (await sut.GetAsync(entry.Id))!.Size);
    }

    [Fact]
    public async Task GivenNoSuchFile_WhenItsContentIsSaved_ThenThrowsFileNotFound_AndNothingIsWritten()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().SaveContentAsync(Guid.NewGuid().ToString("D"), [1]));

        // Assert
        Assert.Equal(ClypseErrorCodes.FileNotFound, ex.Code);
        Assert.Empty(await files.ListAsync());
    }

    [Fact]
    public async Task GivenAFolderRecord_WhenContentIsSavedToIt_ThenThrowsFileNotFound()
    {
        // Arrange
        var sut = CreateSut();
        var record = await sut.AddFolderAsync("Travel");

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => sut.SaveContentAsync(record.Id, [1]));

        // Assert
        Assert.Equal(ClypseErrorCodes.FileNotFound, ex.Code);
    }

    [Fact]
    public async Task GivenContentOverTheLimit_WhenSaved_ThenThrowsFileTooLarge_AndTheFileIsAsItWas()
    {
        // Arrange
        var sut = CreateSut();
        var entry = await sut.AddFileAsync("Notes.md", null, "text/markdown", [1, 2]);

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => sut.SaveContentAsync(entry.Id, new byte[(1024 * 1024) + 1]));

        // Assert
        Assert.Equal(ClypseErrorCodes.FileTooLarge, ex.Code);
        Assert.Equal(new byte[] { 1, 2 }, await sut.ReadContentAsync(entry.Id));
    }

    [Fact]
    public async Task GivenAVaultsFiles_WhenListed_ThenItsMetadataAndContentIdsAreFound_AndNothingElse()
    {
        // Arrange
        var sut = CreateSut();
        var first = await sut.AddFileAsync("a.jpg", null, "image/jpeg", [1]);
        var second = await sut.AddFileAsync("b.jpg", "Travel", "image/jpeg", [2]);
        var record = await sut.AddFolderAsync("Work");
        await files.DeleteAsync($"{vault.Root}files/blobs/{second.Id}.bin");
        var other = Guid.NewGuid().ToString("D");
        await files.WriteAsync($"{vault.Root}secrets/{other}.json", [1]);
        await files.WriteAsync($"{vault.Root}.vault", [1]);
        await files.WriteAsync($"{vault.Root}files/meta/passport.json", [1]);
        await files.WriteAsync($"{vault.Root}files/meta/{other}.txt", [1]);
        await files.WriteAsync($"{vault.Root}files/blobs/{other}.json", [1]);
        await files.CreateFolderAsync($"{vault.Root}files/meta/");

        // Act
        var listing = await sut.ListAsync();

        // Assert
        Assert.Equal(new[] { first.Id, second.Id, record.Id }.Order(), listing.EntryIds.Order());
        Assert.Equal([first.Id], listing.ContentIds);
    }

    [Fact]
    public void GivenAKey_WhenAsked_ThenOnlyThisVaultsMetadataAndContentHaveIds()
    {
        // Arrange
        var sut = CreateSut();
        var id = Guid.NewGuid().ToString("D");

        // Act & Assert
        Assert.Equal(id, sut.GetEntryId($"{vault.Root}files/meta/{id}.json"));
        Assert.Null(sut.GetEntryId($"{vault.Root}files/blobs/{id}.bin"));
        Assert.Null(sut.GetEntryId($"{vault.Root}files/meta/{id}.bin"));
        Assert.Null(sut.GetEntryId($"{vault.Root}files/meta/passport.json"));
        Assert.Null(sut.GetEntryId($"{vault.Root}files/meta/sub/{id}.json"));
        Assert.Null(sut.GetEntryId($"{vault.Root}secrets/{id}.json"));
        Assert.Null(sut.GetEntryId($"profiles/{Guid.NewGuid():D}/files/meta/{id}.json"));
        Assert.Null(sut.GetEntryId(null!));

        Assert.Equal(id, sut.GetContentId($"{vault.Root}files/blobs/{id}.bin"));
        Assert.Null(sut.GetContentId($"{vault.Root}files/meta/{id}.json"));
        Assert.Null(sut.GetContentId($"{vault.Root}files/blobs/{id}.jpg"));
        Assert.Null(sut.GetContentId($"{vault.Root}files/blobs/passport.bin"));
        Assert.Null(sut.GetContentId($"profiles/{Guid.NewGuid():D}/files/blobs/{id}.bin"));
        Assert.Null(sut.GetContentId(null!));

        Assert.Equal($"{vault.Root}files/blobs/{id}.bin", sut.GetContentKey(id));
        Assert.Throws<ClypseException>(() => sut.GetContentKey("passport"));
    }

    [Theory]
    [InlineData("passport")]
    [InlineData("../secrets/x")]
    [InlineData("")]
    public async Task GivenAnIdThatIsNotAGuid_WhenUsed_ThenThrowsFileIdInvalid(string id)
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        Assert.Equal(ClypseErrorCodes.FileIdInvalid, (await Assert.ThrowsAsync<ClypseException>(() => sut.GetAsync(id))).Code);
        Assert.Equal(ClypseErrorCodes.FileIdInvalid, (await Assert.ThrowsAsync<ClypseException>(() => sut.DeleteAsync(id))).Code);
        Assert.Equal(ClypseErrorCodes.FileIdInvalid, (await Assert.ThrowsAsync<ClypseException>(() => sut.ReadContentAsync(id))).Code);
        Assert.Equal(ClypseErrorCodes.FileIdInvalid, (await Assert.ThrowsAsync<ClypseException>(() => sut.SaveContentAsync(id, [1]))).Code);
        Assert.Equal(
            ClypseErrorCodes.FileIdInvalid,
            (await Assert.ThrowsAsync<ClypseException>(() => sut.UpdateAsync(new VaultFileEntry { Id = id, Name = "a.txt" }))).Code);
    }

    private VaultFileService CreateSut() => new(vault, files, tree, settings, clock);

    // A store over the in memory one that notes each write and delete, in order.
    private (Mock<ISyncFileStore> Store, List<string> Calls) RecordingStore()
    {
        var calls = new List<string>();
        var store = new Mock<ISyncFileStore>();
        store.Setup(x => x.ListAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken ct) => files.ListAsync(ct));
        store.Setup(x => x.ReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string key, CancellationToken ct) => files.ReadAsync(key, ct));
        store.Setup(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .Returns((string key, byte[] content, DateTimeOffset? modifiedAt, CancellationToken ct) =>
            {
                calls.Add($"write {key}");
                return files.WriteAsync(key, content, modifiedAt, ct);
            });
        store.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string key, CancellationToken ct) =>
            {
                calls.Add($"delete {key}");
                return files.DeleteAsync(key, ct);
            });
        return (store, calls);
    }
}
