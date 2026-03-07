using clypse.portal.Models.Navigation;

namespace clypse.portal.Application.Services.Interfaces;

/// <summary>
/// Manages navigation state shared between the home page and its layout.
/// </summary>
public interface INavigationStateService
{
    /// <summary>
    /// Gets the current list of navigation items to display in the sidebar.
    /// </summary>
    IReadOnlyList<NavigationItem> NavigationItems { get; }

    /// <summary>
    /// Occurs when the navigation items collection is updated.
    /// </summary>
    event EventHandler? NavigationItemsChanged;

    /// <summary>
    /// Occurs when a navigation action has been requested by the sidebar.
    /// </summary>
    event EventHandler<string>? NavigationActionRequested;

    /// <summary>
    /// Updates the navigation items displayed in the sidebar.
    /// </summary>
    /// <param name="items">The new list of navigation items.</param>
    void UpdateNavigationItems(IEnumerable<NavigationItem> items);

    /// <summary>
    /// Requests that the specified navigation action be handled.
    /// </summary>
    /// <param name="action">The action identifier to handle.</param>
    void RequestNavigationAction(string action);
}
