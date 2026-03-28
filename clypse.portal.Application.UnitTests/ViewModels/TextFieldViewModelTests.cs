using clypse.portal.Application.ViewModels;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class TextFieldViewModelTests
{
    private static TextFieldViewModel CreateSut() => new();

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var sut = CreateSut();

        Assert.Null(sut.Value);
        Assert.Null(sut.ValueChangedCallback);
    }

    [Fact]
    public async Task OnValueChangedAsync_SetsValue()
    {
        var sut = CreateSut();

        await sut.OnValueChangedAsync("hello");

        Assert.Equal("hello", sut.Value);
    }

    [Fact]
    public async Task OnValueChangedAsync_WithNullValue_SetsValueToNull()
    {
        var sut = CreateSut();
        sut.Value = "existing";

        await sut.OnValueChangedAsync(null);

        Assert.Null(sut.Value);
    }

    [Fact]
    public async Task OnValueChangedAsync_InvokesCallback_WithNewValue()
    {
        var sut = CreateSut();
        string? received = null;
        sut.ValueChangedCallback = v => { received = v; return Task.CompletedTask; };

        await sut.OnValueChangedAsync("world");

        Assert.Equal("world", received);
    }

    [Fact]
    public async Task OnValueChangedAsync_NullCallback_DoesNotThrow()
    {
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(() => sut.OnValueChangedAsync("value"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task OnValueChangedAsync_InvokesCallback_WithNullValue()
    {
        var sut = CreateSut();
        string? received = "sentinel";
        sut.ValueChangedCallback = v => { received = v; return Task.CompletedTask; };

        await sut.OnValueChangedAsync(null);

        Assert.Null(received);
    }
}
