using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Vault;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class UnlockVaultDialogViewModelTests
{
    private UnlockVaultDialogViewModel CreateSut() => new();

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var sut = this.CreateSut();
        Assert.Equal(string.Empty, sut.Passphrase);
        Assert.Null(sut.Vault);
    }

    [Fact]
    public void ResetPassphrase_ClearsPassphrase()
    {
        var sut = this.CreateSut();
        sut.Passphrase = "secret";

        sut.ResetPassphrase();

        Assert.Equal(string.Empty, sut.Passphrase);
    }

    [Fact]
    public async Task UnlockCommand_WithPassphrase_InvokesCallback()
    {
        var sut = this.CreateSut();
        sut.Passphrase = "mypassphrase";
        string? received = null;
        sut.OnUnlockCallback = p => { received = p; return Task.CompletedTask; };

        await sut.UnlockCommand.ExecuteAsync(null);

        Assert.Equal("mypassphrase", received);
    }

    [Fact]
    public async Task UnlockCommand_WithEmptyPassphrase_DoesNotInvokeCallback()
    {
        var sut = this.CreateSut();
        var called = false;
        sut.OnUnlockCallback = _ => { called = true; return Task.CompletedTask; };

        await sut.UnlockCommand.ExecuteAsync(null);

        Assert.False(called);
    }

    [Fact]
    public async Task CancelCommand_ClearsPassphraseAndInvokesCallback()
    {
        var sut = this.CreateSut();
        sut.Passphrase = "secret";
        var called = false;
        sut.OnCancelCallback = () => { called = true; return Task.CompletedTask; };

        await sut.CancelCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, sut.Passphrase);
        Assert.True(called);
    }
}
