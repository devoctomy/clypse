using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Testing;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class VaultReportSettingsViewModelTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultReportPreferencesService> mockPreferences = new();
    private readonly Mock<IToastService> mockToasts = new();
    private VaultReportPreferences kept = new() { OldPasswordMonths = 3 };

    public VaultReportSettingsViewModelTests()
    {
        mockPreferences.Setup(x => x.GetAsync()).ReturnsAsync(() => kept);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("preferencesService", Assert.Throws<ArgumentNullException>(() => new VaultReportSettingsViewModel(null!, mockToasts.Object, text.Plurals, t)).ParamName);
        Assert.Equal("toasts", Assert.Throws<ArgumentNullException>(() => new VaultReportSettingsViewModel(mockPreferences.Object, null!, text.Plurals, t)).ParamName);
        Assert.Equal("plurals", Assert.Throws<ArgumentNullException>(() => new VaultReportSettingsViewModel(mockPreferences.Object, mockToasts.Object, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new VaultReportSettingsViewModel(mockPreferences.Object, mockToasts.Object, text.Plurals, null!)).ParamName);
    }

    [Fact]
    public async Task GivenKeptPreferences_WhenLoaded_ThenTheyAreShown()
    {
        // Arrange
        kept = new VaultReportPreferences { OldPasswordMonths = 6 };
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.True(sut.IsLoaded);
        Assert.Equal(6, sut.OldPasswordMonths);
        Assert.Equal(VaultReportSettingsViewModel.MonthsOptions, sut.Options);
    }

    [Fact]
    public async Task GivenAnAgeNotOffered_WhenLoaded_ThenItIsOfferedToo_InOrder()
    {
        // Arrange: e.g. the app's default changed in configuration.
        kept = new VaultReportPreferences { OldPasswordMonths = 9 };
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.Equal([1, 3, 6, 9, 12, 24], sut.Options);
    }

    [Fact]
    public async Task GivenAChange_WhenSaved_ThenItIsKept_AndTheToastSaysSo()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        sut.OldPasswordMonths = 12;

        // Act
        await sut.SaveCommand.ExecuteAsync(null);

        // Assert
        mockPreferences.Verify(x => x.SaveAsync(It.Is<VaultReportPreferences>(p => p.OldPasswordMonths == 12)), Times.Once);
        mockToasts.Verify(x => x.Show("Vault report settings saved.", ToastTone.Success, null), Times.Once);
    }

    [Fact]
    public void GivenAges_WhenWorded_ThenOneAndMany()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        Assert.Equal("1 month", sut.GetMonthsText(1));
        Assert.Equal("3 months", sut.GetMonthsText(3));
    }

    [Fact]
    public void GivenAnAgeUnderTheMinimum_WhenSet_ThenTheMinimumIsUsed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.OldPasswordMonths = 0;

        // Assert
        Assert.Equal(VaultReportPreferences.MinimumMonths, sut.OldPasswordMonths);
    }

    private VaultReportSettingsViewModel CreateSut() => new(mockPreferences.Object, mockToasts.Object, text.Plurals, text.For<ClypseText>());
}
