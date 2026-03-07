using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.Navigation;

namespace clypse.portal.Application.Services;

/// <inheritdoc/>
public class NavigationStateService : INavigationStateService
{
    private List<NavigationItem> navigationItems = [];

    /// <inheritdoc/>
    public event EventHandler? NavigationItemsChanged;

    /// <inheritdoc/>
    public event EventHandler<string>? NavigationActionRequested;

    /// <inheritdoc/>
    public IReadOnlyList<NavigationItem> NavigationItems => navigationItems.AsReadOnly();

    /// <inheritdoc/>
    public void UpdateNavigationItems(IEnumerable<NavigationItem> items)
    {
        navigationItems = items.ToList();
        NavigationItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void RequestNavigationAction(string action)
    {
        NavigationActionRequested?.Invoke(this, action);
    }
}
