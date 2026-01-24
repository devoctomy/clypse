using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using clypse.portal.Models;
using clypse.portal.Services;

namespace clypse.portal.Layout;

public partial class HomeLayout : LayoutComponentBase, IDisposable
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public AppSettings AppSettings { get; set; } = default!;
    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public IUserSettingsService UserSettingsService { get; set; } = default!;

    private List<NavigationItem>? navigationItems;
    private Pages.Home? homePageRef;
    private string? sessionTimeRemaining;
    private Timer? sessionTimer;
    private string currentTheme = "light";
    private string themeIcon = "bi-moon";
    private bool isExpanded; // Start collapsed by default

    public void SetNavigationItems(List<NavigationItem> items)
    {
        navigationItems = items;
        StateHasChanged();
    }

    public void SetHomePageReference(Pages.Home homeRef)
    {
        homePageRef = homeRef;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeTheme();
            await InitializeSidebar();
            await StartSessionTimer();
        }
    }

    private Task InitializeSidebar()
    {
        try
        {
            // Always default to collapsed (false)
            isExpanded = false;
            StateHasChanged();
        }
        catch
        {
            // Default to collapsed if any error occurs
            isExpanded = false;
        }
        
        return Task.CompletedTask;
    }

    private Task ToggleSidebar()
    {
        isExpanded = !isExpanded;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task InitializeTheme()
    {
        try
        {
            currentTheme = await UserSettingsService.GetThemeAsync();
            themeIcon = currentTheme == "light" ? "bi-moon" : "bi-sun";
            
            await JSRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", currentTheme);
        }
        catch
        {
            currentTheme = "light";
            themeIcon = "bi-moon";
        }
    }

    private async Task ToggleTheme()
    {
        currentTheme = currentTheme == "light" ? "dark" : "light";
        themeIcon = currentTheme == "light" ? "bi-moon" : "bi-sun";
        
        await UserSettingsService.SetThemeAsync(currentTheme);
        await JSRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", currentTheme);
        
        StateHasChanged();
    }

    private async Task StartSessionTimer()
    {
        await UpdateSessionTimer();
        
        // Update timer every minute
        sessionTimer = new Timer(async _ =>
        {
            await InvokeAsync(async () =>
            {
                await UpdateSessionTimer();
                StateHasChanged();
            });
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    private async Task UpdateSessionTimer()
    {
        try
        {
            var credentialsJson = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "clypse_credentials");
            
            if (!string.IsNullOrEmpty(credentialsJson))
            {
                var credentials = System.Text.Json.JsonSerializer.Deserialize<StoredCredentials>(credentialsJson);
                
                if (credentials != null && !string.IsNullOrEmpty(credentials.ExpirationTime))
                {
                    var expirationTime = DateTime.Parse(credentials.ExpirationTime);
                    var timeRemaining = expirationTime - DateTime.UtcNow;
                    
                    if (timeRemaining.TotalMinutes > 0)
                    {
                        if (timeRemaining.TotalHours >= 1)
                        {
                            var hours = (int)timeRemaining.TotalHours;
                            var minutes = timeRemaining.Minutes;
                            sessionTimeRemaining = minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
                        }
                        else
                        {
                            var minutes = (int)timeRemaining.TotalMinutes;
                            sessionTimeRemaining = $"{minutes} minute{(minutes != 1 ? "s" : "")}";
                        }
                    }
                    else
                    {
                        sessionTimeRemaining = "expired";
                    }
                }
                else
                {
                    sessionTimeRemaining = null;
                }
            }
            else
            {
                sessionTimeRemaining = null;
            }
        }
        catch
        {
            sessionTimeRemaining = null;
        }
    }

    private async Task HandleNavigationAction(string action)
    {
        // Close sidebar when any navigation action is clicked
        isExpanded = false;
        StateHasChanged();
        
        if (homePageRef != null)
        {
            await homePageRef.HandleNavigationAction(action);
        }
    }

    private async Task HandleLogout()
    {
        // Use the proper authentication service logout
        await AuthService.Logout();
        
        // Redirect to login page
        Navigation.NavigateTo("/login");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        sessionTimer?.Dispose();
    }
}
