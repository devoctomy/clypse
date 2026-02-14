using Microsoft.JSInterop;
using System.Text.Json;
using clypse.portal.Models.Settings;

namespace clypse.portal.Services;

public class UserSettingsService : IUserSettingsService
{
    private const string SETTINGS_KEY = "clypse_user_settings";
    private readonly IJSRuntime _jsRuntime;
    private UserSettings? _cachedSettings;

    public UserSettingsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<UserSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
        {
            return _cachedSettings;
        }

        try
        {
            var settingsJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", SETTINGS_KEY);
            
            if (!string.IsNullOrEmpty(settingsJson))
            {
                _cachedSettings = JsonSerializer.Deserialize<UserSettings>(settingsJson) ?? new UserSettings();
            }
            else
            {
                // Check for legacy theme setting and migrate it
                var legacyTheme = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "clypse_theme");
                _cachedSettings = new UserSettings();
                
                if (!string.IsNullOrEmpty(legacyTheme))
                {
                    _cachedSettings.Theme = legacyTheme;
                    
                    // Save the migrated settings and remove the legacy key
                    await SaveSettingsAsync(_cachedSettings);
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "clypse_theme");
                }
            }
        }
        catch
        {
            _cachedSettings = new UserSettings();
        }

        return _cachedSettings;
    }

    public async Task SaveSettingsAsync(UserSettings settings)
    {
        try
        {
            var settingsJson = JsonSerializer.Serialize(settings);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", SETTINGS_KEY, settingsJson);
            _cachedSettings = settings;
        }
        catch
        {
            // Handle error silently - settings will fall back to defaults
        }
    }

    public async Task<string> GetThemeAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.Theme;
    }

    public async Task SetThemeAsync(string theme)
    {
        var settings = await GetSettingsAsync();
        settings.Theme = theme;
        await SaveSettingsAsync(settings);
    }
}