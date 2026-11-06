using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Devoctomy.Blazwiz.Auth.Models;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VaultLabelHandlerTests
{
    private readonly Mock<IE2eProtectedProfileManager> mockManager = new();
    private readonly Mock<IVaultService> mockVaults = new();
    private readonly HashSet<E2eKeyScope> unlocked = [];

    public VaultLabelHandlerTests()
    {
        mockManager.Setup(x => x.IsUnlocked(It.IsAny<E2eKeyScope>())).Returns((E2eKeyScope s) => unlocked.Contains(s));
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("manager", Assert.Throws<ArgumentNullException>(() => new VaultLabelHandler(null!, mockVaults.Object)).ParamName);
        Assert.Equal("vaultService", Assert.Throws<ArgumentNullException>(() => new VaultLabelHandler(mockManager.Object, null!)).ParamName);
    }

    [Fact]
    public async Task GivenASessionStarted_WhenAVaultIsUnlocked_ThenItIsFoundSoItIsLabelled()
    {
        // Arrange
        using var sut = new VaultLabelHandler(mockManager.Object, mockVaults.Object);
        await sut.OnSessionStartedAsync(new UserSession());
        await sut.OnSessionStartedAsync(new UserSession());
        var scope = new E2eKeyScope(Guid.NewGuid().ToString("D"));
        unlocked.Add(scope);

        // Act
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, scope);

        // Assert
        mockVaults.Verify(x => x.FindAsync(scope.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenASessionStarted_WhenAVaultIsLocked_ThenNothingIsRead()
    {
        // Arrange
        using var sut = new VaultLabelHandler(mockManager.Object, mockVaults.Object);
        await sut.OnSessionStartedAsync(new UserSession());

        // Act
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, new E2eKeyScope(Guid.NewGuid().ToString("D")));

        // Assert
        mockVaults.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GivenFindingFails_WhenAVaultIsUnlocked_ThenNothingIsThrown()
    {
        // Arrange
        using var sut = new VaultLabelHandler(mockManager.Object, mockVaults.Object);
        await sut.OnSessionStartedAsync(new UserSession());
        var scope = new E2eKeyScope(Guid.NewGuid().ToString("D"));
        unlocked.Add(scope);
        mockVaults.Setup(x => x.FindAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("gone"));

        // Act
        var ex = Record.Exception(() => mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, scope));

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public async Task GivenSigningOut_WhenAVaultIsUnlocked_ThenNothingIsRead()
    {
        // Arrange
        using var sut = new VaultLabelHandler(mockManager.Object, mockVaults.Object);
        await sut.OnSessionStartedAsync(new UserSession());
        var scope = new E2eKeyScope(Guid.NewGuid().ToString("D"));
        unlocked.Add(scope);

        // Act
        await sut.OnSigningOutAsync();
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, scope);

        // Assert
        mockVaults.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GivenDisposed_WhenAVaultIsUnlocked_ThenNothingIsRead()
    {
        // Arrange
        var sut = new VaultLabelHandler(mockManager.Object, mockVaults.Object);
        await sut.OnSessionStartedAsync(new UserSession());
        var scope = new E2eKeyScope(Guid.NewGuid().ToString("D"));
        unlocked.Add(scope);

        // Act
        sut.Dispose();
        sut.Dispose();
        mockManager.Raise(x => x.LockStateChanged += null, mockManager.Object, scope);

        // Assert
        mockVaults.VerifyNoOtherCalls();
    }
}
