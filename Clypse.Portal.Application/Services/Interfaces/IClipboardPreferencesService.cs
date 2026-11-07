using Clypse.Portal.Models.Settings;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Keeps on the device whether, and when, copied secret values are cleared from the clipboard.
/// </summary>
public interface IClipboardPreferencesService
{
    /// <summary>
    /// Gets the preferences: those kept on the device, or the app's defaults.
    /// </summary>
    /// <returns>The preferences.</returns>
    Task<ClipboardPreferences> GetAsync();

    /// <summary>
    /// Keeps the preferences on the device.
    /// </summary>
    /// <param name="preferences">The preferences.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAsync(ClipboardPreferences preferences);
}
