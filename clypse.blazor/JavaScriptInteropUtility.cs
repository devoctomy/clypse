using System.Text.Json;

namespace clypse.blazor;

/// <summary>
/// Utility class for safely extracting values from JavaScript interop dictionaries that may contain JsonElement objects.
/// Provides type-safe extraction methods for common data types when dealing with JSON deserialization from JavaScript calls.
/// </summary>
public static class JavaScriptInteropUtility
{
    /// <summary>
    /// Safely extracts a long value from a dictionary that may contain JsonElement objects.
    /// </summary>
    /// <param name="dictionary">The dictionary to extract from.</param>
    /// <param name="key">The key to look for.</param>
    /// <param name="defaultValue">The default value if extraction fails.</param>
    /// <returns>The extracted long value or the default value.</returns>
    public static long GetLongValue(Dictionary<string, object?>? dictionary, string key, long defaultValue = 0L)
    {
        var value = dictionary?.GetValueOrDefault(key, defaultValue);
        if (value == null) return defaultValue;

        // Handle JsonElement case
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Number => jsonElement.GetInt64(),
                JsonValueKind.String when long.TryParse(jsonElement.GetString(), out var parsed) => parsed,
                _ => defaultValue
            };
        }

        // Handle direct numeric types
        return value switch
        {
            long longVal => longVal,
            int intVal => intVal,
            double doubleVal => (long)doubleVal,
            float floatVal => (long)floatVal,
            string strVal when long.TryParse(strVal, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Safely extracts an int value from a dictionary that may contain JsonElement objects.
    /// </summary>
    /// <param name="dictionary">The dictionary to extract from.</param>
    /// <param name="key">The key to look for.</param>
    /// <param name="defaultValue">The default value if extraction fails.</param>
    /// <returns>The extracted int value or the default value.</returns>
    public static int GetIntValue(Dictionary<string, object?>? dictionary, string key, int defaultValue = 0)
    {
        var value = dictionary?.GetValueOrDefault(key, defaultValue);
        if (value == null) return defaultValue;

        // Handle JsonElement case
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Number => jsonElement.GetInt32(),
                JsonValueKind.String when int.TryParse(jsonElement.GetString(), out var parsed) => parsed,
                _ => defaultValue
            };
        }

        // Handle direct numeric types
        return value switch
        {
            int intVal => intVal,
            long longVal => (int)longVal,
            double doubleVal => (int)doubleVal,
            float floatVal => (int)floatVal,
            string strVal when int.TryParse(strVal, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Safely extracts a bool value from a dictionary that may contain JsonElement objects.
    /// </summary>
    /// <param name="dictionary">The dictionary to extract from.</param>
    /// <param name="key">The key to look for.</param>
    /// <param name="defaultValue">The default value if extraction fails.</param>
    /// <returns>The extracted bool value or the default value.</returns>
    public static bool GetBoolValue(Dictionary<string, object?>? dictionary, string key, bool defaultValue = false)
    {
        var value = dictionary?.GetValueOrDefault(key, defaultValue);
        if (value == null) return defaultValue;

        // Handle JsonElement case
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(jsonElement.GetString(), out var parsed) => parsed,
                _ => defaultValue
            };
        }

        // Handle direct types
        return value switch
        {
            bool boolVal => boolVal,
            string strVal when bool.TryParse(strVal, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Safely extracts a string value from a dictionary that may contain JsonElement objects.
    /// </summary>
    /// <param name="dictionary">The dictionary to extract from.</param>
    /// <param name="key">The key to look for.</param>
    /// <param name="defaultValue">The default value if extraction fails.</param>
    /// <returns>The extracted string value or the default value.</returns>
    public static string? GetStringValue(Dictionary<string, object?>? dictionary, string key, string? defaultValue = null)
    {
        var value = dictionary?.GetValueOrDefault(key, defaultValue);
        if (value == null) return defaultValue;

        // Handle JsonElement case
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => defaultValue,
                _ => defaultValue
            };
        }

        // Handle direct types
        return value?.ToString() ?? defaultValue;
    }
}
