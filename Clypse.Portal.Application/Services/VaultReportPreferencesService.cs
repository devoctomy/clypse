using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IVaultReportPreferencesService"/> over local storage, read once and then kept. Until
/// the user chooses, a password is old after <see cref="ClypseSettings.OldPasswordMonths"/>.
/// </summary>
/// <param name="localStorage">The local storage.</param>
/// <param name="settings">The app's settings, for the default age.</param>
public class VaultReportPreferencesService(ILocalStorageService localStorage, ClypseSettings settings)
    : IVaultReportPreferencesService
{
    /// <summary>
    /// The local storage name.
    /// </summary>
    public const string StorageName = "vaultReport";

    private readonly ILocalStorageService localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
    private readonly ClypseSettings settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private VaultReportPreferences? current;

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public async Task<VaultReportPreferences> GetAsync()
    {
        if (current == null)
        {
            VaultReportPreferences? kept;
            try
            {
                kept = await localStorage.GetObjectAsync<VaultReportPreferences>(StorageName);
            }
            catch (Exception)
            {
                // Unreadable: start again from the defaults.
                kept = null;
            }

            current = kept ?? new VaultReportPreferences { OldPasswordMonths = settings.OldPasswordMonths };
            current.OldPasswordMonths = Math.Max(VaultReportPreferences.MinimumMonths, current.OldPasswordMonths);
        }

        return Copy(current);
    }

    /// <inheritdoc/>
    public async Task SaveAsync(VaultReportPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var saved = Copy(preferences);
        saved.OldPasswordMonths = Math.Max(VaultReportPreferences.MinimumMonths, saved.OldPasswordMonths);
        await localStorage.SetObjectAsync(StorageName, saved);
        current = saved;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static VaultReportPreferences Copy(VaultReportPreferences preferences) => new VaultReportPreferences
    {
        OldPasswordMonths = preferences.OldPasswordMonths,
    };
}
