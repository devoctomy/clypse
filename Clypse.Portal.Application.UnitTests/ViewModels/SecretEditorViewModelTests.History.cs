using Clypse.Core.Secrets;
using Clypse.Portal.Application.Exceptions;
using Devoctomy.Blazwiz.Sync.Models;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public partial class SecretEditorViewModelTests
{
    [Fact]
    public async Task GivenASavedSecret_WhenItsHistoryIsExpanded_ThenItsFilesVersionsAreListed()
    {
        // Arrange
        var secret = Login("Mail", "bob", "first");
        mockHistory.Setup(x => x.GetVersionsAsync(vault.Id, SecretKeyOf(secret.Id), It.IsAny<CancellationToken>())).ReturnsAsync([VersionOf(secret)]);
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);

        // Act
        await sut.History.ToggleAsync();

        // Assert
        Assert.True(sut.History.IsAvailable);
        Assert.Single(sut.History.Versions);
    }

    [Fact]
    public async Task GivenANewSecret_WhenLoaded_ThenItHasNoHistory()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(null);

        // Assert
        Assert.False(sut.History.IsAvailable);
    }

    [Fact]
    public async Task GivenAPastVersionPicked_WhenItIsShown_ThenItsValuesAreShownReadOnly()
    {
        // Arrange
        var secret = Login("Mail", "bob", "second");
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);
        var version = SetupVersion(secret, new WebSecret { Name = "Old mail", UserName = "robert", Password = "first" }, secret.Id);

        // Act
        await sut.History.SelectAsync(version);
        await sut.SetValueAsync("Name", "typed");
        sut.OpenGenerator("Password");
        var savedWhileViewing = await sut.SaveAsync();

        // Assert
        Assert.True(sut.IsViewingVersion);
        Assert.Equal("Old mail", sut.GetValue("Name"));
        Assert.Equal("robert", sut.GetValue("UserName"));
        Assert.Equal("first", sut.GetValue("Password"));
        Assert.Null(sut.GeneratingFor);
        Assert.False(savedWhileViewing);
        Assert.Null(saved);
        Assert.False(sut.HasUnsavedChanges);
    }

    [Fact]
    public async Task GivenAPastVersionShown_WhenBackToCurrent_ThenTheSecretCanBeEditedAgain()
    {
        // Arrange
        var secret = Login("Mail", "bob", "second");
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);
        await sut.History.SelectAsync(SetupVersion(secret, new WebSecret { Name = "Old mail" }, secret.Id));

        // Act
        sut.History.BackToCurrent();
        await sut.SetValueAsync("Name", "New mail");

        // Assert
        Assert.False(sut.IsViewingVersion);
        Assert.Equal("New mail", sut.GetValue("Name"));
        Assert.True(sut.HasUnsavedChanges);
    }

    [Fact]
    public async Task GivenAPastVersionShown_WhenRestored_ThenTheSecretIsShownAsTheVaultNowHasIt()
    {
        // Arrange
        var secret = Login("Mail", "bob", "second");
        var restored = new WebSecret { Name = "Old mail", UserName = "robert", Password = "first" };
        restored.SetData(nameof(Secret.Id), secret.Id);
        mockHistory.Setup(x => x.RestoreAsync(It.IsAny<SyncFileVersionContent>(), It.IsAny<CancellationToken>()))
            .Callback(() => secrets[secrets.FindIndex(s => s.Id == secret.Id)] = restored)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);
        await sut.History.SelectAsync(SetupVersion(secret, restored, secret.Id));

        // Act
        await sut.History.RestoreAsync();

        // Assert
        Assert.False(sut.IsViewingVersion);
        Assert.Equal("Old mail", sut.GetValue("Name"));
        Assert.Equal("robert", sut.GetValue("UserName"));
        Assert.False(sut.HasUnsavedChanges);
    }

    [Fact]
    public async Task GivenUnsavedChanges_WhenAPastVersionIsPickedAndSaveChosen_ThenTheyAreSavedAndTheEditorStaysOpen()
    {
        // Arrange
        var secret = Login("Mail", "bob", "second");

        // As the vault session does: the saved secret takes the place of the one it had.
        mockSession.Setup(x => x.SaveAsync(It.IsAny<Secret>(), It.IsAny<CancellationToken>()))
            .Callback<Secret, CancellationToken>((s, _) =>
            {
                saved = s;
                secrets[secrets.FindIndex(e => e.Id == s.Id)] = s;
            })
            .ReturnsAsync((Secret s, CancellationToken _) => s);
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);
        await sut.SetValueAsync("UserName", "alice");
        var version = SetupVersion(secret, new WebSecret { Name = "Old mail" }, secret.Id);

        // Act
        await sut.History.SelectAsync(version);
        var asked = sut.History.IsUnsavedDialogOpen;
        await sut.History.SaveAndViewAsync();

        // Assert
        Assert.True(asked);
        Assert.Equal("alice", saved?.GetData("UserName"));
        Assert.True(sut.IsViewingVersion);
        Assert.Equal("Old mail", sut.GetValue("Name"));
        sut.History.BackToCurrent();
        Assert.Equal("alice", sut.GetValue("UserName"));
        Assert.False(sut.HasUnsavedChanges);
    }

    [Fact]
    public async Task GivenUnsavedChanges_WhenAPastVersionIsPickedAndDontSaveChosen_ThenTheyAreDropped()
    {
        // Arrange
        var secret = Login("Mail", "bob", "second");
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);
        await sut.SetValueAsync("UserName", "alice");
        var version = SetupVersion(secret, new WebSecret { Name = "Old mail" }, secret.Id);

        // Act
        await sut.History.SelectAsync(version);
        await sut.History.DiscardAndViewAsync();
        sut.History.BackToCurrent();

        // Assert
        Assert.Null(saved);
        Assert.Equal("bob", sut.GetValue("UserName"));
        Assert.False(sut.HasUnsavedChanges);
    }

    [Fact]
    public async Task GivenAPastVersionThatIsNotTheSecret_WhenPicked_ThenItSaysSo_AndTheSecretStaysShown()
    {
        // Arrange
        var secret = Login("Mail", "bob", "second");
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);
        var version = VersionOf(secret);
        mockHistory.Setup(x => x.ReadVersionAsync(version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncFileVersionContent { Version = version, Content = [1, 2, 3] });
        mockSession.Setup(x => x.ReadSecret(secret.Id, It.IsAny<byte[]>())).Throws(new ClypseException(ClypseErrorCodes.SecretUnreadable));

        // Act
        await sut.History.SelectAsync(version);

        // Assert
        Assert.False(string.IsNullOrEmpty(sut.Error));
        Assert.False(sut.IsViewingVersion);
        Assert.Equal("Mail", sut.GetValue("Name"));
    }

    [Fact]
    public async Task GivenAPastVersionShown_WhenAnotherSecretIsLoaded_ThenTheVersionIsDropped()
    {
        // Arrange
        var secret = Login("Mail", "bob", "second");
        var other = Login("Bank", "bob", "third");
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);
        await sut.History.SelectAsync(SetupVersion(secret, new WebSecret { Name = "Old mail" }, secret.Id));

        // Act
        await sut.LoadAsync(other.Id);

        // Assert
        Assert.False(sut.IsViewingVersion);
        Assert.Null(sut.History.SelectedContent);
        Assert.Equal("Bank", sut.GetValue("Name"));
    }

    private static string SecretKeyOf(string id) => $"profiles/v1/secrets/{id}.json";

    private static SyncFileVersion VersionOf(Secret secret) => new()
    {
        Profile = "v1",
        Key = SecretKeyOf(secret.Id),
        VersionId = "v1",
        ETag = "\"abc\"",
        UploadedAt = new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero),
    };

    private SyncFileVersion SetupVersion(Secret secret, Secret past, string id)
    {
        var version = VersionOf(secret);
        byte[] content = [1, 2, 3];
        mockHistory.Setup(x => x.ReadVersionAsync(version, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncFileVersionContent { Version = version, Content = content });
        mockSession.Setup(x => x.ReadSecret(id, content)).Returns(past);
        return version;
    }
}
