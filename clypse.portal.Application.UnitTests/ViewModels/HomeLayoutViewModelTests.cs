using System.Text.Json;
using clypse.portal.Application.Services;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Aws;
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
        this.mockAuthService.Setup(s => s.Logout()).Returns(Task.CompletedTask);
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

    // --- Constructor ---

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
        // Arrange / Act / Assert
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
    public void GivenNullUserSettingsService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new HomeLayoutViewModel(
            this.mockAuthService.Object,
            null!,
            this.mockLocalStorageService.Object,
            this.mockBrowserInteropService.Object,
            this.mockNavigationService.Object,
            this.navigationStateService,
            this.appSettings));
    }

    [Fact]
    public void GivenNullLocalStorageService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new HomeLayoutViewModel(
            this.mockAuthService.Object,
            this.mockUserSettingsService.Object,
            null!,
            this.mockBrowserInteropService.Object,
            this.mockNavigationService.Object,
            this.navigationStateService,
            this.appSettings));
    }

    [Fact]
    public void GivenNullBrowserInteropService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new HomeLayoutViewModel(
            this.mockAuthService.Object,
            this.mockUserSettingsService.Object,
            this.mockLocalStorageService.Object,
            null!,
            this.mockNavigationService.Object,
            this.navigationStateService,
            this.appSettings));
    }

    [Fact]
    public void GivenNullNavigationService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new HomeLayoutViewModel(
            this.mockAuthService.Object,
            this.mockUserSettingsService.Object,
            this.mockLocalStorageService.Object,
            this.mockBrowserInteropService.Object,
            null!,
            this.navigationStateService,
            this.appSettings));
    }

    [Fact]
    public void GivenNullNavigationStateService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new HomeLayoutViewModel(
            this.mockAuthService.Object,
            this.mockUserSettingsService.Object,
            this.mockLocalStorageService.Object,
            this.mockBrowserInteropService.Object,
            this.mockNavigationService.Object,
            null!,
            this.appSettings));
    }

    [Fact]
    public void GivenNullAppSettings_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new HomeLayoutViewModel(
            this.mockAuthService.Object,
            this.mockUserSettingsService.Object,
            this.mockLocalStorageService.Object,
            this.mockBrowserInteropService.Object,
            this.mockNavigationService.Object,
            this.navigationStateService,
            null!));
    }

    // --- Initial state ---

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.Equal("light", sut.CurrentTheme);
        Assert.Equal("bi-moon", sut.ThemeIcon);
        Assert.False(sut.IsExpanded);
        Assert.Empty(sut.NavigationItems);
    }

    // --- AppSettings property ---

    [Fact]
    public void GivenInstance_WhenGetAppSettings_ThenReturnsCorrectSettings()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.Same(this.appSettings, sut.AppSettings);
    }

    // --- ToggleSidebar ---

    [Fact]
    public void GivenCollapsedSidebar_WhenToggleSidebar_ThenSidebarIsExpanded()
    {
        // Arrange
        var sut = CreateSut();

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

        // Act
        sut.ToggleSidebarCommand.Execute(null);

        // Assert
        Assert.False(sut.IsExpanded);
    }

    // --- ToggleThemeAsync ---

    [Fact]
    public async Task GivenLightTheme_WhenToggleTheme_ThenThemeChangesToDark()
    {
        // Arrange
        var sut = CreateSut();

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
        await sut.ToggleThemeCommand.ExecuteAsync(null);

        // Act
        await sut.ToggleThemeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("light", sut.CurrentTheme);
        Assert.Equal("bi-moon", sut.ThemeIcon);
    }

    // --- HandleLogoutAsync ---

    [Fact]
    public async Task GivenLoggedIn_WhenHandleLogout_ThenLogoutCalledAndNavigatesToLogin()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.HandleLogoutCommand.ExecuteAsync(null);

        // Assert
        this.mockAuthService.Verify(s => s.Logout(), Times.Once);
        this.mockNavigationService.Verify(n => n.NavigateTo("/login"), Times.Once);
    }

    // --- HandleNavigationAction ---

    [Fact]
    public async Task GivenExpandedSidebar_WhenHandleNavigationAction_ThenSidebarIsCollapsed()
    {
        // Arrange
        var sut = CreateSut();
        sut.ToggleSidebarCommand.Execute(null);

        // Act
        sut.HandleNavigationActionCommand.Execute("some-action");
        await Task.Delay(50);

        // Assert
        Assert.False(sut.IsExpanded);
    }

    // --- NavigationItems update ---

    [Fact]
    public void GivenNavigationItems_WhenNavigationStateServiceUpdates_ThenViewModelNavigationItemsUpdated()
    {
        // Arrange
        var sut = CreateSut();
        var items = new List<Models.Navigation.NavigationItem>
        {
            new() { Text = "Create Vault", Action = "create-vault" },
        };

        // Act
        this.navigationStateService.UpdateNavigationItems(items);

        // Assert
        Assert.Single(sut.NavigationItems);
        Assert.Equal("Create Vault", sut.NavigationItems[0].Text);
    }

    // --- OnAfterRenderAsync ---

    [Fact]
    public async Task GivenFirstRender_WhenOnAfterRenderAsync_ThenThemeIsInitialised()
    {
        // Arrange
        var sut = CreateSut();
        this.mockUserSettingsService.Setup(s => s.GetThemeAsync()).ReturnsAsync("dark");

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);

        // Assert
        Assert.Equal("dark", sut.CurrentTheme);
        Assert.Equal("bi-sun", sut.ThemeIcon);
    }

    [Fact]
    public async Task GivenNotFirstRender_WhenOnAfterRenderAsync_ThenThemeIsNotChanged()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.OnAfterRenderAsync(firstRender: false);

        // Assert
        this.mockUserSettingsService.Verify(s => s.GetThemeAsync(), Times.Never);
    }

    [Fact]
    public async Task GivenGetThemeThrows_WhenOnAfterRenderAsync_ThenThemeFallsBackToLight()
    {
        // Arrange
        var sut = CreateSut();
        this.mockUserSettingsService.Setup(s => s.GetThemeAsync()).ThrowsAsync(new Exception("storage error"));

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);

        // Assert
        Assert.Equal("light", sut.CurrentTheme);
        Assert.Equal("bi-moon", sut.ThemeIcon);
    }

    // --- UpdateSessionTimerAsync (via OnAfterRenderAsync with no stored credentials) ---

    [Fact]
    public async Task GivenNoStoredCredentials_WhenOnAfterRenderAsync_ThenLogoutIsCalled()
    {
        // Arrange
        var sut = CreateSut();
        this.mockLocalStorageService.Setup(s => s.GetItemAsync("clypse_credentials")).ReturnsAsync((string?)null);

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);
        await Task.Delay(50);

        // Assert
        this.mockAuthService.Verify(s => s.Logout(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GivenStoredCredentialsWithFutureExpiry_WhenOnAfterRenderAsync_ThenLogoutIsNotCalled()
    {
        // Arrange
        var sut = CreateSut();
        var futureExpiry = DateTime.UtcNow.AddHours(1).ToString("o");
        var credentials = new StoredCredentials { ExpirationTime = futureExpiry };
        this.mockLocalStorageService
            .Setup(s => s.GetItemAsync("clypse_credentials"))
            .ReturnsAsync(JsonSerializer.Serialize(credentials));

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);
        await Task.Delay(50);

        // Assert
        this.mockAuthService.Verify(s => s.Logout(), Times.Never);
    }

    [Fact]
    public async Task GivenStoredCredentialsWithPastExpiry_WhenOnAfterRenderAsync_ThenLogoutIsNotCalled()
    {
        // Arrange
        var sut = CreateSut();
        var pastExpiry = DateTime.UtcNow.AddHours(-1).ToString("o");
        var credentials = new StoredCredentials { ExpirationTime = pastExpiry };
        this.mockLocalStorageService
            .Setup(s => s.GetItemAsync("clypse_credentials"))
            .ReturnsAsync(JsonSerializer.Serialize(credentials));

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);
        await Task.Delay(50);

        // Assert
        this.mockAuthService.Verify(s => s.Logout(), Times.Never);
    }

    [Fact]
    public async Task GivenGetItemAsyncThrows_WhenOnAfterRenderAsync_ThenLogoutIsCalled()
    {
        // Arrange
        var sut = CreateSut();
        this.mockLocalStorageService
            .Setup(s => s.GetItemAsync("clypse_credentials"))
            .ThrowsAsync(new Exception("storage error"));

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);
        await Task.Delay(50);

        // Assert
        this.mockAuthService.Verify(s => s.Logout(), Times.AtLeastOnce);
    }

    // --- Dispose ---

    [Fact]
    public void GivenInstance_WhenDisposed_ThenNoExceptionIsThrown()
    {
        // Arrange
        var sut = CreateSut();

        // Act / Assert
        sut.Dispose();
    }
}
