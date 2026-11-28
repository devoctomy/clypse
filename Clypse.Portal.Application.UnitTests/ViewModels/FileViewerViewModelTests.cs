using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Files;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Sync.Connectivity;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;
using Devoctomy.Blazwiz.Testing;
using Devoctomy.Blazwiz.UI.Sync.Text;
using Devoctomy.Blazwiz.UI.Sync.ViewModels;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class FileViewerViewModelTests
{
    private const string VaultId = "vault-1";

    private static readonly DateTimeOffset Added = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultFileSession> mockSession = new();
    private readonly Mock<ISyncFileHistoryService> mockHistory = new();
    private readonly VaultFileEntry image = NewFile("Passport.jpg", "image/jpeg", "Travel");
    private readonly VaultFileEntry document = NewFile("Ticket.pdf", "application/pdf", string.Empty);
    private List<VaultFileEntry> entries;

    public FileViewerViewModelTests()
    {
        entries = [image, document, new VaultFileEntry { Id = Guid.NewGuid().ToString("D"), Kind = VaultFileKind.Folder, Folder = "Archive" }];
        mockSession.SetupGet(x => x.Entries).Returns(() => entries);
        mockSession.SetupGet(x => x.Vault).Returns(new Vault { Id = VaultId });
        mockSession.Setup(x => x.GetContentKey(It.IsAny<string>())).Returns((string id) => ContentKeyOf(id));
        mockHistory.SetupGet(x => x.IsAvailable).Returns(true);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var types = new FileTypeService(text.For<ClypseText>());
        var history = CreateHistory();

        // Act & Assert
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new FileViewerViewModel(null!, types, text.Errors, history)).ParamName);
        Assert.Equal("fileTypes", Assert.Throws<ArgumentNullException>(() => new FileViewerViewModel(mockSession.Object, null!, text.Errors, history)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new FileViewerViewModel(mockSession.Object, types, null!, history)).ParamName);
        Assert.Equal("history", Assert.Throws<ArgumentNullException>(() => new FileViewerViewModel(mockSession.Object, types, text.Errors, null!)).ParamName);
    }

    [Fact]
    public async Task GivenAnImage_WhenOpened_ThenItIsDecryptedAndShownFromADataUri_WithItsDetails()
    {
        // Arrange
        byte[] content = [1, 2, 3, 4, 5];
        var expected = "data:image/jpeg;base64," + Convert.ToBase64String(content);
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync(content);
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(image.Id);

        // Assert
        Assert.True(sut.IsFound);
        Assert.Equal(image.Id, sut.FileId);
        Assert.Equal(FileViewKind.Image, sut.Kind);
        Assert.Equal(expected, sut.ImageSource);
        Assert.False(sut.IsLoading);
        Assert.False(sut.IsNotViewable);
        Assert.Null(sut.Error);
        Assert.Equal("Passport.jpg", sut.Name);
        Assert.Equal("Travel", sut.Folder);
        Assert.Equal("Expires 2031", sut.Notes);
        Assert.Equal("bi-file-earmark-image", sut.Icon);
        Assert.Equal("Image", sut.TypeName);
        Assert.Equal("2 KB", sut.SizeText);
        Assert.False(string.IsNullOrEmpty(sut.AddedText));
        Assert.NotEqual(sut.AddedText, sut.UpdatedText);
    }

    [Fact]
    public async Task GivenAnImage_WhenOpened_ThenTheDecryptedBytesAreWipedOnceTheImageIsBuilt()
    {
        // Arrange
        byte[] content = [1, 2, 3, 4, 5];
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync(content);
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(image.Id);

        // Assert
        Assert.NotNull(sut.ImageSource);
        Assert.All(content, b => Assert.Equal(0, b));
    }

    [Theory]
    [InlineData("image/png", "a.png", "data:image/png;base64,")]
    [InlineData("Image/GIF", "a.gif", "data:image/gif;base64,")]
    [InlineData("image/webp; charset=binary", "a.webp", "data:image/webp;base64,")]
    public async Task GivenAnImageOfAnAllowedType_WhenOpened_ThenTheDataUriCarriesThatType(string contentType, string name, string expectedStart)
    {
        // Arrange
        var file = NewFile(name, contentType, string.Empty);
        entries = [file];
        mockSession.Setup(x => x.ReadContentAsync(file.Id, It.IsAny<CancellationToken>())).ReturnsAsync([9]);
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(file.Id);

        // Assert
        Assert.StartsWith(expectedStart, sut.ImageSource, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Ticket.pdf", "application/pdf")]
    [InlineData("Drawing.svg", "image/svg+xml")]
    [InlineData("Page.html", "text/html")]
    [InlineData("Notes.txt", "text/plain")]
    [InlineData("Notes.md", "text/markdown")]
    [InlineData("Archive.zip", null)]
    public async Task GivenAFileThatIsNotAnImage_WhenOpened_ThenOnlyItsDetailsAreShown_AndItsContentIsNotRead(string name, string? contentType)
    {
        // Arrange
        var file = NewFile(name, contentType, string.Empty);
        entries = [file];
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(file.Id);

        // Assert
        Assert.True(sut.IsFound);
        Assert.True(sut.IsNotViewable);
        Assert.Null(sut.ImageSource);
        Assert.Null(sut.Error);
        Assert.False(sut.IsLoading);
        Assert.Equal(name, sut.Name);
        mockSession.Verify(x => x.ReadContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenAnImageWhoseContentIsNotHereYet_WhenOpened_ThenItSaysSo_AndItsDetailsAreStillShown()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(image.Id);

        // Assert
        Assert.Equal("That file's content is not on this device yet.", sut.Error);
        Assert.Null(sut.ImageSource);
        Assert.False(sut.IsLoading);
        Assert.True(sut.IsFound);
        Assert.False(sut.IsNotViewable);
    }

    [Fact]
    public async Task GivenReadingFails_WhenOpened_ThenTheErrorIsShown()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ThrowsAsync(new ClypseException(ClypseErrorCodes.NoVaultOpen));
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(image.Id);

        // Assert
        Assert.Equal("Open a vault first.", sut.Error);
        Assert.Null(sut.ImageSource);
        Assert.False(sut.IsLoading);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    public async Task GivenNoSuchFile_WhenOpened_ThenItIsNotFound_AndNothingIsRead(string? id)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(id);

        // Assert
        Assert.False(sut.IsFound);
        Assert.Null(sut.FileId);
        Assert.False(sut.IsNotViewable);
        Assert.Equal(string.Empty, sut.Name);
        Assert.Equal(string.Empty, sut.TypeName);
        Assert.Equal(string.Empty, sut.SizeText);
        Assert.Equal(string.Empty, sut.AddedText);
        Assert.Equal("bi-file-earmark", sut.Icon);
        Assert.Null(sut.Notes);
        mockSession.Verify(x => x.ReadContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenAFolderRecordsId_WhenOpened_ThenItIsNotFound()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(entries[2].Id);

        // Assert
        Assert.False(sut.IsFound);
    }

    [Fact]
    public async Task GivenAnOpenImage_WhenFullScreenIsToggled_ThenItFillsThePage_AndTogglingAgainGoesBackToTheDetails()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);
        var shown = sut.ImageSource;
        var before = sut.IsFullScreen;

        // Act
        sut.ToggleFullScreenCommand.Execute(null);
        var expanded = sut.IsFullScreen;
        sut.ToggleFullScreenCommand.Execute(null);

        // Assert
        Assert.False(before);
        Assert.True(expanded);
        Assert.False(sut.IsFullScreen);
        Assert.Equal(shown, sut.ImageSource);
        mockSession.Verify(x => x.ReadContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenNoImageShowing_WhenFullScreenIsToggled_ThenNothingHappens()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ToggleFullScreen();
        var withNothingOpen = sut.IsFullScreen;
        await sut.LoadAsync(document.Id);
        sut.ToggleFullScreen();

        // Assert
        Assert.False(withNothingOpen);
        Assert.False(sut.IsFullScreen);
    }

    [Fact]
    public async Task GivenAnImageFullScreen_WhenClearedOrAnotherFileIsOpened_ThenItIsNoLongerFullScreen()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => [1, 2, 3]);
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);
        sut.ToggleFullScreen();

        // Act
        sut.Clear();
        var afterClear = sut.IsFullScreen;
        await sut.LoadAsync(image.Id);
        sut.ToggleFullScreen();
        await sut.LoadAsync(image.Id);

        // Assert
        Assert.False(afterClear);
        Assert.False(sut.IsFullScreen);
        Assert.NotNull(sut.ImageSource);
    }

    [Fact]
    public async Task GivenAnImageFullScreen_WhenTheVaultLocks_ThenNothingOfItIsKept()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);
        sut.ToggleFullScreen();

        // Act
        entries = [];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.False(sut.IsFullScreen);
        Assert.Null(sut.ImageSource);
    }

    [Fact]
    public async Task GivenAnOpenImage_WhenCleared_ThenNothingOfItIsKept()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        // Act
        sut.Clear();

        // Assert
        Assert.Null(sut.ImageSource);
        Assert.False(sut.IsFound);
        Assert.Null(sut.FileId);
        Assert.Equal(FileViewKind.None, sut.Kind);
        Assert.Contains(nameof(FileViewerViewModel.ImageSource), changed);
        Assert.Contains(nameof(FileViewerViewModel.IsFound), changed);
    }

    [Fact]
    public async Task GivenAnImageBeingDecrypted_WhenTheViewerIsClearedMeanwhile_ThenNothingOfItIsKept_AndItsBytesAreWiped()
    {
        // Arrange
        byte[] content = [1, 2, 3];
        var reading = new TaskCompletionSource<byte[]?>();
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).Returns(reading.Task);
        var sut = CreateSut();
        var opening = sut.LoadAsync(image.Id);

        // Act
        sut.Clear();
        reading.SetResult(content);
        await opening;

        // Assert
        Assert.Null(sut.ImageSource);
        Assert.Null(sut.Error);
        Assert.False(sut.IsFound);
        Assert.False(sut.IsLoading);
        Assert.All(content, b => Assert.Equal(0, b));
    }

    [Fact]
    public async Task GivenAnOpenImage_WhenAnotherFileIsOpened_ThenTheFirstImageIsDropped()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);

        // Act
        await sut.LoadAsync(document.Id);

        // Assert
        Assert.Equal(document.Id, sut.FileId);
        Assert.Null(sut.ImageSource);
        Assert.True(sut.IsNotViewable);
    }

    [Fact]
    public async Task GivenAnOpenImage_WhenTheVaultLocksOrTheFileIsDeleted_ThenNothingOfItIsKept()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);

        // Act: locking forgets every entry; a delete removes this one.
        entries = [];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Null(sut.ImageSource);
        Assert.False(sut.IsFound);
    }

    [Fact]
    public async Task GivenAnOpenImage_WhenItsDetailsChange_ThenTheyFollow_AndTheImageStays()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);
        var shown = sut.ImageSource;
        var renamed = NewFile("Old passport.jpg", "image/jpeg", "Archive");
        renamed.Id = image.Id;

        // Act
        entries = [renamed, document];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal("Old passport.jpg", sut.Name);
        Assert.Equal("Archive", sut.Folder);
        Assert.Equal(shown, sut.ImageSource);
        mockSession.Verify(x => x.ReadContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAnOpenImage_WhenDisposed_ThenNothingOfItIsKept_AndTheSessionIsNoLongerFollowed()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);

        // Act
        sut.Dispose();
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Null(sut.ImageSource);
        Assert.False(sut.IsFound);
    }

    [Fact]
    public void GivenNothingOpen_WhenTheSessionChanges_ThenNothingHappens()
    {
        // Arrange
        var sut = CreateSut();
        var changed = 0;
        sut.PropertyChanged += (_, _) => changed++;

        // Act
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal(0, changed);
    }

    [Fact]
    public async Task GivenAnImageShown_WhenItsHistoryIsExpanded_ThenItsContentsVersionsAreListed()
    {
        // Arrange
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        mockHistory.Setup(x => x.GetVersionsAsync(VaultId, ContentKeyOf(image.Id), It.IsAny<CancellationToken>())).ReturnsAsync([VersionOf(image.Id)]);
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);

        // Act
        await sut.History.ToggleAsync();

        // Assert
        Assert.True(sut.History.IsAvailable);
        Assert.Single(sut.History.Versions);
    }

    [Fact]
    public async Task GivenAnImageShown_WhenAPastVersionIsPicked_ThenThatVersionIsShownInItsPlace()
    {
        // Arrange
        byte[] current = [1, 2, 3];
        byte[] past = [9, 8, 7];
        var version = VersionOf(image.Id);
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync(current);
        mockHistory.Setup(x => x.ReadVersionAsync(version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncFileVersionContent { Version = version, Content = past });
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);
        var currentSource = sut.ImageSource;

        // Act
        await sut.History.SelectAsync(version);

        // Assert
        Assert.True(sut.IsViewingVersion);
        Assert.False(sut.HasUnsavedChanges);
        Assert.Equal("data:image/jpeg;base64," + Convert.ToBase64String(past), sut.ShownImageSource);
        Assert.Equal(currentSource, sut.ImageSource);
        Assert.Equal(new byte[] { 9, 8, 7 }, past);
    }

    [Fact]
    public async Task GivenAPastVersionShown_WhenBackToCurrent_OrTheViewerIsCleared_ThenTheCurrentImageIsShownAgain()
    {
        // Arrange
        var version = VersionOf(image.Id);
        mockSession.Setup(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        mockHistory.Setup(x => x.ReadVersionAsync(version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncFileVersionContent { Version = version, Content = [9, 8, 7] });
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);
        var currentSource = sut.ImageSource;
        await sut.History.SelectAsync(version);

        // Act
        sut.History.BackToCurrent();
        var afterBack = sut.ShownImageSource;
        await sut.History.SelectAsync(version);
        sut.Clear();

        // Assert
        Assert.NotNull(currentSource);
        Assert.Equal(currentSource, afterBack);
        Assert.False(sut.IsViewingVersion);
        Assert.Null(sut.History.SelectedContent);
        Assert.Null(sut.ShownImageSource);
    }

    [Fact]
    public async Task GivenAnImageShown_WhenItsContentIsSavedAgain_ThenTheImageIsReadAgain()
    {
        // Arrange
        byte[] restored = [4, 5, 6];
        mockSession.SetupSequence(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3])
            .ReturnsAsync(restored);
        var sut = CreateSut();
        await sut.LoadAsync(image.Id);
        var after = NewFile(image.Name!, image.ContentType, image.Folder);
        after.Id = image.Id;
        after.LastUpdatedAt = image.LastUpdatedAt.AddHours(1);
        entries = [after, document];

        // Act
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal("data:image/jpeg;base64," + Convert.ToBase64String([4, 5, 6]), sut.ImageSource);
        mockSession.Verify(x => x.ReadContentAsync(image.Id, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GivenAFileThatIsNotAnImage_WhenOpened_ThenItHasNoHistory()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(document.Id);

        // Assert
        Assert.False(sut.History.IsAvailable);
    }

    private static VaultFileEntry NewFile(string name, string? contentType, string folder)
    {
        return new VaultFileEntry
        {
            Id = Guid.NewGuid().ToString("D"),
            Kind = VaultFileKind.File,
            Name = name,
            Folder = folder,
            ContentType = contentType,
            Size = 2048,
            Notes = "Expires 2031",
            CreatedAt = Added,
            LastUpdatedAt = Added.AddDays(1),
        };
    }

    private static string ContentKeyOf(string id) => $"profiles/{VaultId}/files/blobs/{id}.bin";

    private static SyncFileVersion VersionOf(string id) => new()
    {
        Profile = VaultId,
        Key = ContentKeyOf(id),
        VersionId = "v1",
        ETag = "\"abc\"",
        UploadedAt = Added,
    };

    private FileHistoryViewModel CreateHistory()
    {
        var connectivity = new Mock<IConnectivityService>();
        connectivity.SetupGet(x => x.Status).Returns(new ConnectivityStatus { IsOnline = true });
        return new FileHistoryViewModel(mockHistory.Object, connectivity.Object, text.For<SyncUiText>(), text.Errors, new FakeTimeProvider());
    }

    private FileViewerViewModel CreateSut() => new(mockSession.Object, new FileTypeService(text.For<ClypseText>()), text.Errors, CreateHistory());
}
