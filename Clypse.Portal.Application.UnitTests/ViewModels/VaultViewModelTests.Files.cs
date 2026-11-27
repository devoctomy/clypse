using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Vaults;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

// The vault's files, opened and closed with it, and the page's tabs.
public partial class VaultViewModelTests
{
    [Fact]
    public async Task GivenAVault_WhenLoaded_ThenItsFilesAreOpenedWithIt_AndClosedWhenThePageIsLeft()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("v1");
        var closedBeforeOpening = mockFiles.Invocations.Count(i => i.Method.Name == nameof(IVaultFileSession.Close));
        sut.Dispose();

        // Assert
        mockFiles.Verify(x => x.OpenAsync(vault, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, closedBeforeOpening);
        mockFiles.Verify(x => x.Close(), Times.Exactly(2));
    }

    [Fact]
    public async Task GivenAMissingVault_WhenLoaded_ThenItsFilesAreNotOpened()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("no-such-vault");

        // Assert
        Assert.True(sut.IsMissing);
        mockFiles.Verify(x => x.OpenAsync(It.IsAny<Vault>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenOpeningTheFilesFails_WhenLoaded_ThenTheErrorIsShown()
    {
        // Arrange
        mockFiles.Setup(x => x.OpenAsync(vault, It.IsAny<CancellationToken>())).ThrowsAsync(new ClypseException(ClypseErrorCodes.FileUnreadable));
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("v1");

        // Assert
        Assert.Equal("A file in this vault could not be read.", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenThePage_WhenATabIsSelected_ThenItShows_AndSecretsIsTheTabShownFirst()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        var first = sut.ActiveTab;
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        // Act
        sut.SelectTabCommand.Execute(VaultViewModel.VaultTab.Files);
        var files = sut.ActiveTab;
        sut.SelectTabCommand.Execute(VaultViewModel.VaultTab.Secrets);

        // Assert
        Assert.Equal(VaultViewModel.VaultTab.Secrets, first);
        Assert.Equal(VaultViewModel.VaultTab.Files, files);
        Assert.Equal(VaultViewModel.VaultTab.Secrets, sut.ActiveTab);
        Assert.Equal(2, changed.Count(p => p == nameof(VaultViewModel.ActiveTab)));
    }

    [Fact]
    public async Task GivenUnsavedEditsInTheEditor_WhenAnotherTabIsSelected_ThenTheTabOnlyChangesOnceTheyAreDealtWith()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        sut.SelectTab(VaultViewModel.VaultTab.Files);
        Action? held = null;
        guard.SetHolder(action =>
        {
            held = action;
            return true;
        });

        // Act
        sut.SelectTab(VaultViewModel.VaultTab.Secrets);
        var whileAsked = sut.ActiveTab;
        held?.Invoke();

        // Assert
        Assert.Equal(VaultViewModel.VaultTab.Files, whileAsked);
        Assert.NotNull(held);
        Assert.Equal(VaultViewModel.VaultTab.Secrets, sut.ActiveTab);
    }

    [Fact]
    public async Task GivenTheTabThatIsShowing_WhenSelectedAgain_ThenNothingIsAsked()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        var asked = 0;
        guard.SetHolder(_ =>
        {
            asked++;
            return true;
        });

        // Act
        sut.SelectTab(VaultViewModel.VaultTab.Secrets);

        // Assert
        Assert.Equal(0, asked);
        Assert.Equal(VaultViewModel.VaultTab.Secrets, sut.ActiveTab);
    }

    [Fact]
    public async Task GivenTheFilesTab_WhenAnotherVaultIsLoaded_ThenSecretsIsShownAgain()
    {
        // Arrange
        var other = new Vault { Id = "v2", Root = "profiles/v2/", Name = "Work", IsUnlocked = true };
        mockVaults.Setup(x => x.FindAsync("v2", It.IsAny<CancellationToken>())).ReturnsAsync(other);
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        sut.SelectTab(VaultViewModel.VaultTab.Files);

        // Act
        await sut.LoadAsync("v2");

        // Assert
        Assert.Equal(VaultViewModel.VaultTab.Secrets, sut.ActiveTab);
        mockFiles.Verify(x => x.OpenAsync(other, It.IsAny<CancellationToken>()), Times.Once);
    }
}
