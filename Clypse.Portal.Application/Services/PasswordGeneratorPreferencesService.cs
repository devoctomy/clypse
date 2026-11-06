using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IPasswordGeneratorPreferencesService"/> in the browser's local storage.
/// </summary>
/// <param name="localStorage">The browser's local storage.</param>
/// <param name="settings">Clypse's settings, with the built in templates.</param>
public class PasswordGeneratorPreferencesService(ILocalStorageService localStorage, ClypseSettings settings)
    : IPasswordGeneratorPreferencesService
{
    /// <summary>
    /// The local storage name the preferences are kept under (the app's prefix is added).
    /// </summary>
    public const string StorageName = "password_generator";

    private readonly ILocalStorageService localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
    private readonly ClypseSettings settings = settings ?? throw new ArgumentNullException(nameof(settings));

    /// <inheritdoc/>
    public async Task<PasswordGeneratorPreferences> GetAsync()
    {
        try
        {
            return await localStorage.GetObjectAsync<PasswordGeneratorPreferences>(StorageName) ?? new PasswordGeneratorPreferences();
        }
        catch (Exception)
        {
            // Unreadable (e.g. from an older build): start again from the defaults.
            return new PasswordGeneratorPreferences();
        }
    }

    /// <inheritdoc/>
    public Task SaveAsync(PasswordGeneratorPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return localStorage.SetObjectAsync(StorageName, preferences);
    }

    /// <inheritdoc/>
    public IReadOnlyList<MemorablePasswordTemplate> GetTemplates(PasswordGeneratorPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var templates = new List<MemorablePasswordTemplate>();
        foreach (var template in settings.MemorablePasswordTemplates.Concat(preferences.UserTemplates))
        {
            if (!string.IsNullOrWhiteSpace(template.Name)
                && !string.IsNullOrWhiteSpace(template.Template)
                && !templates.Any(t => string.Equals(t.Name, template.Name, StringComparison.CurrentCultureIgnoreCase)))
            {
                templates.Add(template);
            }
        }

        return templates;
    }
}
