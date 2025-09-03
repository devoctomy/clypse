using Microsoft.AspNetCore.Components;
using clypse.portal.Services;

namespace clypse.portal.Pages;

public partial class Login : ComponentBase
{
    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;

    private LoginModel loginModel = new();
    private bool isLoading = false;
    private string? errorMessage;

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
            
            // Check if already authenticated
            if (await AuthService.CheckAuthentication())
            {
                Navigation.NavigateTo("/");
            }
        }
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
