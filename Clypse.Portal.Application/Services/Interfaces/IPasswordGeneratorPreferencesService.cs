using Clypse.Portal.Models.Settings;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Keeps the password generator's preferences on this device.
/// </summary>
public interface IPasswordGeneratorPreferencesService
{
    /// <summary>
    /// Gets the preferences, or the defaults if none were saved.
    /// </summary>
    /// <returns>The preferences.</returns>
    Task<PasswordGeneratorPreferences> GetAsync();

    /// <summary>
    /// Saves the preferences.
    /// </summary>
    /// <param name="preferences">The preferences.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveAsync(PasswordGeneratorPreferences preferences);

    /// <summary>
    /// Gets every memorable template: the built in ones (<see cref="ClypseSettings.MemorablePasswordTemplates"/>), then
    /// the user's own.
    /// </summary>
    /// <param name="preferences">The preferences, with the user's own templates.</param>
    /// <returns>The templates that have a template, in order, each name once.</returns>
    IReadOnlyList<MemorablePasswordTemplate> GetTemplates(PasswordGeneratorPreferences preferences);
}
