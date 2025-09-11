using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using clypse.portal.Services;
using clypse.portal.Models;

namespace clypse.portal.Pages;

public partial class Login : ComponentBase
{
    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public AppSettings AppSettings { get; set; } = default!;

    private LoginModel loginModel = new();
    private bool isLoading = false;
    private string? errorMessage;
    private string currentTheme = "light";
    private string themeIcon = "bi-moon";

#if DEBUG
    private bool IsDebugBuild => true;
#else
    private bool IsDebugBuild => false;
#endif

    private class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await AuthService.Initialize();
            await InitializeTheme();
            
            // Check if already authenticated
            if (await AuthService.CheckAuthentication())
            {
                Navigation.NavigateTo("/");
            }
        }
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

    private async Task HandleLogin()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var result = await AuthService.Login(loginModel.Username, loginModel.Password);

            if (result.Success)
            {
                Navigation.NavigateTo("/");
            }
            else
            {
                errorMessage = result.Error ?? "Login failed";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}
