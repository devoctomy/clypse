namespace clypse.portal.Application.Services.Interfaces;

/// <summary>
/// Provides access to browser local storage functionality.
/// </summary>
public interface ILocalStorageService
{
    /// <summary>
    /// Retrieves a value from local storage by its key.
    /// </summary>
    /// <param name="key">The key of the item to retrieve.</param>
    /// <returns>The value stored at the specified key, or null if the key does not exist.</returns>
    Task<string?> GetItemAsync(string key);

    /// <summary>
    /// Stores a value in local storage with the specified key.
    /// </summary>
    /// <param name="key">The key under which to store the value.</param>
    /// <param name="value">The value to store.</param>
    Task SetItemAsync(string key, string value);

    /// <summary>
    /// Removes an item from local storage by its key.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    Task RemoveItemAsync(string key);

    /// <summary>
    /// Clears all items from local storage except for persistent settings that should be preserved across sessions.
    /// </summary>
    Task ClearAllExceptPersistentSettingsAsync();
}