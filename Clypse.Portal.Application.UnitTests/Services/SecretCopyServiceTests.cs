using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Devoctomy.Blazwiz.Testing;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Microsoft.JSInterop;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class SecretCopyServiceTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IClipboardService> mockClipboard = new();
    private readonly Mock<IToastService> mockToasts = new();
    private readonly ClypseSettings settings = new() { ClipboardClearSeconds = 20 };

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("clipboard", Assert.Throws<ArgumentNullException>(() => new SecretCopyService(null!, mockToasts.Object, settings, t)).ParamName);
        Assert.Equal("toasts", Assert.Throws<ArgumentNullException>(() => new SecretCopyService(mockClipboard.Object, null!, settings, t)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new SecretCopyService(mockClipboard.Object, mockToasts.Object, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new SecretCopyService(mockClipboard.Object, mockToasts.Object, settings, null!)).ParamName);
    }

    [Fact]
    public async Task GivenAValue_WhenCopied_ThenItStaysOnTheClipboard_AndTheToastSaysWhat()
    {
        // Act
        var copied = await CreateSut().CopyAsync("UserName", "bob", false);

        // Assert
        Assert.True(copied);
        mockClipboard.Verify(x => x.CopyAsync("bob", null), Times.Once);
        mockToasts.Verify(x => x.Show("User name copied.", ToastTone.Success, null), Times.Once);
    }

    [Fact]
    public async Task GivenASecretValue_WhenCopied_ThenItIsClearedLater_AndTheToastSaysWhen()
    {
        // Act
        var copied = await CreateSut().CopyAsync("Password", "hunter2", true);

        // Assert
        Assert.True(copied);
        mockClipboard.Verify(x => x.CopyAsync("hunter2", TimeSpan.FromSeconds(20)), Times.Once);
        mockToasts.Verify(x => x.Show("Password copied. It is cleared from the clipboard in 20 seconds.", ToastTone.Success, null), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GivenNoValue_WhenCopied_ThenNothingIsCopied(string? value)
    {
        // Act
        var copied = await CreateSut().CopyAsync("Password", value, true);

        // Assert
        Assert.False(copied);
        mockClipboard.VerifyNoOtherCalls();
        mockToasts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GivenTheBrowserRefuses_WhenCopied_ThenTheToastSaysItWasNot()
    {
        // Arrange
        mockClipboard.Setup(x => x.CopyAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>())).ThrowsAsync(new JSException("NotAllowedError"));

        // Act
        var copied = await CreateSut().CopyAsync("Password", "hunter2", true);

        // Assert
        Assert.False(copied);
        mockToasts.Verify(x => x.Show("Password could not be copied.", ToastTone.Danger, null), Times.Once);
    }

    [Fact]
    public async Task GivenAClearTimeUnderASecond_WhenASecretIsCopied_ThenItIsClearedAfterASecond()
    {
        // Arrange
        settings.ClipboardClearSeconds = 0;

        // Act
        await CreateSut().CopyAsync("Password", "hunter2", true);

        // Assert
        mockClipboard.Verify(x => x.CopyAsync("hunter2", TimeSpan.FromSeconds(1)), Times.Once);
    }

    private SecretCopyService CreateSut() => new(mockClipboard.Object, mockToasts.Object, settings, text.For<ClypseText>());
}
