using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Vault;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class VaultDeleteConfirmDialogViewModelTests
{
    private VaultDeleteConfirmDialogViewModel CreateSut() => new();

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var sut = this.CreateSut();
        Assert.Equal(string.Empty, sut.ConfirmationText);
        Assert.Null(sut.VaultToDelete);
        Assert.False(sut.IsConfirmationValid);
    }

    [Fact]
    public void ExpectedConfirmationText_WithName_ReturnsName()
    {
        var sut = this.CreateSut();
        sut.VaultToDelete = new VaultMetadata { Id = "vault-id", Name = "My Vault" };
        Assert.Equal("My Vault", sut.ExpectedConfirmationText);
    }

    [Fact]
    public void ExpectedConfirmationText_WithoutName_ReturnsId()
    {
        var sut = this.CreateSut();
        sut.VaultToDelete = new VaultMetadata { Id = "vault-id", Name = string.Empty };
        Assert.Equal("vault-id", sut.ExpectedConfirmationText);
    }

    [Fact]
    public void IsConfirmationValid_WhenTextMatches_ReturnsTrue()
    {
        var sut = this.CreateSut();
        sut.VaultToDelete = new VaultMetadata { Id = "vault-id", Name = "My Vault" };
        sut.ConfirmationText = "My Vault";
        Assert.True(sut.IsConfirmationValid);
    }

    [Fact]
    public void IsConfirmationValid_WhenTextDoesNotMatch_ReturnsFalse()
    {
        var sut = this.CreateSut();
        sut.VaultToDelete = new VaultMetadata { Id = "vault-id", Name = "My Vault" };
        sut.ConfirmationText = "my vault";
        Assert.False(sut.IsConfirmationValid);
    }

    [Fact]
    public void IsConfirmationValid_WithLeadingWhitespace_MatchesTrimmed()
    {
        var sut = this.CreateSut();
        sut.VaultToDelete = new VaultMetadata { Id = "vault-id", Name = "My Vault" };
        sut.ConfirmationText = "  My Vault  ";
        Assert.True(sut.IsConfirmationValid);
    }

    [Fact]
    public void Reset_ClearsConfirmationText()
    {
        var sut = this.CreateSut();
        sut.VaultToDelete = new VaultMetadata { Id = "id", Name = "Name" };
        sut.ConfirmationText = "Name";

        sut.Reset();

        Assert.Equal(string.Empty, sut.ConfirmationText);
    }
}
