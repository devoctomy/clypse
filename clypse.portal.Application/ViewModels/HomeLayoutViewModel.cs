using System.Text.Json;
using Blazing.Mvvm.ComponentModel;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.Aws;
using clypse.portal.Models.Navigation;
using clypse.portal.Models.Settings;
using CommunityToolkit.Mvvm.Input;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the home layout, managing the sidebar, theme, session timer, and navigation.
/// </summary>
public partial class HomeLayoutViewModel : ViewModelBase
{
    private readonly IAuthenticationService authService;
    private readonly IUserSettingsService userSettingsService;
    private readonly ILocalStorageService localStorageService;
    private readonly IBrowserInteropService browserInteropService;
    private readonly INavigationService navigationService;
    private readonly INavigationStateService navigationStateService;
    private readonly AppSettings appSettings;

    private Timer? sessionTimer;
    private string currentTheme = "light";
    private string themeIcon = "bi-moon";
    private bool isExpanded;

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeLayoutViewModel"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="userSettingsService">The user settings service.</param>
    /// <param name="localStorageService">The local storage service.</param>
    /// <param name="browserInteropService">The browser interop service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="navigationStateService">The navigation state service.</param>
    /// <param name="appSettings">The application settings.</param>
    public HomeLayoutViewModel(
        IAuthenticationService authService,
        IUserSettingsService userSettingsService,
        ILocalStorageService localStorageService,
        IBrowserInteropService browserInteropService,
        INavigationService navigationService,
        INavigationStateService navigationStateService,
        AppSettings appSettings)
    {
        this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
        this.userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
        this.localStorageService = localStorageService ?? throw new ArgumentNullException(nameof(localStorageService));
        this.browserInteropService = browserInteropService ?? throw new ArgumentNullException(nameof(browserInteropService));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        this.navigationStateService = navigationStateService ?? throw new ArgumentNullException(nameof(navigationStateService));
        this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

        this.navigationStateService.NavigationItemsChanged += OnNavigationItemsChanged;
    }

    /// <summary>
    /// Gets the current list of navigation items for the sidebar.
    /// </summary>
    public IReadOnlyList<NavigationItem> NavigationItems => navigationStateService.NavigationItems;

    /// <summary>
    /// Gets the current theme name.
    /// </summary>
    public string CurrentTheme
    {
        get => currentTheme;
        private set => SetProperty(ref currentTheme, value);
    }

    /// <summary>
    /// Gets the icon class for the theme switcher button.
    /// </summary>
    public string ThemeIcon
    {
        get => themeIcon;
        private set => SetProperty(ref themeIcon, value);
    }

    /// <summary>
    /// Gets a value indicating whether the sidebar is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => isExpanded;
        private set => SetProperty(ref isExpanded, value);
    }

    /// <summary>
    /// Gets the application settings.
    /// </summary>
    public AppSettings AppSettings => appSettings;

    /// <inheritdoc/>
    public override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeThemeAsync();
            await StartSessionTimerAsync();
        }
    }

    /// <summary>
    /// Toggles the sidebar expanded/collapsed state.
    /// </summary>
    [RelayCommand]
    public void ToggleSidebar()
    {
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// Toggles between light and dark themes.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task ToggleThemeAsync()
    {
        CurrentTheme = CurrentTheme == "light" ? "dark" : "light";
        ThemeIcon = CurrentTheme == "light" ? "bi-moon" : "bi-sun";
        await userSettingsService.SetThemeAsync(CurrentTheme);
        await browserInteropService.SetThemeAsync(CurrentTheme);
    }

    /// <summary>
    /// Handles a navigation action request from the sidebar.
    /// </summary>
    /// <param name="action">The action identifier.</param>
    [RelayCommand]
    public void HandleNavigationAction(string action)
    {
        IsExpanded = false;
        navigationStateService.RequestNavigationAction(action);
    }

    /// <summary>
    /// Handles logout.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task HandleLogoutAsync()
    {
        await authService.Logout();
        navigationService.NavigateTo("/login");
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            navigationStateService.NavigationItemsChanged -= OnNavigationItemsChanged;
            sessionTimer?.Dispose();
            sessionTimer = null;
        }

        base.Dispose(disposing);
    }

    private static bool ValidateCredentialsExpiry(StoredCredentials? credentials)
    {
        if (credentials != null && !string.IsNullOrEmpty(credentials.ExpirationTime))
        {
            var expirationTime = DateTime.Parse(credentials.ExpirationTime);
            var timeRemaining = expirationTime - DateTime.UtcNow;

            if (timeRemaining.TotalMinutes > 0)
            {
                return false;
            }
            else
            {
                // TODO: If user is 'remembered' then we can automatically refresh credentials here instead of logging out, but for now we will just log out when credentials expire
                return false;
            }
        }

        return true;
    }

    private async Task InitializeThemeAsync()
    {
        try
        {
            CurrentTheme = await userSettingsService.GetThemeAsync();
            ThemeIcon = CurrentTheme == "light" ? "bi-moon" : "bi-sun";
            await browserInteropService.SetThemeAsync(CurrentTheme);
        }
        catch
        {
            CurrentTheme = "light";
            ThemeIcon = "bi-moon";
        }
    }

    private async Task StartSessionTimerAsync()
    {
        await UpdateSessionTimerAsync();

        sessionTimer = new Timer(
            async _ =>
            {
                await UpdateSessionTimerAsync();
            },
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
    }

    private async Task UpdateSessionTimerAsync()
    {
        try
        {
            var credentialsJson = await localStorageService.GetItemAsync("clypse_credentials");

            if (!string.IsNullOrEmpty(credentialsJson))
            {
                var credentials = JsonSerializer.Deserialize<StoredCredentials>(credentialsJson);

                bool valid = ValidateCredentialsExpiry(credentials);
                if (!valid)
                {
                    return;
                }
            }

            await HandleLogoutAsync();
        }
        catch
        {
            await HandleLogoutAsync();
        }
    }

    private void OnNavigationItemsChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(NavigationItems));
    }
}
