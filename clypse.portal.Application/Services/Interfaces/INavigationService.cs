namespace clypse.portal.Application.Services.Interfaces;

/// <summary>
/// Provides navigation functionality for the application.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the specified URL.
    /// </summary>
    /// <param name="uri">The target URI to navigate to.</param>
    void NavigateTo(string uri);
}
