using clypse.portal.Models.Settings;

namespace clypse.portal.Application.Services.Interfaces;

/// <summary>
/// Provides access to user-specific settings and preferences.
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Retrieves the current user settings.
    /// </summary>
    /// <returns>The user settings object containing all user preferences.</returns>
    Task<UserSettings> GetSettingsAsync();

    /// <summary>
    /// Saves the user settings to persistent storage.
    /// </summary>
    /// <param name="settings">The user settings to save.</param>
    /// <returns>Nothing.</returns>
    Task SaveSettingsAsync(UserSettings settings);

    /// <summary>
    /// Retrieves the current theme setting.
    /// </summary>
    /// <returns>The name of the currently active theme.</returns>
    Task<string> GetThemeAsync();

    /// <summary>
    /// Sets the theme for the application.
    /// </summary>
    /// <param name="theme">The name of the theme to apply.</param>
    /// <returns>Nothing.</returns>
    Task SetThemeAsync(string theme);
}
