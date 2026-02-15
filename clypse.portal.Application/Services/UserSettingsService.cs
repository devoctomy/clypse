using System.Text.Json;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.Settings;
using Microsoft.JSInterop;

namespace clypse.portal.Application.Services;

/// <inheritdoc/>
public class UserSettingsService(IJSRuntime jsRuntime)
    : IUserSettingsService
{
    private const string SettingsLocalStorageKey = "clypse_user_settings";
    private readonly IJSRuntime jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private UserSettings? cachedSettings;

    /// <inheritdoc/>
    public async Task<UserSettings> GetSettingsAsync()
    {
        if (this.cachedSettings != null)
        {
            return this.cachedSettings;
        }

        try
        {
            var settingsJson = await this.jsRuntime.InvokeAsync<string?>("localStorage.getItem", SettingsLocalStorageKey);

            if (!string.IsNullOrEmpty(settingsJson))
            {
                this.cachedSettings = JsonSerializer.Deserialize<UserSettings>(settingsJson) ?? new UserSettings();
            }
            else
            {
                // Check for legacy theme setting and migrate it
                var legacyTheme = await this.jsRuntime.InvokeAsync<string?>("localStorage.getItem", "clypse_theme");
                this.cachedSettings = new UserSettings();

                if (!string.IsNullOrEmpty(legacyTheme))
                {
                    this.cachedSettings.Theme = legacyTheme;

                    // Save the migrated settings and remove the legacy key
                    await this.SaveSettingsAsync(this.cachedSettings);
                    await this.jsRuntime.InvokeVoidAsync("localStorage.removeItem", "clypse_theme");
                }
            }
        }
        catch
        {
            this.cachedSettings = new UserSettings();
        }

        return this.cachedSettings;
    }

    /// <inheritdoc/>
    public async Task SaveSettingsAsync(UserSettings settings)
    {
        try
        {
            var settingsJson = JsonSerializer.Serialize(settings);
            await this.jsRuntime.InvokeVoidAsync("localStorage.setItem", SettingsLocalStorageKey, settingsJson);
            this.cachedSettings = settings;
        }
        catch
        {
            // Handle error silently - settings will fall back to defaults
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetThemeAsync()
    {
        var settings = await this.GetSettingsAsync();
        return settings.Theme;
    }

    /// <inheritdoc/>
    public async Task SetThemeAsync(string theme)
    {
        var settings = await this.GetSettingsAsync();
        settings.Theme = theme;
        await this.SaveSettingsAsync(settings);
    }
}