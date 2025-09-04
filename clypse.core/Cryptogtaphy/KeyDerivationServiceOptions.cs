namespace clypse.core.Cryptogtaphy;

/// <summary>
/// Represents configuration options for a key derivation service, including a collection of parameters that can be used
/// to customize its behavior.
/// </summary>
public class KeyDerivationServiceOptions
{
    /// <summary>
    /// Gets a dictionary of parameters for the key derivation service.
    /// </summary>
    public Dictionary<string, object> Parameters { get; } = [];

    /// <summary>
    /// Get a parameter by key as a string.
    /// </summary>
    /// <param name="key">Key of the parameter to get.</param>
    /// <returns>Parameter as a string.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the key is not found in the Parameters dictionary.</exception>"
    public string GetAsString(string key)
    {
        if (this.Parameters.TryGetValue(key, out var value))
        {
            return (string)value;
        }

        throw new KeyNotFoundException($"Key '{key}' not found in Parameters.");
    }

    /// <summary>
    /// Get a parameter by key as a int.
    /// </summary>
    /// <param name="key">Key of the parameter to get.</param>
    /// <returns>Parameter as a int.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the key is not found in the Parameters dictionary.</exception>"
    public int GetAsInt(string key)
    {
        if (this.Parameters.TryGetValue(key, out var value))
        {
            return (int)value;
        }

        throw new KeyNotFoundException($"Key '{key}' not found in Parameters.");
    }
}
