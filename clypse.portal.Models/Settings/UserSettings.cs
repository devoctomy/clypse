namespace clypse.portal.Models.Settings;

/// <summary>
/// Represents user-specific settings for the application.
/// </summary>
public class UserSettings
{
    /// <summary>
    /// Gets or sets the theme preference (e.g., "light", "dark").
    /// </summary>
    public string Theme { get; set; } = "light";
}