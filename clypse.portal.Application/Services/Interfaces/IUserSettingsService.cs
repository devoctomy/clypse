using clypse.portal.Models.Settings;

namespace clypse.portal.Application.Services.Interfaces;

public interface IUserSettingsService
{
    Task<UserSettings> GetSettingsAsync();
    Task SaveSettingsAsync(UserSettings settings);
    Task<string> GetThemeAsync();
    Task SetThemeAsync(string theme);
}
