using clypse.portal.Models;

namespace clypse.portal.Services;

public interface IUserSettingsService
{
    Task<UserSettings> GetSettingsAsync();
    Task SaveSettingsAsync(UserSettings settings);
    Task<string> GetThemeAsync();
    Task SetThemeAsync(string theme);
}
