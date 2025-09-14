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

    private List<NavigationItem>? navigationItems;
    private Pages.Home? homePageRef;
    private string? sessionTimeRemaining;
    private Timer? sessionTimer;
    private string currentTheme = "light";
    private string themeIcon = "bi-moon";
    private bool isExpanded = true; // Start expanded by default

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

    private async Task InitializeSidebar()
    {
        try
        {
            var savedState = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "clypse_sidebar_expanded");
            if (!string.IsNullOrEmpty(savedState) && bool.TryParse(savedState, out bool expanded))
            {
                isExpanded = expanded;
            }
            else
            {
                // Default to collapsed on mobile, expanded on desktop
                var isMobile = await JSRuntime.InvokeAsync<bool>("eval", "window.matchMedia('(max-width: 768px)').matches");
                isExpanded = !isMobile;
            }
            StateHasChanged();
        }
        catch
        {
            // Default to expanded if we can't determine
            isExpanded = true;
        }
    }

    private async Task ToggleSidebar()
    {
        isExpanded = !isExpanded;
        
        try
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "clypse_sidebar_expanded", isExpanded.ToString());
        }
        catch
        {
            // Ignore localStorage errors
        }
        
        StateHasChanged();
    }

    private async Task InitializeTheme()
    {
        try
        {
            var savedTheme = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "clypse_theme");
            currentTheme = !string.IsNullOrEmpty(savedTheme) ? savedTheme : "light";
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
        
        await JSRuntime.InvokeVoidAsync("localStorage.setItem", "clypse_theme", currentTheme);
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
        
        try
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "clypse_sidebar_expanded", isExpanded.ToString());
        }
        catch
        {
            // Ignore localStorage errors
        }
        
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
        sessionTimer?.Dispose();
    }
}
