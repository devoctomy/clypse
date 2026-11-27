using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Files;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class FileDetailsViewModelTests
{
    private static readonly DateTimeOffset Added = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultFileSession> mockSession = new();
    private readonly VaultFileTreeService tree = new();
    private readonly VaultFileEntry file = new()
    {
        Id = Guid.NewGuid().ToString("D"),
        Kind = VaultFileKind.File,
        Name = "Passport.jpg",
        Folder = "Travel/Passports",
        ContentType = "image/jpeg",
        Size = 2048,
        Notes = "Expires 2031",
        CreatedAt = Added,
        LastUpdatedAt = Added.AddDays(1),
    };

    private List<VaultFileEntry> entries;

    public FileDetailsViewModelTests()
    {
        entries =
        [
            file,
            new VaultFileEntry { Id = Guid.NewGuid().ToString("D"), Kind = VaultFileKind.Folder, Folder = "Archive" },
        ];
        mockSession.SetupGet(x => x.Entries).Returns(() => entries);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var types = new FileTypeService(text.For<ClypseText>());

        // Act & Assert
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new FileDetailsViewModel(null!, tree, types, text.Errors)).ParamName);
        Assert.Equal("tree", Assert.Throws<ArgumentNullException>(() => new FileDetailsViewModel(mockSession.Object, null!, types, text.Errors)).ParamName);
        Assert.Equal("fileTypes", Assert.Throws<ArgumentNullException>(() => new FileDetailsViewModel(mockSession.Object, tree, null!, text.Errors)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new FileDetailsViewModel(mockSession.Object, tree, types, null!)).ParamName);
    }

    [Fact]
    public void GivenAFile_WhenLoaded_ThenItsDetailsAreShown_WithTheFoldersItCanGoIn()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Load(file.Id);

        // Assert
        Assert.True(sut.IsFound);
        Assert.Equal("Passport.jpg", sut.Name);
        Assert.Equal("Travel/Passports", sut.Folder);
        Assert.Equal("Expires 2031", sut.Notes);
        Assert.Equal(["Archive", "Travel", "Travel/Passports"], sut.Folders);
        Assert.Equal("bi-file-earmark-image", sut.Icon);
        Assert.Equal("Image", sut.TypeName);
        Assert.Equal("2 KB", sut.SizeText);
        Assert.False(string.IsNullOrEmpty(sut.AddedText));
        Assert.False(string.IsNullOrEmpty(sut.UpdatedText));
        Assert.NotEqual(sut.AddedText, sut.UpdatedText);
        Assert.Null(sut.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    public async Task GivenNoSuchFile_WhenLoaded_ThenItIsNotFound_AndNothingCanBeSaved(string? id)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Load(id);
        var saved = await sut.SaveAsync();

        // Assert
        Assert.False(sut.IsFound);
        Assert.False(saved);
        Assert.Equal(string.Empty, sut.Name);
        Assert.Equal(string.Empty, sut.TypeName);
        Assert.Equal(string.Empty, sut.SizeText);
        Assert.Equal(string.Empty, sut.AddedText);
        Assert.Equal("bi-file-earmark", sut.Icon);
        mockSession.Verify(x => x.UpdateFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GivenAFolderRecordsId_WhenLoaded_ThenItIsNotFound()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Load(entries[1].Id);

        // Assert
        Assert.False(sut.IsFound);
    }

    [Fact]
    public async Task GivenChangedDetails_WhenSaved_ThenTheFilesNameFolderAndNotesAreSaved()
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(file.Id);
        sut.Name = "Old passport.jpg";
        sut.Folder = "/Archive/";
        sut.Notes = "Expired";

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.True(saved);
        Assert.Null(sut.Error);
        Assert.False(sut.IsWorking);
        Assert.Equal("Archive", sut.Folder);
        mockSession.Verify(x => x.UpdateFileAsync(file.Id, "Old passport.jpg", "Archive", "Expired", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenSavingFails_WhenSaved_ThenItSaysWhy()
    {
        // Arrange
        mockSession.Setup(x => x.UpdateFileAsync(file.Id, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClypseException(ClypseErrorCodes.FileNameRequired));
        var sut = CreateSut();
        sut.Load(file.Id);
        sut.Name = string.Empty;

        // Act
        var saved = await sut.SaveAsync();

        // Assert
        Assert.False(saved);
        Assert.Equal("Give the file a name.", sut.Error);
        Assert.False(sut.IsWorking);
    }

    [Fact]
    public void GivenNullValues_WhenSet_ThenTheyAreEmpty()
    {
        // Arrange
        var sut = CreateSut();
        sut.Load(file.Id);

        // Act
        sut.Name = null!;
        sut.Folder = null!;
        sut.Notes = null!;

        // Assert
        Assert.Equal(string.Empty, sut.Name);
        Assert.Equal(string.Empty, sut.Folder);
        Assert.Equal(string.Empty, sut.Notes);
    }

    private FileDetailsViewModel CreateSut() => new(mockSession.Object, tree, new FileTypeService(text.For<ClypseText>()), text.Errors);
}
