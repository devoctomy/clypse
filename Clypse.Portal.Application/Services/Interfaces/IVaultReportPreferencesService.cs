using Clypse.Portal.Models.Settings;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Keeps on the device how a vault's report judges its secrets, e.g. when a password counts as old.
/// </summary>
public interface IVaultReportPreferencesService
{
    /// <summary>
    /// Raised when the preferences are saved.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Gets the preferences: those kept on the device, or the app's defaults.
    /// </summary>
    /// <returns>The preferences.</returns>
    Task<VaultReportPreferences> GetAsync();

    /// <summary>
    /// Keeps the preferences on the device.
    /// </summary>
    /// <param name="preferences">The preferences.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAsync(VaultReportPreferences preferences);
}
