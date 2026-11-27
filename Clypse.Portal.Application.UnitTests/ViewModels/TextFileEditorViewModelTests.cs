using System.Text;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Files;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class TextFileEditorViewModelTests
{
    private static readonly DateTimeOffset Added = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultFileSession> mockSession = new();
    private readonly Mock<ITextEditorInteropService> mockInterop = new();
    private readonly UnsavedChangesGuard guard = new();
    private readonly VaultFileEntry markdown = NewFile("Notes.md", "text/markdown", "Work");
    private readonly VaultFileEntry plain = NewFile("todo.txt", "text/plain", string.Empty);
    private readonly VaultFileEntry image = NewFile("Passport.jpg", "image/jpeg", string.Empty);
    private readonly Dictionary<string, byte[]> contents = [];
    private readonly List<(string Id, byte[] Content)> saved = [];
    private List<VaultFileEntry> entries;
    private bool unlocked = true;

    public TextFileEditorViewModelTests()
    {
        entries = [markdown, plain, image];
        contents[markdown.Id] = Encoding.UTF8.GetBytes("# Title\n\nSome **bold** text.");
        contents[plain.Id] = Encoding.UTF8.GetBytes("milk\neggs");
        mockSession.SetupGet(x => x.Entries).Returns(() => entries);
        mockSession.SetupGet(x => x.IsUnlocked).Returns(() => unlocked);
        mockSession.Setup(x => x.ReadContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => contents.TryGetValue(id, out var content) ? (byte[])content.Clone() : null);
        mockSession.Setup(x => x.SaveContentAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, byte[] content, CancellationToken _) => Store(id, content));
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();
        var types = new FileTypeService(t);
        var renderer = new MarkdownRenderer();
        var format = new MarkdownFormatService(t);

        // Act & Assert
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new TextFileEditorViewModel(null!, types, renderer, format, mockInterop.Object, guard, text.Errors)).ParamName);
        Assert.Equal("fileTypes", Assert.Throws<ArgumentNullException>(() => new TextFileEditorViewModel(mockSession.Object, null!, renderer, format, mockInterop.Object, guard, text.Errors)).ParamName);
        Assert.Equal("renderer", Assert.Throws<ArgumentNullException>(() => new TextFileEditorViewModel(mockSession.Object, types, null!, format, mockInterop.Object, guard, text.Errors)).ParamName);
        Assert.Equal("formatService", Assert.Throws<ArgumentNullException>(() => new TextFileEditorViewModel(mockSession.Object, types, renderer, null!, mockInterop.Object, guard, text.Errors)).ParamName);
        Assert.Equal("interop", Assert.Throws<ArgumentNullException>(() => new TextFileEditorViewModel(mockSession.Object, types, renderer, format, null!, guard, text.Errors)).ParamName);
        Assert.Equal("guard", Assert.Throws<ArgumentNullException>(() => new TextFileEditorViewModel(mockSession.Object, types, renderer, format, mockInterop.Object, null!, text.Errors)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new TextFileEditorViewModel(mockSession.Object, types, renderer, format, mockInterop.Object, guard, null!)).ParamName);
    }

    [Fact]
    public async Task GivenAMarkdownDocument_WhenOpened_ThenItsTextIsDecrypted_WithItsPreview()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(markdown.Id);

        // Assert
        Assert.True(sut.IsFound);
        Assert.True(sut.IsReady);
        Assert.True(sut.IsMarkdown);
        Assert.Equal("Notes.md", sut.Name);
        Assert.Equal("Work", sut.Folder);
        Assert.Equal("# Title\n\nSome **bold** text.", sut.Content);
        Assert.Contains(">Title</h1>", sut.PreviewHtml, StringComparison.Ordinal);
        Assert.Contains("<strong>bold</strong>", sut.PreviewHtml, StringComparison.Ordinal);
        Assert.False(sut.IsDirty);
        Assert.False(sut.IsLoading);
        Assert.Null(sut.Error);
        Assert.Null(sut.LastSavedAt);
    }

    [Fact]
    public async Task GivenAPlainTextFile_WhenOpened_ThenItHasItsText_AndNoPreview()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(plain.Id);

        // Assert
        Assert.True(sut.IsReady);
        Assert.False(sut.IsMarkdown);
        Assert.Equal("milk\neggs", sut.Content);
        Assert.Equal(string.Empty, sut.PreviewHtml);
    }

    [Fact]
    public async Task GivenATextFile_WhenOpened_ThenTheDecryptedBytesAreWipedOnceTheTextIsRead()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("secret words");
        mockSession.Setup(x => x.ReadContentAsync(plain.Id, It.IsAny<CancellationToken>())).ReturnsAsync(content);
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(plain.Id);

        // Assert
        Assert.Equal("secret words", sut.Content);
        Assert.All(content, b => Assert.Equal(0, b));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenAFileThatIsNotText_OrNoFile_WhenOpened_ThenItIsNotFound_AndNothingIsRead(bool isImage)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(isImage ? image.Id : Guid.NewGuid().ToString("D"));

        // Assert
        Assert.False(sut.IsFound);
        Assert.False(sut.IsReady);
        Assert.Equal(string.Empty, sut.Content);
        mockSession.Verify(x => x.ReadContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenATextFileWhoseContentIsNotHereYet_WhenOpened_ThenItSaysSo_AndCannotBeEdited()
    {
        // Arrange
        contents.Remove(plain.Id);
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(plain.Id);
        await sut.SaveAsync();

        // Assert
        Assert.Equal("That file's content is not on this device yet.", sut.Error);
        Assert.True(sut.IsFound);
        Assert.False(sut.IsReady);
        Assert.False(sut.IsLoading);
        Assert.Empty(saved);
    }

    [Fact]
    public async Task GivenContentThatIsNotText_WhenOpened_ThenItIsRefused_RatherThanDamagedWhenSaved()
    {
        // Arrange
        contents[plain.Id] = [0xFF, 0xFE, 0x00, 0xC3, 0x28];
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(plain.Id);

        // Assert
        Assert.Equal("That file's content is not text, so it cannot be edited here.", sut.Error);
        Assert.False(sut.IsReady);
        Assert.Equal(string.Empty, sut.Content);
    }

    [Fact]
    public async Task GivenTextLongerThanTheEditorTakes_WhenOpened_ThenItIsRefused()
    {
        // Arrange
        contents[plain.Id] = new byte[TextFileEditorViewModel.MaximumTextSize + 1];
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(plain.Id);

        // Assert
        Assert.Equal("Text can be edited here up to 1 MB.", sut.Error);
        Assert.False(sut.IsReady);
    }

    [Fact]
    public async Task GivenAnOpenDocument_WhenTyped_ThenItHasUnsavedChanges_ThePreviewFollows_AndTheVaultIsKeptUnlocked()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(markdown.Id);
        mockSession.Invocations.Clear();

        // Act
        sut.Content = "# Changed";
        var dirty = sut.IsDirty;
        var preview = sut.PreviewHtml;
        sut.Content = "# Title\n\nSome **bold** text.";

        // Assert
        Assert.True(dirty);
        Assert.Contains(">Changed</h1>", preview, StringComparison.Ordinal);
        Assert.False(sut.IsDirty);
        mockSession.Verify(x => x.KeepUnlocked(), Times.Exactly(2));
    }

    [Fact]
    public async Task GivenChangedText_WhenSaved_ThenItIsStoredAsUtf8_AndThereAreNoUnsavedChanges()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "milk\neggs\ncafé";

        // Act
        await sut.SaveCommand.ExecuteAsync(null);

        // Assert
        var (id, content) = Assert.Single(saved);
        Assert.Equal(plain.Id, id);
        Assert.Equal("milk\neggs\ncafé", Encoding.UTF8.GetString(content));
        Assert.False(sut.IsDirty);
        Assert.False(sut.IsSaving);
        Assert.Null(sut.Error);
        Assert.Equal(Added.AddMinutes(1), sut.LastSavedAt);
    }

    [Fact]
    public async Task GivenAFileWithAByteOrderMarkAndWindowsLineEndings_WhenEditedAndSaved_ThenBothAreKept()
    {
        // Arrange
        contents[plain.Id] = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("one\r\ntwo")];
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        var shown = sut.Content;
        var dirtyWhenOpened = sut.IsDirty;

        // Act
        sut.Content = "one\ntwo\nthree";
        await sut.SaveAsync();

        // Assert
        Assert.Equal("one\ntwo", shown);
        Assert.False(dirtyWhenOpened);
        byte[] expected = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("one\r\ntwo\r\nthree")];
        Assert.Equal(expected, Assert.Single(saved).Content);
    }

    [Fact]
    public async Task GivenSavingFails_WhenSaved_ThenTheErrorIsShown_AndTheChangesAreStillUnsaved()
    {
        // Arrange
        mockSession.Setup(x => x.SaveContentAsync(plain.Id, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClypseException(ClypseErrorCodes.FileNotFound));
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "changed";

        // Act
        await sut.SaveAsync();
        var shown = sut.Error;
        sut.DismissErrorCommand.Execute(null);

        // Assert
        Assert.Equal("That file no longer exists.", shown);
        Assert.Null(sut.Error);
        Assert.True(sut.IsDirty);
        Assert.False(sut.IsSaving);
        Assert.Equal("changed", sut.Content);
    }

    [Fact]
    public async Task GivenTextGrownLongerThanTheEditorTakes_WhenSaved_ThenItIsRefused_AndNothingIsStored()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = new string('x', (int)TextFileEditorViewModel.MaximumTextSize + 1);

        // Act
        await sut.SaveAsync();

        // Assert
        Assert.Equal("Text can be edited here up to 1 MB.", sut.Error);
        Assert.Empty(saved);
        Assert.True(sut.IsDirty);
    }

    [Fact]
    public async Task GivenAMarkdownDocument_WhenAFormatIsApplied_ThenTheSelectionIsEdited_AndTheTextFollows()
    {
        // Arrange
        mockInterop.Setup(x => x.GetSelectionAsync(TextFileEditorViewModel.EditorElementId))
            .ReturnsAsync(new EditorSelection { Text = "say hello", Start = 4, End = 9 });
        var sut = CreateSut();
        await sut.LoadAsync(markdown.Id);

        // Act
        await sut.FormatCommand.ExecuteAsync(MarkdownFormat.Bold);

        // Assert
        Assert.Equal("say **hello**", sut.Content);
        Assert.True(sut.IsDirty);
        mockInterop.Verify(
            x => x.ApplyEditAsync(
                TextFileEditorViewModel.EditorElementId,
                It.Is<MarkdownTextEdit>(e => e.Start == 4 && e.End == 9 && e.Replacement == "**hello**" && e.SelectionStart == 6 && e.SelectionEnd == 11)),
            Times.Once);
    }

    [Fact]
    public async Task GivenAPlainTextFile_OrNoEditorOnThePage_WhenAFormatIsApplied_ThenNothingHappens()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);

        // Act
        await sut.FormatAsync(MarkdownFormat.Bold);
        await sut.LoadAsync(markdown.Id);
        await sut.FormatAsync(MarkdownFormat.Bold);

        // Assert
        Assert.False(sut.IsDirty);
        mockInterop.Verify(x => x.GetSelectionAsync(It.IsAny<string>()), Times.Once);
        mockInterop.Verify(x => x.ApplyEditAsync(It.IsAny<string>(), It.IsAny<MarkdownTextEdit>()), Times.Never);
    }

    [Fact]
    public async Task GivenThePreview_WhenHidden_ThenItIsNotWorkedOut_UntilItIsShownAgain()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(markdown.Id);

        // Act
        sut.TogglePreviewCommand.Execute(null);
        sut.Content = "# Hidden";
        var hidden = (sut.IsPreviewCollapsed, sut.PreviewHtml);
        sut.TogglePreviewCommand.Execute(null);

        // Assert
        Assert.Equal((true, string.Empty), hidden);
        Assert.False(sut.IsPreviewCollapsed);
        Assert.Contains(">Hidden</h1>", sut.PreviewHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GivenNoUnsavedChanges_WhenClosed_ThenTheEditorCloses_AndTheTextIsDropped()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        var closed = 0;
        sut.CloseRequested += (_, _) => closed++;

        // Act
        sut.CloseCommand.Execute(null);

        // Assert
        Assert.Equal(1, closed);
        Assert.False(sut.IsLeaveDialogOpen);
        Assert.False(sut.IsFound);
        Assert.Equal(string.Empty, sut.Content);
    }

    [Fact]
    public async Task GivenUnsavedChanges_WhenClosed_ThenTheUserIsAsked_AndKeepingOnEditingChangesNothing()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "changed";
        var closed = 0;
        sut.CloseRequested += (_, _) => closed++;

        // Act
        sut.Close();
        var asked = sut.IsLeaveDialogOpen;
        sut.KeepEditingCommand.Execute(null);

        // Assert
        Assert.True(asked);
        Assert.False(sut.IsLeaveDialogOpen);
        Assert.Equal(0, closed);
        Assert.Equal("changed", sut.Content);
        Assert.True(sut.IsDirty);
    }

    [Fact]
    public async Task GivenUnsavedChanges_WhenDiscarded_ThenTheEditorCloses_AndNothingIsSaved()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "changed";
        var closed = 0;
        sut.CloseRequested += (_, _) => closed++;
        sut.Close();

        // Act
        sut.DiscardAndLeaveCommand.Execute(null);

        // Assert
        Assert.Equal(1, closed);
        Assert.Empty(saved);
        Assert.False(sut.IsLeaveDialogOpen);
        Assert.Equal(string.Empty, sut.Content);
        Assert.False(sut.IsDirty);
    }

    [Fact]
    public async Task GivenUnsavedChanges_WhenSavedAndLeft_ThenTheyAreSaved_AndTheEditorCloses()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "changed";
        var closed = 0;
        sut.CloseRequested += (_, _) => closed++;
        sut.Close();

        // Act
        await sut.SaveAndLeaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("changed", Encoding.UTF8.GetString(Assert.Single(saved).Content));
        Assert.Equal(1, closed);
        Assert.False(sut.IsLeaveDialogOpen);
    }

    [Fact]
    public async Task GivenSavingFails_WhenSavedAndLeft_ThenTheEditorStaysOpen_WithTheChanges()
    {
        // Arrange
        mockSession.Setup(x => x.SaveContentAsync(plain.Id, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClypseException(ClypseErrorCodes.NoVaultOpen));
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "changed";
        var closed = 0;
        sut.CloseRequested += (_, _) => closed++;
        sut.Close();

        // Act
        await sut.SaveAndLeaveAsync();

        // Assert
        Assert.Equal(0, closed);
        Assert.False(sut.IsLeaveDialogOpen);
        Assert.Equal("Open a vault first.", sut.Error);
        Assert.Equal("changed", sut.Content);
    }

    [Fact]
    public async Task GivenUnsavedChanges_WhenSomethingElseWouldLoseThem_ThenItIsHeld_UntilTheyAreDiscarded()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        var ran = 0;
        guard.Run(() => ran++);
        var withNoChanges = ran;
        sut.Content = "changed";

        // Act
        guard.Run(() => ran++);
        var whileAsked = (ran, sut.IsLeaveDialogOpen);
        sut.DiscardAndLeave();

        // Assert
        Assert.Equal(1, withNoChanges);
        Assert.Equal((1, true), whileAsked);
        Assert.Equal(2, ran);
        Assert.Throws<ArgumentNullException>(() => sut.Hold(null!));
    }

    [Fact]
    public async Task GivenUnsavedChanges_WhenTheVaultLocks_ThenEverythingDecryptedIsForgotten_AndNothingIsAsked()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "unsaved words";
        var closed = 0;
        sut.CloseRequested += (_, _) => closed++;

        // Act
        unlocked = false;
        entries = [];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal(string.Empty, sut.Content);
        Assert.False(sut.IsFound);
        Assert.False(sut.IsDirty);
        Assert.False(sut.IsLeaveDialogOpen);
        Assert.False(sut.IsRemoteChangeDialogOpen);
        Assert.Equal(0, closed);
        Assert.Empty(saved);
    }

    [Fact]
    public async Task GivenNoUnsavedChanges_WhenASyncChangesTheOpenFile_ThenItIsReadAgain()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);

        // Act
        ChangeElsewhere(plain, "milk\neggs\nbread");
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal("milk\neggs\nbread", sut.Content);
        Assert.False(sut.IsDirty);
        Assert.False(sut.IsRemoteChangeDialogOpen);
    }

    [Fact]
    public async Task GivenUnsavedChanges_WhenASyncChangesTheOpenFile_ThenTheUserChoosesWhichToKeep()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "mine";

        // Act
        ChangeElsewhere(plain, "theirs");
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);
        var asked = (sut.IsRemoteChangeDialogOpen, sut.IsRemoteDeleted, sut.Content);
        sut.KeepMineCommand.Execute(null);
        var kept = (sut.IsRemoteChangeDialogOpen, sut.Content, sut.IsDirty);

        ChangeElsewhere(plain, "theirs again");
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);
        sut.TakeTheirsCommand.Execute(null);

        // Assert
        Assert.Equal((true, false, "mine"), asked);
        Assert.Equal((false, "mine", true), kept);
        Assert.Equal("theirs again", sut.Content);
        Assert.False(sut.IsDirty);
        Assert.False(sut.IsRemoteChangeDialogOpen);
    }

    [Fact]
    public async Task GivenAnOpenFile_WhenOnlyItsDetailsChange_ThenTheyFollow_AndTheTextIsLeftAlone()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "mine";
        var renamed = NewFile("shopping.txt", "text/plain", "Lists");
        renamed.Id = plain.Id;
        renamed.LastUpdatedAt = Added.AddHours(1);

        // Act
        entries = [markdown, renamed, image];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal("shopping.txt", sut.Name);
        Assert.Equal("Lists", sut.Folder);
        Assert.Equal("mine", sut.Content);
        Assert.False(sut.IsRemoteChangeDialogOpen);
    }

    [Fact]
    public async Task GivenAnOpenFile_WhenSomethingElseInTheVaultChanges_ThenNothingIsReadAgain()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        mockSession.Invocations.Clear();

        // Act
        entries = [markdown, plain];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal("milk\neggs", sut.Content);
        mockSession.Verify(x => x.ReadContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenChangedText_WhenSaved_ThenTheSaveItselfIsNotTakenForAChangeElsewhere()
    {
        // Arrange
        mockSession.Setup(x => x.SaveContentAsync(plain.Id, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, byte[] content, CancellationToken _) =>
            {
                var entry = Store(id, content);
                mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);
                return entry;
            });
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "changed";

        // Act
        await sut.SaveAsync();
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.False(sut.IsRemoteChangeDialogOpen);
        Assert.False(sut.IsDirty);
        Assert.Equal("changed", sut.Content);
    }

    [Fact]
    public async Task GivenNoUnsavedChanges_WhenASyncDeletesTheOpenFile_ThenTheEditorCloses()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        var closed = 0;
        sut.CloseRequested += (_, _) => closed++;

        // Act
        entries = [markdown, image];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal(1, closed);
        Assert.False(sut.IsFound);
        Assert.False(sut.IsRemoteChangeDialogOpen);
    }

    [Fact]
    public async Task GivenUnsavedChanges_WhenASyncDeletesTheOpenFile_AndItIsLetGo_ThenTheEditorCloses()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "mine";
        var closed = 0;
        sut.CloseRequested += (_, _) => closed++;

        // Act
        entries = [markdown, image];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);
        var asked = (sut.IsRemoteChangeDialogOpen, sut.IsRemoteDeleted, closed);
        sut.TakeTheirs();

        // Assert
        Assert.Equal((true, true, 0), asked);
        Assert.Equal(1, closed);
        Assert.Empty(saved);
        Assert.Equal(string.Empty, sut.Content);
    }

    [Fact]
    public async Task GivenUnsavedChanges_WhenASyncDeletesTheOpenFile_AndMineIsKept_ThenSavingAddsItAgain()
    {
        // Arrange
        var added = NewFile("todo.txt", "text/plain", string.Empty);
        byte[]? addedContent = null;
        mockSession.Setup(x => x.AddFileAsync("todo.txt", string.Empty, "text/plain", It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string? _, string? _, byte[] content, CancellationToken _) =>
            {
                addedContent = (byte[])content.Clone();
                entries = [markdown, image, added];
                return added;
            });
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "mine";
        entries = [markdown, image];
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Act
        sut.KeepMine();
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);
        var afterKeeping = (sut.IsRemoteChangeDialogOpen, sut.IsDirty, sut.Content);
        await sut.SaveAsync();

        // Assert
        Assert.Equal((false, true, "mine"), afterKeeping);
        Assert.Equal("mine", Encoding.UTF8.GetString(addedContent!));
        Assert.Empty(saved);
        Assert.False(sut.IsDirty);
        Assert.Null(sut.Error);
    }

    [Fact]
    public async Task GivenAnOpenFile_WhenDisposed_ThenTheTextIsDropped_AndNothingIsHeldBackAnyMore()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        sut.Content = "changed";
        var ran = 0;

        // Act
        sut.Dispose();
        guard.Run(() => ran++);
        mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty);

        // Assert
        Assert.Equal(1, ran);
        Assert.Equal(string.Empty, sut.Content);
        Assert.False(sut.IsFound);
    }

    [Fact]
    public async Task GivenAFileBeingDecrypted_WhenTheEditorIsClearedMeanwhile_ThenNothingOfItIsKept()
    {
        // Arrange
        var reading = new TaskCompletionSource<byte[]?>();
        mockSession.Setup(x => x.ReadContentAsync(plain.Id, It.IsAny<CancellationToken>())).Returns(reading.Task);
        var sut = CreateSut();
        var opening = sut.LoadAsync(plain.Id);

        // Act
        sut.Clear();
        reading.SetResult(Encoding.UTF8.GetBytes("late"));
        await opening;

        // Assert
        Assert.Equal(string.Empty, sut.Content);
        Assert.False(sut.IsReady);
        Assert.False(sut.IsLoading);
        Assert.Null(sut.Error);
    }

    [Fact]
    public void GivenTheToolbar_WhenItsButtonsAreListed_ThenEveryFormatHasOneButton()
    {
        // Act
        var formats = TextFileEditorViewModel.ToolbarGroups.SelectMany(g => g).Select(b => b.Format).ToList();

        // Assert
        Assert.Equal(Enum.GetValues<MarkdownFormat>().Order(), formats.Order());
        Assert.All(TextFileEditorViewModel.ToolbarGroups.SelectMany(g => g), b => Assert.StartsWith("bi-", b.Icon, StringComparison.Ordinal));
    }

    private static VaultFileEntry NewFile(string name, string contentType, string folder)
    {
        return new VaultFileEntry
        {
            Id = Guid.NewGuid().ToString("D"),
            Kind = VaultFileKind.File,
            Name = name,
            Folder = folder,
            ContentType = contentType,
            Size = 10,
            CreatedAt = Added,
            LastUpdatedAt = Added,
        };
    }

    private static VaultFileEntry Updated(VaultFileEntry entry, long size, DateTimeOffset at)
    {
        return new VaultFileEntry
        {
            Id = entry.Id,
            Kind = entry.Kind,
            Name = entry.Name,
            Folder = entry.Folder,
            ContentType = entry.ContentType,
            Size = size,
            CreatedAt = entry.CreatedAt,
            LastUpdatedAt = at,
        };
    }

    // What the session does when this editor saves: the content is stored, and the entry replaced by one saved later.
    private VaultFileEntry Store(string id, byte[] content)
    {
        saved.Add((id, (byte[])content.Clone()));
        contents[id] = (byte[])content.Clone();
        var current = entries.First(e => e.Id == id);
        var entry = Updated(current, content.Length, current.LastUpdatedAt.AddMinutes(1));
        entries = [.. entries.Where(e => e.Id != id), entry];
        return entry;
    }

    // What a sync does when another device saved the file: new content, and an entry updated later.
    private void ChangeElsewhere(VaultFileEntry file, string newText)
    {
        contents[file.Id] = Encoding.UTF8.GetBytes(newText);
        var current = entries.First(e => e.Id == file.Id);
        entries = [.. entries.Where(e => e.Id != file.Id), Updated(current, newText.Length, current.LastUpdatedAt.AddHours(1))];
    }

    private TextFileEditorViewModel CreateSut()
    {
        var t = text.For<ClypseText>();
        return new TextFileEditorViewModel(
            mockSession.Object,
            new FileTypeService(t),
            new MarkdownRenderer(),
            new MarkdownFormatService(t),
            mockInterop.Object,
            guard,
            text.Errors);
    }
}
