using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Testing;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class ClipboardSettingsViewModelTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IClipboardPreferencesService> mockPreferences = new();
    private readonly Mock<IToastService> mockToasts = new();
    private ClipboardPreferences kept = new() { ClearSecrets = true, ClearAfterSeconds = 30 };

    public ClipboardSettingsViewModelTests()
    {
        mockPreferences.Setup(x => x.GetAsync()).ReturnsAsync(() => kept);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("preferencesService", Assert.Throws<ArgumentNullException>(() => new ClipboardSettingsViewModel(null!, mockToasts.Object, text.Plurals, t)).ParamName);
        Assert.Equal("toasts", Assert.Throws<ArgumentNullException>(() => new ClipboardSettingsViewModel(mockPreferences.Object, null!, text.Plurals, t)).ParamName);
        Assert.Equal("plurals", Assert.Throws<ArgumentNullException>(() => new ClipboardSettingsViewModel(mockPreferences.Object, mockToasts.Object, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new ClipboardSettingsViewModel(mockPreferences.Object, mockToasts.Object, text.Plurals, null!)).ParamName);
    }

    [Fact]
    public async Task GivenKeptPreferences_WhenLoaded_ThenTheyAreShown()
    {
        // Arrange
        kept = new ClipboardPreferences { ClearSecrets = false, ClearAfterSeconds = 60 };
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.True(sut.IsLoaded);
        Assert.False(sut.ClearSecrets);
        Assert.Equal(60, sut.ClearAfterSeconds);
        Assert.Equal(ClipboardSettingsViewModel.SecondsOptions, sut.Options);
    }

    [Fact]
    public async Task GivenATimeNotOffered_WhenLoaded_ThenItIsOfferedToo_InOrder()
    {
        // Arrange: e.g. the app's default changed in configuration.
        kept = new ClipboardPreferences { ClearAfterSeconds = 45 };
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.Equal([10, 15, 30, 45, 60, 120], sut.Options);
    }

    [Fact]
    public async Task GivenChanges_WhenSaved_ThenTheyAreKept_AndTheToastSaysSo()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        sut.ClearSecrets = false;
        sut.ClearAfterSeconds = 15;

        // Act
        await sut.SaveCommand.ExecuteAsync(null);

        // Assert
        mockPreferences.Verify(x => x.SaveAsync(It.Is<ClipboardPreferences>(p => !p.ClearSecrets && p.ClearAfterSeconds == 15)), Times.Once);
        mockToasts.Verify(x => x.Show("Clipboard settings saved.", ToastTone.Success, null), Times.Once);
    }

    [Fact]
    public void GivenTimes_WhenWorded_ThenOneAndMany()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        Assert.Equal("1 second", sut.GetSecondsText(1));
        Assert.Equal("30 seconds", sut.GetSecondsText(30));
    }

    [Fact]
    public void GivenATimeUnderTheMinimum_WhenSet_ThenTheMinimumIsUsed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ClearAfterSeconds = 0;

        // Assert
        Assert.Equal(ClipboardPreferences.MinimumSeconds, sut.ClearAfterSeconds);
    }

    private ClipboardSettingsViewModel CreateSut() => new(mockPreferences.Object, mockToasts.Object, text.Plurals, text.For<ClypseText>());
}
