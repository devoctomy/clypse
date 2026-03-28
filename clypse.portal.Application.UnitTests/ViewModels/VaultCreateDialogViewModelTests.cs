using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Vault;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class VaultCreateDialogViewModelTests
{
    private VaultCreateDialogViewModel CreateSut() => new();

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var sut = this.CreateSut();
        Assert.Equal(string.Empty, sut.VaultName);
        Assert.Equal(string.Empty, sut.VaultDescription);
        Assert.Equal(string.Empty, sut.VaultPassphrase);
        Assert.Equal(string.Empty, sut.VaultPassphraseConfirm);
        Assert.False(sut.IsFormValid);
    }

    [Theory]
    [InlineData("", "Desc", "Pass1234", "Pass1234", false)]
    [InlineData("Name", "", "Pass1234", "Pass1234", false)]
    [InlineData("Name", "Desc", "", "", false)]
    [InlineData("Name", "Desc", "short", "short", false)]
    [InlineData("Name", "Desc", "Pass1234", "Different", false)]
    [InlineData("Name", "Desc", "Pass1234", "Pass1234", true)]
    public void IsFormValid_WithVariousInputs_ReturnsExpected(
        string name, string desc, string pass, string confirm, bool expected)
    {
        var sut = this.CreateSut();
        sut.VaultName = name;
        sut.VaultDescription = desc;
        sut.VaultPassphrase = pass;
        sut.VaultPassphraseConfirm = confirm;

        Assert.Equal(expected, sut.IsFormValid);
    }

    [Fact]
    public void ClearForm_ResetsAllFields()
    {
        var sut = this.CreateSut();
        sut.VaultName = "Test";
        sut.VaultDescription = "Desc";
        sut.VaultPassphrase = "Pass1234";
        sut.VaultPassphraseConfirm = "Pass1234";

        sut.ClearForm();

        Assert.Equal(string.Empty, sut.VaultName);
        Assert.Equal(string.Empty, sut.VaultDescription);
        Assert.Equal(string.Empty, sut.VaultPassphrase);
        Assert.Equal(string.Empty, sut.VaultPassphraseConfirm);
    }

    [Fact]
    public async Task CreateVaultCommand_WhenFormValid_InvokesCallback()
    {
        var sut = this.CreateSut();
        sut.VaultName = "MyVault";
        sut.VaultDescription = "Desc";
        sut.VaultPassphrase = "Pass1234";
        sut.VaultPassphraseConfirm = "Pass1234";

        VaultCreationRequest? received = null;
        sut.OnCreateVaultCallback = r => { received = r; return Task.CompletedTask; };

        await sut.CreateVaultCommand.ExecuteAsync(null);

        Assert.NotNull(received);
        Assert.Equal("MyVault", received.Name);
    }

    [Fact]
    public async Task CreateVaultCommand_WhenFormInvalid_DoesNotInvokeCallback()
    {
        var sut = this.CreateSut();
        var called = false;
        sut.OnCreateVaultCallback = _ => { called = true; return Task.CompletedTask; };

        await sut.CreateVaultCommand.ExecuteAsync(null);

        Assert.False(called);
    }

    [Fact]
    public async Task CancelCommand_InvokesCallback()
    {
        var sut = this.CreateSut();
        var called = false;
        sut.OnCancelCallback = () => { called = true; return Task.CompletedTask; };

        await sut.CancelCommand.ExecuteAsync(null);

        Assert.True(called);
    }
}
