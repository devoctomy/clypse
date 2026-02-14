namespace clypse.portal.Models.Navigation;

/// <summary>
/// Represents a navigation item in the application.
/// </summary>
public class NavigationItem
{
    /// <summary>
    /// Gets or sets the display text for the navigation item.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action to perform when the navigation item is selected.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon class for the navigation item (e.g., Bootstrap Icons).
    /// </summary>
    public string Icon { get; set; } = "bi bi-circle";

    /// <summary>
    /// Gets or sets the button CSS class for styling the navigation item.
    /// </summary>
    public string ButtonClass { get; set; } = "btn-primary";
}
