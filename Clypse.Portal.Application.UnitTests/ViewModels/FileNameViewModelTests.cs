using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Files;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class FileNameViewModelTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultFileSession> mockSession = new();
    private readonly VaultFileTreeService tree = new();

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new FileNameViewModel(null!, tree, text.Errors, t)).ParamName);
        Assert.Equal("tree", Assert.Throws<ArgumentNullException>(() => new FileNameViewModel(mockSession.Object, null!, text.Errors, t)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new FileNameViewModel(mockSession.Object, tree, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new FileNameViewModel(mockSession.Object, tree, text.Errors, null!)).ParamName);
    }

    [Fact]
    public void GivenANewFolder_WhenLoaded_ThenTheNameIsEmpty_AndTheDialogSaysWhatItMakes()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Load(FileNameViewModel.FileNameMode.NewFolder, "Travel");

        // Assert
        Assert.Equal(FileNameViewModel.FileNameMode.NewFolder, sut.Mode);
        Assert.Equal(string.Empty, sut.Name);
        Assert.Equal("New folder", sut.Title);
        Assert.Equal("Create", sut.SaveText);
        Assert.Null(sut.Error);
    }

    [Fact]
    public void GivenAFolderToRename_WhenLoaded_ThenTheNameIsItsOwn_AndTheDialogSaysItRenames()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Load(FileNameViewModel.FileNameMode.RenameFolder, "/Travel/Passports/");

        // Assert
        Assert.Equal("Passports", sut.Name);
        Assert.Equal("Rename folder", sut.Title);
        Assert.Equal("Rename", sut.SaveText);
    }

    [Theory]
    [InlineData("Travel", "Visas", "Travel/Visas")]
    [InlineData("", " Travel ", "Travel")]
    [InlineData(null, "Travel", "Travel")]
    public async Task GivenANewFoldersName_WhenSaved_ThenTheFolderIsMadeInTheFolderItWasAskedFor(string? parent, string name, string expected)
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(FileNameViewModel.FileNameMode.NewFolder, parent);
        sut.Name = name;

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.True(saved);
        Assert.Null(sut.Error);
        Assert.False(sut.IsWorking);
        mockSession.Verify(x => x.CreateFolderAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
        mockSession.Verify(x => x.MoveFolderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Travel/Passports", "Old passports", "Travel/Old passports")]
    [InlineData("Travel", "Trips", "Trips")]
    public async Task GivenAFoldersNewName_WhenSaved_ThenTheFolderIsRenamedWhereItIs(string folder, string name, string expected)
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(FileNameViewModel.FileNameMode.RenameFolder, folder);
        sut.Name = name;

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.True(saved);
        mockSession.Verify(x => x.MoveFolderAsync(folder, expected, It.IsAny<CancellationToken>()), Times.Once);
        mockSession.Verify(x => x.CreateFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Travel/Visas")]
    public async Task GivenANameAFolderCannotHave_WhenSaved_ThenItSaysSo_AndNothingIsMade(string name)
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(FileNameViewModel.FileNameMode.NewFolder, string.Empty);
        sut.Name = name;

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.False(saved);
        Assert.Equal("Give the folder a name, without a / in it.", sut.Error);
        Assert.False(sut.IsWorking);
        mockSession.Verify(x => x.CreateFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenAFolderThatAlreadyExists_WhenSaved_ThenItSaysSo_AndANewLoadClearsIt()
    {
        // Arrange
        mockSession.Setup(x => x.CreateFolderAsync("Travel", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClypseException(ClypseErrorCodes.FolderExists));
        var sut = CreateSut();
        sut.Load(FileNameViewModel.FileNameMode.NewFolder, string.Empty);
        sut.Name = "Travel";

        // Act
        var saved = await sut.SaveAsync();
        var shown = sut.Error;
        sut.Load(FileNameViewModel.FileNameMode.NewFolder, string.Empty);

        // Assert
        Assert.False(saved);
        Assert.Equal("There is already a folder with that name.", shown);
        Assert.Null(sut.Error);
        Assert.Equal(string.Empty, sut.Name);
    }

    [Theory]
    [InlineData(FileNameViewModel.FileNameMode.NewFolder, "New folder", "Folder name", "Create", "bi-folder")]
    [InlineData(FileNameViewModel.FileNameMode.RenameFolder, "Rename folder", "Folder name", "Rename", "bi-folder")]
    [InlineData(FileNameViewModel.FileNameMode.NewMarkdown, "New markdown document", "Name", "Create", "bi-file-earmark-richtext")]
    [InlineData(FileNameViewModel.FileNameMode.NewText, "New text file", "Name", "Create", "bi-file-earmark-text")]
    public void GivenWhatIsBeingNamed_WhenLoaded_ThenTheDialogSaysSo(FileNameViewModel.FileNameMode mode, string title, string label, string save, string icon)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Load(mode, "Travel");

        // Assert
        Assert.Equal(mode, sut.Mode);
        Assert.Equal(title, sut.Title);
        Assert.Equal(label, sut.Label);
        Assert.Equal(save, sut.SaveText);
        Assert.Equal(icon, sut.Icon);
        Assert.Null(sut.CreatedFileId);
    }

    [Theory]
    [InlineData(FileNameViewModel.FileNameMode.NewMarkdown, "Trip notes", "Trip notes.md", "text/markdown")]
    [InlineData(FileNameViewModel.FileNameMode.NewMarkdown, " Trip notes.MD ", "Trip notes.MD", "text/markdown")]
    [InlineData(FileNameViewModel.FileNameMode.NewMarkdown, "Trip notes.markdown", "Trip notes.markdown", "text/markdown")]
    [InlineData(FileNameViewModel.FileNameMode.NewMarkdown, "v1.2", "v1.2.md", "text/markdown")]
    [InlineData(FileNameViewModel.FileNameMode.NewText, "Codes", "Codes.txt", "text/plain")]
    [InlineData(FileNameViewModel.FileNameMode.NewText, "Codes.txt", "Codes.txt", "text/plain")]
    [InlineData(FileNameViewModel.FileNameMode.NewText, "Codes.md", "Codes.md.txt", "text/plain")]
    public async Task GivenANewDocumentsName_WhenSaved_ThenAnEmptyFileOfThatKindIsMade_ToOpenInTheEditor(
        FileNameViewModel.FileNameMode mode, string name, string expectedName, string expectedType)
    {
        // Arrange
        var id = Guid.NewGuid().ToString("D");
        mockSession.Setup(x => x.AddFileAsync(expectedName, "Travel", expectedType, It.Is<byte[]>(b => b.Length == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VaultFileEntry { Id = id, Kind = VaultFileKind.File, Name = expectedName, Folder = "Travel" });
        var sut = CreateSut();
        sut.Load(mode, "Travel");
        sut.Name = name;

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.True(saved);
        Assert.Equal(id, sut.CreatedFileId);
        Assert.Null(sut.Error);
        mockSession.Verify(x => x.CreateFolderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(FileNameViewModel.FileNameMode.NewMarkdown)]
    [InlineData(FileNameViewModel.FileNameMode.NewText)]
    public async Task GivenNoName_WhenANewDocumentIsSaved_ThenItSaysSo_AndNothingIsMade(FileNameViewModel.FileNameMode mode)
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(mode, string.Empty);
        sut.Name = "   ";

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.False(saved);
        Assert.Equal("Give the file a name.", sut.Error);
        Assert.Null(sut.CreatedFileId);
        mockSession.Verify(x => x.AddFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenAFolder_WhenMade_ThenNoFileIsReportedAsMade()
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(FileNameViewModel.FileNameMode.NewFolder, string.Empty);
        sut.Name = "Travel";

        // Act
        await sut.SaveAsync();

        // Assert
        Assert.Null(sut.CreatedFileId);
    }

    [Fact]
    public async Task GivenASaveStillRunning_WhenSavedAgain_ThenTheSecondDoesNothing()
    {
        // Arrange: e.g. Create pressed twice.
        var making = new TaskCompletionSource();
        mockSession.Setup(x => x.CreateFolderAsync("Travel", It.IsAny<CancellationToken>())).Returns(making.Task);
        var sut = CreateSut();
        sut.Load(FileNameViewModel.FileNameMode.NewFolder, string.Empty);
        sut.Name = "Travel";
        var first = sut.SaveAsync();

        // Act
        var second = await sut.SaveAsync();
        making.SetResult();

        // Assert
        Assert.False(second);
        Assert.True(await first);
        Assert.False(sut.IsWorking);
        mockSession.Verify(x => x.CreateFolderAsync("Travel", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GivenANullName_WhenSet_ThenItIsEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Name = null!;

        // Assert
        Assert.Equal(string.Empty, sut.Name);
    }

    private FileNameViewModel CreateSut() => new(mockSession.Object, tree, text.Errors, text.For<ClypseText>());
}
