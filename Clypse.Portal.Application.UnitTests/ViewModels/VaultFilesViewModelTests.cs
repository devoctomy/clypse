using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Files;
using Clypse.Portal.Models.Settings;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class VaultFilesViewModelTests
{
    private static readonly DateTimeOffset Added = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultFileSession> mockSession = new();
    private readonly VaultFileTreeService tree = new();
    private readonly ClypseSettings settings = new() { MaxFileSizeMegabytes = 1 };
    private readonly Vault vault = new() { Id = "v1", Root = "profiles/v1/", Name = "Personal", IsUnlocked = true };
    private List<VaultFileEntry> entries = [];

    public VaultFilesViewModelTests()
    {
        mockSession.SetupGet(x => x.Vault).Returns(vault);
        mockSession.SetupGet(x => x.Entries).Returns(() => entries);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();
        var types = new FileTypeService(t);

        // Act & Assert
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new VaultFilesViewModel(null!, tree, types, settings, text.Errors, text.Plurals, t)).ParamName);
        Assert.Equal("tree", Assert.Throws<ArgumentNullException>(() => new VaultFilesViewModel(mockSession.Object, null!, types, settings, text.Errors, text.Plurals, t)).ParamName);
        Assert.Equal("fileTypes", Assert.Throws<ArgumentNullException>(() => new VaultFilesViewModel(mockSession.Object, tree, null!, settings, text.Errors, text.Plurals, t)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new VaultFilesViewModel(mockSession.Object, tree, types, null!, text.Errors, text.Plurals, t)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new VaultFilesViewModel(mockSession.Object, tree, types, settings, null!, text.Plurals, t)).ParamName);
        Assert.Equal("plurals", Assert.Throws<ArgumentNullException>(() => new VaultFilesViewModel(mockSession.Object, tree, types, settings, text.Errors, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new VaultFilesViewModel(mockSession.Object, tree, types, settings, text.Errors, text.Plurals, null!)).ParamName);
    }

    [Fact]
    public void GivenFilesAndFolders_WhenShown_ThenTheTopLevelListsItsFoldersThenItsFiles_ByName()
    {
        // Arrange
        entries = [File("zebra.jpg", string.Empty), File("Apple.jpg", string.Empty), File("deep.jpg", "Travel/Passports"), Record("work")];

        // Act
        var sut = CreateSut();

        // Assert
        Assert.Equal("Personal", sut.VaultName);
        Assert.Equal(string.Empty, sut.Folder);
        Assert.Empty(sut.Breadcrumb);
        Assert.Equal(["Travel", "work"], sut.Folders.Select(f => f.Name));
        Assert.Equal(["Apple.jpg", "zebra.jpg"], sut.Files.Select(f => f.Name));
        Assert.False(sut.IsEmpty);
        Assert.False(sut.IsSearching);
    }

    [Fact]
    public void GivenNoFilesOrFolders_WhenShown_ThenTheTabIsEmpty()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.True(sut.IsEmpty);
        Assert.Empty(sut.Folders);
        Assert.Empty(sut.Files);
    }

    [Fact]
    public void GivenAFolder_WhenOpened_ThenItsContentsAndBreadcrumbAreShown_AndAnySearchIsCleared()
    {
        // Arrange
        entries = [File("top.jpg", string.Empty), File("b.jpg", "Travel/Passports"), File("a.jpg", "Travel/Passports"), File("old.jpg", "Travel/Passports/Old")];
        var sut = CreateSut();
        sut.Search = "top";
        sut.ToggleNewMenu();

        // Act
        sut.OpenFolder(" /Travel/Passports/ ");

        // Assert
        Assert.Equal("Travel/Passports", sut.Folder);
        Assert.Equal(["Travel", "Travel/Passports"], sut.Breadcrumb.Select(f => f.Path));
        Assert.Equal(["Old"], sut.Folders.Select(f => f.Name));
        Assert.Equal(["a.jpg", "b.jpg"], sut.Files.Select(f => f.Name));
        Assert.Equal(string.Empty, sut.Search);
        Assert.False(sut.IsNewMenuOpen);
    }

    [Fact]
    public void GivenSearchText_WhenSet_ThenFilesInEveryFolderAreSearchedByNameAndNotes_AndFoldersAreNotListed()
    {
        // Arrange
        var noted = File("scan.jpg", "Travel");
        noted.Notes = "My PASSPORT, expires 2031";
        entries = [File("Passport.jpg", "Travel/Old"), noted, File("ticket.pdf", "Travel"), Record("Passport copies")];
        var sut = CreateSut();
        sut.OpenFolder("Travel");

        // Act
        sut.Search = "  passport ";

        // Assert
        Assert.True(sut.IsSearching);
        Assert.Equal(["Passport.jpg", "scan.jpg"], sut.Files.Select(f => f.Name));
        Assert.Empty(sut.Folders);
    }

    [Fact]
    public void GivenSearchTextThatMatchesNothing_WhenSet_ThenNoFilesAreListed_UntilItIsCleared()
    {
        // Arrange
        entries = [File("Passport.jpg", string.Empty)];
        var sut = CreateSut();

        // Act
        sut.Search = "nothing like it";
        var whileSearching = sut.Files.Count;
        sut.Search = null!;

        // Assert
        Assert.Equal(0, whileSearching);
        Assert.Equal(string.Empty, sut.Search);
        Assert.Single(sut.Files);
    }

    [Fact]
    public void GivenTheMetadataIsBeingRead_WhenShown_ThenTheProgressIsDescribed()
    {
        // Arrange
        mockSession.SetupGet(x => x.IsLoading).Returns(true);
        mockSession.SetupGet(x => x.TotalCount).Returns(8);
        mockSession.SetupGet(x => x.LoadedCount).Returns(2);

        // Act
        var sut = CreateSut();

        // Assert
        Assert.True(sut.IsLoading);
        Assert.Equal(25, sut.LoadingPercent);
        Assert.Equal("Decrypting the details of 2 of 8 files...", sut.LoadingText);
    }

    [Fact]
    public void GivenNothingToRead_WhenShown_ThenTheProgressIsZero_AndNothingIsUnreadable()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.Equal(0, sut.LoadingPercent);
        Assert.Null(sut.UnreadableText);
    }

    [Theory]
    [InlineData(1, "1 file in this vault could not be read.")]
    [InlineData(3, "3 files in this vault could not be read.")]
    public void GivenMetadataThatCouldNotBeRead_WhenShown_ThenTheWarningSaysHowMany(int count, string expected)
    {
        // Arrange
        mockSession.SetupGet(x => x.UnreadableCount).Returns(count);

        // Act
        var sut = CreateSut();

        // Assert
        Assert.Equal(expected, sut.UnreadableText);
    }

    [Fact]
    public void GivenTheSessionChanges_WhenRaised_ThenTheListFollows()
    {
        // Arrange
        entries = [File("one.jpg", string.Empty)];
        var sut = CreateSut();
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        // Act
        entries = [File("one.jpg", string.Empty), File("two.jpg", string.Empty)];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal(["one.jpg", "two.jpg"], sut.Files.Select(f => f.Name));
        Assert.Contains(nameof(VaultFilesViewModel.Files), changed);
        Assert.Contains(nameof(VaultFilesViewModel.IsLoading), changed);
    }

    [Fact]
    public void GivenTheOpenFolderGoes_WhenTheSessionChanges_ThenTheNearestFolderLeftAboveItIsShown()
    {
        // Arrange
        entries = [File("a.jpg", "Travel/Passports/Old"), File("b.jpg", "Travel")];
        var sut = CreateSut();
        sut.OpenFolder("Travel/Passports/Old");

        // Act
        entries = [File("b.jpg", "Travel")];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal("Travel", sut.Folder);
        Assert.Equal(["b.jpg"], sut.Files.Select(f => f.Name));
    }

    [Fact]
    public void GivenEverythingGoes_WhenTheSessionChanges_ThenTheTopLevelIsShown()
    {
        // Arrange
        entries = [File("a.jpg", "Travel")];
        var sut = CreateSut();
        sut.OpenFolder("Travel");

        // Act
        entries = [];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal(string.Empty, sut.Folder);
        Assert.True(sut.IsEmpty);
    }

    [Fact]
    public void GivenAFile_WhenDescribed_ThenItHasItsIcon_AndItsTypeSizeAndWhenItWasAdded()
    {
        // Arrange
        var file = File("Passport.jpg", string.Empty);
        file.ContentType = "image/jpeg";
        file.Size = 2048;
        mockSession.Setup(x => x.HasContent(file.Id)).Returns(true);
        var sut = CreateSut();

        // Act
        var icon = sut.GetIcon(file);
        var summary = sut.GetSummary(file);

        // Assert
        Assert.Equal("bi-file-earmark-image", icon);
        Assert.StartsWith("Image · 2 KB · Added ", summary, StringComparison.Ordinal);
        Assert.True(sut.HasContent(file));
        Assert.False(sut.HasContent(File("other.jpg", string.Empty)));
        Assert.Throws<ArgumentNullException>(() => sut.GetSummary(null!));
        Assert.Throws<ArgumentNullException>(() => sut.HasContent(null!));
    }

    [Fact]
    public void GivenTheNewMenu_WhenToggled_ThenItOpensAndCloses()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ToggleNewMenu();
        var opened = sut.IsNewMenuOpen;
        sut.ToggleNewMenu();
        var toggledShut = sut.IsNewMenuOpen;
        sut.ToggleNewMenu();
        sut.CloseNewMenu();

        // Assert
        Assert.True(opened);
        Assert.False(toggledShut);
        Assert.False(sut.IsNewMenuOpen);
    }

    [Fact]
    public void GivenAnOpenFolder_WhenFolderIsChosenFromTheNewMenu_ThenTheNameDialogOpensForThatFolder_AndTheMenuCloses()
    {
        // Arrange
        entries = [File("a.jpg", "Travel")];
        var sut = CreateSut();
        sut.OpenFolder("Travel");
        sut.ToggleNewMenu();

        // Act
        sut.NewFolder();

        // Assert
        Assert.Equal(VaultFilesViewModel.FilesDialog.NewFolder, sut.ActiveDialog);
        Assert.Equal("Travel", sut.DialogFolder);
        Assert.Null(sut.DialogFileId);
        Assert.False(sut.IsNewMenuOpen);
    }

    [Fact]
    public void GivenAFolderAndAFile_WhenTheirDialogsAreAskedFor_ThenEachOpensForItsTarget_AndClosingClearsIt()
    {
        // Arrange
        var file = File("a.jpg", "Travel");
        entries = [file];
        var sut = CreateSut();

        // Act
        sut.RenameFolder("/Travel/");
        var rename = (sut.ActiveDialog, sut.DialogFolder, sut.DialogFileId);
        sut.ShowDetails(file.Id);
        var details = (sut.ActiveDialog, sut.DialogFolder, sut.DialogFileId);
        sut.CloseDialog();

        // Assert
        Assert.Equal((VaultFilesViewModel.FilesDialog.RenameFolder, "Travel", null), rename);
        Assert.Equal((VaultFilesViewModel.FilesDialog.Details, null, file.Id), details);
        Assert.Equal(VaultFilesViewModel.FilesDialog.None, sut.ActiveDialog);
        Assert.Null(sut.DialogFolder);
        Assert.Null(sut.DialogFileId);
    }

    [Fact]
    public void GivenAFile_WhenOpened_ThenTheViewerOpensForIt_AndTheNewMenuCloses()
    {
        // Arrange
        var file = File("Passport.jpg", "Travel");
        entries = [file];
        var sut = CreateSut();
        sut.ToggleNewMenu();

        // Act
        sut.OpenFileCommand.Execute(file.Id);

        // Assert
        Assert.Equal(VaultFilesViewModel.FilesDialog.Viewer, sut.ActiveDialog);
        Assert.Equal(file.Id, sut.DialogFileId);
        Assert.Null(sut.DialogFolder);
        Assert.False(sut.IsNewMenuOpen);
    }

    [Theory]
    [InlineData("Notes.md", "text/markdown")]
    [InlineData("Notes.md", "")]
    [InlineData("todo.txt", "text/plain")]
    public void GivenATextFile_WhenOpened_ThenTheEditorTakesTheListsPlace_AndNoDialogOpens(string name, string contentType)
    {
        // Arrange
        var file = File(name, string.Empty);
        file.ContentType = contentType;
        entries = [file];
        var sut = CreateSut();
        sut.ShowDetails(file.Id);

        // Act
        sut.OpenFile(file.Id);
        var editing = sut.EditingFileId;
        sut.CloseEditorCommand.Execute(null);

        // Assert
        Assert.Equal(file.Id, editing);
        Assert.Equal(VaultFilesViewModel.FilesDialog.None, sut.ActiveDialog);
        Assert.Null(sut.DialogFileId);
        Assert.Null(sut.EditingFileId);
    }

    [Fact]
    public void GivenATextFileLongerThanTheEditorTakes_WhenOpened_ThenItsDetailsShowInTheViewer()
    {
        // Arrange
        var file = File("log.txt", string.Empty);
        file.ContentType = "text/plain";
        file.Size = TextFileEditorViewModel.MaximumTextSize + 1;
        entries = [file];
        var sut = CreateSut();

        // Act
        sut.OpenFile(file.Id);

        // Assert
        Assert.Null(sut.EditingFileId);
        Assert.Equal(VaultFilesViewModel.FilesDialog.Viewer, sut.ActiveDialog);
        Assert.Equal(file.Id, sut.DialogFileId);
    }

    [Fact]
    public void GivenAFileThatIsGone_WhenOpened_ThenTheViewerSaysSo_RatherThanTheEditor()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.OpenFile(Guid.NewGuid().ToString("D"));

        // Assert
        Assert.Null(sut.EditingFileId);
        Assert.Equal(VaultFilesViewModel.FilesDialog.Viewer, sut.ActiveDialog);
    }

    [Fact]
    public void GivenAnOpenFolder_WhenADocumentIsChosenFromTheNewMenu_ThenTheNameDialogOpensForThatKind_InThatFolder()
    {
        // Arrange
        entries = [File("a.jpg", "Travel")];
        var sut = CreateSut();
        sut.OpenFolder("Travel");
        sut.ToggleNewMenu();

        // Act
        sut.NewMarkdownCommand.Execute(null);
        var markdown = (sut.ActiveDialog, sut.DialogFolder, sut.IsNewMenuOpen);
        sut.NewTextCommand.Execute(null);

        // Assert
        Assert.Equal((VaultFilesViewModel.FilesDialog.NewMarkdown, "Travel", false), markdown);
        Assert.Equal(VaultFilesViewModel.FilesDialog.NewText, sut.ActiveDialog);
        Assert.Equal("Travel", sut.DialogFolder);
    }

    [Fact]
    public void GivenTheViewer_WhenDetailsOrDeleteIsAskedFor_ThenThatDialogTakesItsPlaceForTheSameFile()
    {
        // Arrange
        var file = File("Passport.jpg", string.Empty);
        entries = [file];
        var sut = CreateSut();
        sut.OpenFile(file.Id);

        // Act
        sut.ShowDetails(file.Id);
        var details = (sut.ActiveDialog, sut.DialogFileId);
        sut.OpenFile(file.Id);
        sut.AskDeleteFile(file.Id);

        // Assert
        Assert.Equal((VaultFilesViewModel.FilesDialog.Details, file.Id), details);
        Assert.Equal(VaultFilesViewModel.FilesDialog.DeleteFile, sut.ActiveDialog);
        Assert.Equal(file.Id, sut.DialogFileId);
    }

    [Fact]
    public void GivenAFile_WhenAskedWhetherToDeleteIt_ThenTheQuestionNamesIt()
    {
        // Arrange
        var file = File("Passport.jpg", "Travel");
        entries = [file];
        var sut = CreateSut();

        // Act
        sut.AskDeleteFile(file.Id);

        // Assert
        Assert.Equal(VaultFilesViewModel.FilesDialog.DeleteFile, sut.ActiveDialog);
        Assert.Equal("Delete Passport.jpg? This cannot be undone.", sut.DeleteQuestion);
    }

    [Fact]
    public void GivenAFolder_WhenAskedWhetherToDeleteIt_ThenTheQuestionSaysHowManyFilesGoWithIt()
    {
        // Arrange
        entries = [File("a.jpg", "Travel/Passports"), File("b.jpg", "Travel/Passports/Old"), File("c.jpg", "Travel/Visas"), Record("Travel/Empty"), Record("Work")];
        var sut = CreateSut();

        // Act
        sut.AskDeleteFolder("Travel/Passports");
        var two = sut.DeleteQuestion;
        sut.AskDeleteFolder("Travel/Visas");
        var one = sut.DeleteQuestion;
        sut.AskDeleteFolder("Travel/Empty");
        var none = sut.DeleteQuestion;

        // Assert
        Assert.Equal(VaultFilesViewModel.FilesDialog.DeleteFolder, sut.ActiveDialog);
        Assert.Equal("Delete the folder Passports and the 2 files in it? This cannot be undone.", two);
        Assert.Equal("Delete the folder Visas and the 1 file in it? This cannot be undone.", one);
        Assert.Equal("Delete the empty folder Empty?", none);
    }

    [Fact]
    public async Task GivenADeleteQuestionAboutAFile_WhenConfirmed_ThenTheFileIsDeleted_AndTheDialogCloses()
    {
        // Arrange
        var file = File("Passport.jpg", string.Empty);
        entries = [file];
        var sut = CreateSut();
        sut.AskDeleteFile(file.Id);

        // Act
        await sut.ConfirmDeleteAsync();

        // Assert
        mockSession.Verify(x => x.DeleteFileAsync(file.Id, It.IsAny<CancellationToken>()), Times.Once);
        mockSession.Verify(x => x.DeleteFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(VaultFilesViewModel.FilesDialog.None, sut.ActiveDialog);
        Assert.False(sut.IsWorking);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenADeleteQuestionAboutAFolder_WhenConfirmed_ThenTheFolderIsDeleted_AndTheDialogCloses()
    {
        // Arrange
        entries = [File("a.jpg", "Travel")];
        var sut = CreateSut();
        sut.AskDeleteFolder("Travel");

        // Act
        await sut.ConfirmDeleteAsync();

        // Assert
        mockSession.Verify(x => x.DeleteFolderAsync("Travel", It.IsAny<CancellationToken>()), Times.Once);
        mockSession.Verify(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(VaultFilesViewModel.FilesDialog.None, sut.ActiveDialog);
    }

    [Fact]
    public async Task GivenDeletingFails_WhenConfirmed_ThenTheErrorIsShown_AndTheDialogCloses()
    {
        // Arrange
        var file = File("Passport.jpg", string.Empty);
        entries = [file];
        mockSession.Setup(x => x.DeleteFileAsync(file.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClypseException(ClypseErrorCodes.NoVaultOpen));
        var sut = CreateSut();
        sut.AskDeleteFile(file.Id);

        // Act
        await sut.ConfirmDeleteAsync();
        var shown = sut.ErrorMessage;
        sut.DismissError();

        // Assert
        Assert.Equal("Open a vault first.", shown);
        Assert.Equal(VaultFilesViewModel.FilesDialog.None, sut.ActiveDialog);
        Assert.False(sut.IsWorking);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenNoDeleteQuestion_WhenConfirmed_ThenNothingIsDeleted()
    {
        // Arrange
        var file = File("Passport.jpg", string.Empty);
        entries = [file];
        var sut = CreateSut();
        sut.ShowDetails(file.Id);

        // Act
        await sut.ConfirmDeleteAsync();

        // Assert
        mockSession.Verify(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(VaultFilesViewModel.FilesDialog.Details, sut.ActiveDialog);
    }

    [Fact]
    public async Task GivenPickedFiles_WhenAdded_ThenEachGoesIntoTheOpenFolder_WithItsTypeWorkedOut()
    {
        // Arrange
        entries = [File("a.jpg", "Travel")];
        var sut = CreateSut();
        sut.OpenFolder("Travel");
        var progress = new List<string?>();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(VaultFilesViewModel.WorkingText))
            {
                progress.Add(sut.WorkingText);
            }
        };
        var picked = new[] { Picked("Passport.jpg", "image/jpeg", [1, 2, 3]), Picked("notes.md", string.Empty, [4]) };

        // Act
        await sut.AddFilesAsync(picked);

        // Assert
        mockSession.Verify(x => x.AddFileAsync("Passport.jpg", "Travel", "image/jpeg", new byte[] { 1, 2, 3 }, It.IsAny<CancellationToken>()), Times.Once);
        mockSession.Verify(x => x.AddFileAsync("notes.md", "Travel", "text/markdown", new byte[] { 4 }, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(["Adding 1 of 2: Passport.jpg", "Adding 2 of 2: notes.md", null], progress);
        Assert.False(sut.IsWorking);
        Assert.Empty(sut.AddErrors);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenAFileOverTheLimit_WhenAdded_ThenItIsNotRead_AndTheOthersAreStillAdded()
    {
        // Arrange
        var read = false;
        var tooLarge = new FileToAdd
        {
            Name = "big.bin",
            Size = (1024 * 1024) + 1,
            ReadAsync = _ =>
            {
                read = true;
                return Task.FromResult(Array.Empty<byte>());
            },
        };
        var sut = CreateSut();

        // Act
        await sut.AddFilesAsync([tooLarge, Picked("small.txt", "text/plain", [1])]);

        // Assert
        Assert.False(read);
        Assert.Equal(["big.bin was not added. That file is too large. A vault takes files of up to 1 MB."], sut.AddErrors);
        mockSession.Verify(x => x.AddFileAsync("small.txt", string.Empty, "text/plain", new byte[] { 1 }, It.IsAny<CancellationToken>()), Times.Once);
        mockSession.Verify(x => x.AddFileAsync("big.bin", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(sut.IsWorking);
    }

    [Fact]
    public async Task GivenAFileThatCannotBeStored_WhenAdded_ThenItIsListedAsNotAdded_AndCanBeDismissed()
    {
        // Arrange
        mockSession.Setup(x => x.AddFileAsync("locked.jpg", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClypseException(ClypseErrorCodes.NoVaultOpen));
        var sut = CreateSut();

        // Act
        await sut.AddFilesAsync([Picked("locked.jpg", "image/jpeg", [1])]);
        var failures = sut.AddErrors;
        sut.DismissError();

        // Assert
        Assert.Equal(["locked.jpg was not added. Open a vault first."], failures);
        Assert.Empty(sut.AddErrors);
    }

    [Fact]
    public async Task GivenMoreFilesThanCanBeAddedAtOnce_WhenAdded_ThenNoneIsRead_AndTheErrorSaysHowMany()
    {
        // Arrange
        var sut = CreateSut();
        var picked = Enumerable.Range(0, VaultFilesViewModel.MaximumFilesAtOnce + 1).Select(i => Picked($"{i}.txt", "text/plain", [1])).ToList();

        // Act
        await sut.AddFilesAsync(picked);

        // Assert
        Assert.Equal("Add up to 20 files at a time.", sut.ErrorMessage);
        mockSession.Verify(x => x.AddFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoFiles_WhenAdded_ThenNothingHappens_AndNullThrows()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.AddFilesAsync([]);

        // Assert
        Assert.False(sut.IsWorking);
        mockSession.Verify(x => x.AddFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.AddFilesAsync(null!));
    }

    [Fact]
    public void GivenADisposedViewModel_WhenTheSessionChanges_ThenItNoLongerFollows()
    {
        // Arrange
        entries = [File("one.jpg", string.Empty)];
        var sut = CreateSut();
        sut.Dispose();

        // Act
        entries = [];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Single(sut.Files);
    }

    private static VaultFileEntry File(string name, string folder)
    {
        return new VaultFileEntry
        {
            Id = Guid.NewGuid().ToString("D"),
            Kind = VaultFileKind.File,
            Name = name,
            Folder = folder,
            ContentType = "image/jpeg",
            Size = 3,
            CreatedAt = Added,
            LastUpdatedAt = Added,
        };
    }

    private static VaultFileEntry Record(string folder)
    {
        return new VaultFileEntry { Id = Guid.NewGuid().ToString("D"), Kind = VaultFileKind.Folder, Folder = folder, CreatedAt = Added, LastUpdatedAt = Added };
    }

    private static FileToAdd Picked(string name, string contentType, byte[] content)
    {
        return new FileToAdd { Name = name, ContentType = contentType, Size = content.Length, ReadAsync = _ => Task.FromResult(content) };
    }

    private VaultFilesViewModel CreateSut()
    {
        var t = text.For<ClypseText>();
        return new VaultFilesViewModel(mockSession.Object, tree, new FileTypeService(t), settings, text.Errors, text.Plurals, t);
    }
}
