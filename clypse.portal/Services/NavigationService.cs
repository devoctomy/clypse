using clypse.portal.Application.Services.Interfaces;
using Microsoft.AspNetCore.Components;

namespace clypse.portal.Services;

/// <inheritdoc/>
public class NavigationService(NavigationManager navigationManager) : INavigationService
{
    private readonly NavigationManager navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));

    /// <inheritdoc/>
    public void NavigateTo(string uri)
    {
        navigationManager.NavigateTo(uri);
    }
}
