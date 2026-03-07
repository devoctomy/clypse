using clypse.core.Vault;
using clypse.portal.Application.Services;
using clypse.portal.Models.Vault;
using Moq;

namespace clypse.portal.Application.UnitTests.Services.Navigation;

public class VaultStateServiceTests
{
    private readonly VaultStateService sut;

    public VaultStateServiceTests()
    {
        this.sut = new VaultStateService();
    }

    [Fact]
    public void GivenNoState_WhenCreated_ThenAllPropertiesAreNull()
    {
        // Assert
        Assert.Null(this.sut.CurrentVault);
        Assert.Null(this.sut.CurrentVaultKey);
        Assert.Null(this.sut.LoadedVault);
        Assert.Null(this.sut.VaultManager);
    }

    [Fact]
    public void GivenVaultData_WhenSetVaultState_ThenAllPropertiesAreSet()
    {
        // Arrange
        var vault = new VaultMetadata { Id = "test-id", Name = "Test Vault" };
        var key = "test-key";
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();

        // Act
        this.sut.SetVaultState(vault, key, mockVault.Object, mockManager.Object);

        // Assert
        Assert.Same(vault, this.sut.CurrentVault);
        Assert.Equal(key, this.sut.CurrentVaultKey);
        Assert.Same(mockVault.Object, this.sut.LoadedVault);
        Assert.Same(mockManager.Object, this.sut.VaultManager);
    }

    [Fact]
    public void GivenVaultState_WhenClearVaultState_ThenAllPropertiesAreNull()
    {
        // Arrange
        var vault = new VaultMetadata { Id = "test-id" };
        var mockVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        this.sut.SetVaultState(vault, "key", mockVault.Object, mockManager.Object);

        // Act
        this.sut.ClearVaultState();

        // Assert
        Assert.Null(this.sut.CurrentVault);
        Assert.Null(this.sut.CurrentVaultKey);
        Assert.Null(this.sut.LoadedVault);
        Assert.Null(this.sut.VaultManager);
    }

    [Fact]
    public void GivenSubscriber_WhenSetVaultState_ThenEventIsRaised()
    {
        // Arrange
        var eventRaised = false;
        this.sut.VaultStateChanged += (_, _) => eventRaised = true;

        // Act
        this.sut.SetVaultState(
            new VaultMetadata { Id = "id" },
            "key",
            new Mock<IVault>().Object,
            new Mock<IVaultManager>().Object);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void GivenSubscriber_WhenClearVaultState_ThenEventIsRaised()
    {
        // Arrange
        var eventRaised = false;
        this.sut.VaultStateChanged += (_, _) => eventRaised = true;

        // Act
        this.sut.ClearVaultState();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void GivenVaultState_WhenUpdateLoadedVault_ThenLoadedVaultIsUpdated()
    {
        // Arrange
        var vault = new VaultMetadata { Id = "test-id", Name = "Test Vault" };
        var mockOldVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        this.sut.SetVaultState(vault, "key", mockOldVault.Object, mockManager.Object);

        var mockNewVault = new Mock<IVault>();
        mockNewVault.Setup(v => v.Index).Returns(new VaultIndex { Entries = [] });
        mockNewVault.Setup(v => v.Info).Returns(new VaultInfo("Test", "Desc"));

        // Act
        this.sut.UpdateLoadedVault(mockNewVault.Object);

        // Assert
        Assert.Same(mockNewVault.Object, this.sut.LoadedVault);
    }

    [Fact]
    public void GivenVaultState_WhenUpdateLoadedVault_ThenEventIsRaised()
    {
        // Arrange
        var vault = new VaultMetadata { Id = "test-id", Name = "Test Vault" };
        var mockOldVault = new Mock<IVault>();
        var mockManager = new Mock<IVaultManager>();
        this.sut.SetVaultState(vault, "key", mockOldVault.Object, mockManager.Object);

        var mockNewVault = new Mock<IVault>();
        mockNewVault.Setup(v => v.Index).Returns(new VaultIndex { Entries = [] });
        mockNewVault.Setup(v => v.Info).Returns(new VaultInfo("Test", "Desc"));

        var eventRaised = false;
        this.sut.VaultStateChanged += (_, _) => eventRaised = true;

        // Act
        this.sut.UpdateLoadedVault(mockNewVault.Object);

        // Assert
        Assert.True(eventRaised);
    }
}
