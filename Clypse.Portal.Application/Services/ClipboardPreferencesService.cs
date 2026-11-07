using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IClipboardPreferencesService"/> over local storage, read once and then kept, so a
/// copy does not wait on storage first. Until the user chooses, a secret value is cleared after
/// <see cref="ClypseSettings.ClipboardClearSeconds"/>.
/// </summary>
/// <param name="localStorage">The local storage.</param>
/// <param name="settings">The app's settings, for the default time.</param>
public class ClipboardPreferencesService(ILocalStorageService localStorage, ClypseSettings settings)
    : IClipboardPreferencesService
{
    /// <summary>
    /// The local storage name.
    /// </summary>
    public const string StorageName = "clipboard";

    private readonly ILocalStorageService localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
    private readonly ClypseSettings settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private ClipboardPreferences? current;

    /// <inheritdoc/>
    public async Task<ClipboardPreferences> GetAsync()
    {
        if (current == null)
        {
            ClipboardPreferences? kept;
            try
            {
                kept = await localStorage.GetObjectAsync<ClipboardPreferences>(StorageName);
            }
            catch (Exception)
            {
                // Unreadable: start again from the defaults.
                kept = null;
            }

            current = kept ?? new ClipboardPreferences { ClearAfterSeconds = settings.ClipboardClearSeconds };
            current.ClearAfterSeconds = Math.Max(ClipboardPreferences.MinimumSeconds, current.ClearAfterSeconds);
        }

        return Copy(current);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(ClipboardPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var saved = Copy(preferences);
        saved.ClearAfterSeconds = Math.Max(ClipboardPreferences.MinimumSeconds, saved.ClearAfterSeconds);
        await localStorage.SetObjectAsync(StorageName, saved);
        current = saved;
    }

    private static ClipboardPreferences Copy(ClipboardPreferences preferences) => new ClipboardPreferences
    {
        ClearSecrets = preferences.ClearSecrets,
        ClearAfterSeconds = preferences.ClearAfterSeconds,
    };
}
