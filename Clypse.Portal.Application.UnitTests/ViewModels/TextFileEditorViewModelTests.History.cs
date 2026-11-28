using System.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Files;
using Devoctomy.Blazwiz.Sync.Models;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public partial class TextFileEditorViewModelTests
{
    private const string VaultId = "vault-1";

    [Fact]
    public async Task GivenAnOpenDocument_WhenItsHistoryIsExpanded_ThenItsContentsVersionsAreListed()
    {
        // Arrange
        mockHistory.Setup(x => x.GetVersionsAsync(VaultId, ContentKeyOf(markdown.Id), It.IsAny<CancellationToken>())).ReturnsAsync([VersionOf(markdown)]);
        var sut = CreateSut();
        await sut.LoadAsync(markdown.Id);

        // Act
        await sut.History.ToggleAsync();

        // Assert
        Assert.True(sut.History.IsAvailable);
        Assert.Single(sut.History.Versions);
    }

    [Fact]
    public async Task GivenAPastVersionPicked_WhenItIsShown_ThenTheEditorShowsItReadOnly_WithItsPreview()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(markdown.Id);
        var version = SetupVersion(markdown, "# Old title");

        // Act
        await sut.History.SelectAsync(version);

        // Assert
        Assert.True(sut.IsViewingVersion);
        Assert.Equal("# Old title", sut.EditorText);
        Assert.Contains(">Old title</h1>", sut.ShownPreviewHtml, StringComparison.Ordinal);
        Assert.Equal("# Title\n\nSome **bold** text.", sut.Content);
        Assert.Contains(">Title</h1>", sut.PreviewHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GivenAPastVersionShown_WhenTypedIntoFormattedOrSaved_ThenNothingChanges()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(markdown.Id);
        await sut.History.SelectAsync(SetupVersion(markdown, "# Old title"));

        // Act
        sut.EditorText = "typed";
        await sut.FormatAsync(MarkdownFormat.Bold);
        await sut.SaveAsync();

        // Assert
        Assert.Equal("# Old title", sut.EditorText);
        Assert.False(sut.IsDirty);
        Assert.Empty(saved);
        mockInterop.Verify(x => x.GetSelectionAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GivenAPastVersionShown_WhenBackToCurrent_ThenTheTextCanBeEditedAgain()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(markdown.Id);
        await sut.History.SelectAsync(SetupVersion(markdown, "# Old title"));

        // Act
        sut.History.BackToCurrent();
        sut.EditorText = "# Edited";

        // Assert
        Assert.False(sut.IsViewingVersion);
        Assert.Equal("# Edited", sut.Content);
        Assert.Contains(">Edited</h1>", sut.ShownPreviewHtml, StringComparison.Ordinal);
        Assert.True(sut.IsDirty);
    }

    [Fact]
    public async Task GivenUnsavedEdits_WhenAPastVersionIsPickedAndSaveChosen_ThenTheEditsAreSavedFirst()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "bread";
        var version = SetupVersion(plain, "milk");

        // Act
        await sut.History.SelectAsync(version);
        var asked = sut.History.IsUnsavedDialogOpen;
        await sut.History.SaveAndViewAsync();

        // Assert
        Assert.True(asked);
        Assert.False(sut.HasUnsavedChanges);
        Assert.Equal("bread", Encoding.UTF8.GetString(Assert.Single(saved).Content));
        Assert.Equal("milk", sut.EditorText);
    }

    [Fact]
    public async Task GivenUnsavedEdits_WhenAPastVersionIsPickedAndDontSaveChosen_ThenTheEditsAreDropped()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "bread";
        var version = SetupVersion(plain, "milk");

        // Act
        await sut.History.SelectAsync(version);
        await sut.History.DiscardAndViewAsync();

        // Assert
        Assert.Empty(saved);
        Assert.False(sut.IsDirty);
        Assert.Equal("milk\neggs", sut.Content);
        Assert.Equal("milk", sut.EditorText);
    }

    [Fact]
    public async Task GivenAPastVersionShown_WhenTheEditorIsCleared_ThenTheVersionIsDropped()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        await sut.History.SelectAsync(SetupVersion(plain, "milk"));

        // Act
        sut.Clear();

        // Assert
        Assert.False(sut.IsViewingVersion);
        Assert.Null(sut.History.SelectedContent);
        Assert.Equal(string.Empty, sut.EditorText);
    }

    [Fact]
    public async Task GivenAPastVersionLongerThanTheEditorTakes_WhenPicked_ThenItSaysSo_AndTheCurrentTextStaysShown()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        var version = VersionOf(plain);
        mockHistory.Setup(x => x.ReadVersionAsync(version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncFileVersionContent { Version = version, Content = new byte[TextFileEditorViewModel.MaximumTextSize + 1] });

        // Act
        await sut.History.SelectAsync(version);

        // Assert
        Assert.Equal("Text can be edited here up to 1 MB.", sut.Error);
        Assert.False(sut.IsViewingVersion);
        Assert.Equal("milk\neggs", sut.EditorText);
    }

    private static string ContentKeyOf(string id) => $"profiles/{VaultId}/files/blobs/{id}.bin";

    private static SyncFileVersion VersionOf(VaultFileEntry file) => new()
    {
        Profile = VaultId,
        Key = ContentKeyOf(file.Id),
        VersionId = "v1",
        ETag = "\"abc\"",
        UploadedAt = Added,
    };

    private SyncFileVersion SetupVersion(VaultFileEntry file, string text)
    {
        var version = VersionOf(file);
        mockHistory.Setup(x => x.ReadVersionAsync(version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncFileVersionContent { Version = version, Content = Encoding.UTF8.GetBytes(text) });
        return version;
    }
}
