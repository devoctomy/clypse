using clypse.portal.Application.Services;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Settings;
using Moq;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class HomeLayoutViewModelTests
{
    private readonly Mock<IAuthenticationService> mockAuthService;
    private readonly Mock<IUserSettingsService> mockUserSettingsService;
    private readonly Mock<ILocalStorageService> mockLocalStorageService;
    private readonly Mock<IBrowserInteropService> mockBrowserInteropService;
    private readonly Mock<INavigationService> mockNavigationService;
    private readonly NavigationStateService navigationStateService;
    private readonly AppSettings appSettings;

    public HomeLayoutViewModelTests()
    {
        this.mockAuthService = new Mock<IAuthenticationService>();
        this.mockUserSettingsService = new Mock<IUserSettingsService>();
        this.mockLocalStorageService = new Mock<ILocalStorageService>();
        this.mockBrowserInteropService = new Mock<IBrowserInteropService>();
        this.mockNavigationService = new Mock<INavigationService>();
        this.navigationStateService = new NavigationStateService();
        this.appSettings = new AppSettings();

        this.mockUserSettingsService.Setup(s => s.GetThemeAsync()).ReturnsAsync("light");
        this.mockUserSettingsService.Setup(s => s.SetThemeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        this.mockBrowserInteropService.Setup(s => s.SetThemeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        this.mockLocalStorageService.Setup(s => s.GetItemAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
    }

    private HomeLayoutViewModel CreateSut()
    {
        return new HomeLayoutViewModel(
            this.mockAuthService.Object,
            this.mockUserSettingsService.Object,
            this.mockLocalStorageService.Object,
            this.mockBrowserInteropService.Object,
            this.mockNavigationService.Object,
            this.navigationStateService,
            this.appSettings);
    }

    [Fact]
    public void GivenValidParameters_WhenConstructing_ThenCreatesInstance()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public void GivenNullAuthService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HomeLayoutViewModel(
            null!,
            this.mockUserSettingsService.Object,
            this.mockLocalStorageService.Object,
            this.mockBrowserInteropService.Object,
            this.mockNavigationService.Object,
            this.navigationStateService,
            this.appSettings));
    }

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.Equal("light", sut.CurrentTheme);
        Assert.Equal("bi-moon", sut.ThemeIcon);
        Assert.False(sut.IsExpanded);
        Assert.Empty(sut.NavigationItems);
    }

    [Fact]
    public void GivenCollapsedSidebar_WhenToggleSidebar_ThenSidebarIsExpanded()
    {
        // Arrange
        var sut = CreateSut();
        Assert.False(sut.IsExpanded);

        // Act
        sut.ToggleSidebarCommand.Execute(null);

        // Assert
        Assert.True(sut.IsExpanded);
    }

    [Fact]
    public void GivenExpandedSidebar_WhenToggleSidebar_ThenSidebarIsCollapsed()
    {
        // Arrange
        var sut = CreateSut();
        sut.ToggleSidebarCommand.Execute(null);
        Assert.True(sut.IsExpanded);

        // Act
        sut.ToggleSidebarCommand.Execute(null);

        // Assert
        Assert.False(sut.IsExpanded);
    }

    [Fact]
    public async Task GivenLightTheme_WhenToggleTheme_ThenThemeChangesToDark()
    {
        // Arrange
        var sut = CreateSut();
        Assert.Equal("light", sut.CurrentTheme);

        // Act
        await sut.ToggleThemeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("dark", sut.CurrentTheme);
        Assert.Equal("bi-sun", sut.ThemeIcon);
        this.mockUserSettingsService.Verify(s => s.SetThemeAsync("dark"), Times.Once);
        this.mockBrowserInteropService.Verify(s => s.SetThemeAsync("dark"), Times.Once);
    }

    [Fact]
    public async Task GivenDarkTheme_WhenToggleTheme_ThenThemeChangesToLight()
    {
        // Arrange
        var sut = CreateSut();
        // Toggle to dark first
        await sut.ToggleThemeCommand.ExecuteAsync(null);
        Assert.Equal("dark", sut.CurrentTheme);

        // Act - toggle back to light
        await sut.ToggleThemeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("light", sut.CurrentTheme);
        Assert.Equal("bi-moon", sut.ThemeIcon);
    }

    [Fact]
    public async Task GivenLoggedIn_WhenHandleLogout_ThenLogoutCalledAndNavigatesToLogin()
    {
        // Arrange
        var sut = CreateSut();
        this.mockAuthService.Setup(s => s.Logout()).Returns(Task.CompletedTask);

        // Act
        await sut.HandleLogoutCommand.ExecuteAsync(null);

        // Assert
        this.mockAuthService.Verify(s => s.Logout(), Times.Once);
        this.mockNavigationService.Verify(n => n.NavigateTo("/login"), Times.Once);
    }

    [Fact]
    public void GivenNavigationItems_WhenNavigationStateServiceUpdates_ThenViewModelNavigationItemsUpdated()
    {
        // Arrange
        var sut = CreateSut();
        var items = new List<Models.Navigation.NavigationItem>
        {
            new() { Text = "Create Vault", Action = "create-vault" }
        };

        // Act
        this.navigationStateService.UpdateNavigationItems(items);

        // Assert
        Assert.Single(sut.NavigationItems);
        Assert.Equal("Create Vault", sut.NavigationItems[0].Text);
    }

    [Fact]
    public async Task GivenExpandedSidebar_WhenHandleNavigationAction_ThenSidebarIsCollapsed()
    {
        // Arrange
        var sut = CreateSut();
        sut.ToggleSidebarCommand.Execute(null);
        Assert.True(sut.IsExpanded);

        // Act
        sut.HandleNavigationActionCommand.Execute("some-action");

        // Allow async processing
        await Task.Delay(50);

        // Assert
        Assert.False(sut.IsExpanded);
    }
}
