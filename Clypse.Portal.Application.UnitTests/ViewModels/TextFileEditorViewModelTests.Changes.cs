using System.Text;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

// What happens to the open file from outside the editor: the vault locking, a sync changing or deleting it, and the
// editor being cleared or disposed.
public partial class TextFileEditorViewModelTests
{
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
        mockSession.SetupGet(x => x.IsUnlocked).Returns(false);
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
    public async Task GivenASyncChangesTheOpenFile_WhenItCannotBeReadAgain_ThenNothingIsThrown_AndTheEditorIsLeftAsItIs()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(plain.Id);
        ChangeElsewhere(plain, "theirs");
        mockSession.Setup(x => x.ReadContentAsync(plain.Id, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var ex = Record.Exception(() => mockSession.Raise(x => x.Changed += null, mockSession.Object, EventArgs.Empty));

        // Assert
        Assert.Null(ex);
        Assert.True(sut.IsReady);
        Assert.Equal("milk\neggs", sut.Content);
        Assert.False(sut.IsRemoteChangeDialogOpen);
        Assert.Null(sut.Error);
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
}
