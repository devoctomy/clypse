using clypse.portal.Application.Services.Interfaces;
using Microsoft.JSInterop;

namespace clypse.portal.Application.Services;

/// <inheritdoc/>
public class LocalStorageService(IJSRuntime jsRuntime)
    : ILocalStorageService
{
    private static readonly string[] PersistentLocalStorageKeys = { "users", "clypse_user_settings" };
    private readonly IJSRuntime jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    /// <inheritdoc/>
    public async Task<string?> GetItemAsync(string key)
    {
        return await this.jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
    }

    /// <inheritdoc/>
    public async Task SetItemAsync(string key, string value)
    {
        await this.jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    /// <inheritdoc/>
    public async Task RemoveItemAsync(string key)
    {
        await this.jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

    /// <inheritdoc/>
    public async Task ClearAllExceptPersistentSettingsAsync()
    {
        // Clear all localStorage data except persistent user settings and saved users
        var persistentKeysJson = System.Text.Json.JsonSerializer.Serialize(PersistentLocalStorageKeys);
        var clearStorageScript = $@"
            const persistentKeys = {persistentKeysJson};
            const keysToRemove = [];
            for (let i = 0; i < localStorage.length; i++) {{
                const key = localStorage.key(i);
                if (!persistentKeys.includes(key)) {{
                    keysToRemove.push(key);
                }}
            }}
            keysToRemove.forEach(key => localStorage.removeItem(key));";
        await this.jsRuntime.InvokeVoidAsync("eval", clearStorageScript);
    }
}
