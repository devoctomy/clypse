using Microsoft.JSInterop;

namespace clypse.portal.Services;

public interface ILocalStorageService
{
    Task<string?> GetItemAsync(string key);
    Task SetItemAsync(string key, string value);
    Task RemoveItemAsync(string key);
    Task ClearAllExceptPersistentSettingsAsync();
}

public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    
    private static readonly string[] PersistentStorageKeys = { "users", "clypse_user_settings" };

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetItemAsync(string key)
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
    }

    public async Task SetItemAsync(string key, string value)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    public async Task RemoveItemAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

    public async Task ClearAllExceptPersistentSettingsAsync()
    {
        // Clear all localStorage data except persistent user settings and saved users
        var persistentKeysJson = System.Text.Json.JsonSerializer.Serialize(PersistentStorageKeys);
        await _jsRuntime.InvokeVoidAsync("eval", $@"
            const persistentKeys = {persistentKeysJson};
            const keysToRemove = [];
            for (let i = 0; i < localStorage.length; i++) {{
                const key = localStorage.key(i);
                if (!persistentKeys.includes(key)) {{
                    keysToRemove.push(key);
                }}
            }}
            keysToRemove.forEach(key => localStorage.removeItem(key));
        ");
    }
}