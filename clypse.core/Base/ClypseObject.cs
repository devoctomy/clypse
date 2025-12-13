using System.Text.Json.Serialization;

namespace clypse.core.Base;

/// <summary>
/// Base class for all Clypse objects, providing common functionality for object management including unique identification, timestamps, and data storage.
/// </summary>
public class ClypseObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClypseObject"/> class with a unique identifier and current timestamps.
    /// </summary>
    public ClypseObject()
    {
        this.Id = Guid.NewGuid().ToString();
        this.CreatedAt = DateTime.UtcNow;
        this.LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets or sets the unique identifier for this object.
    /// </summary>
    [RequiredData]
    [JsonIgnore]
    public string Id
    {
        get
        {
            return this.GetData(nameof(this.Id)) !;
        }

        set
        {
            this.SetData(nameof(this.Id), value);
        }
    }

    /// <summary>
    /// Gets or sets the timestamp when this object was created.
    /// </summary>
    [RequiredData]
    [JsonIgnore]
    public DateTime CreatedAt
    {
        get
        {
            var value = this.GetData(nameof(this.CreatedAt));
            return DateTime.ParseExact(value!, "dd-MM-yyyyTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        set
        {
            this.SetData(nameof(this.CreatedAt), value.ToString("dd-MM-yyyyTHH:mm:ss"));
        }
    }

    /// <summary>
    /// Gets or sets the timestamp when this object was last updated.
    /// </summary>
    [RequiredData]
    [JsonIgnore]
    public DateTime LastUpdatedAt
    {
        get
        {
            var value = this.GetData(nameof(this.LastUpdatedAt));
            return DateTime.ParseExact(value!, "dd-MM-yyyyTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        set
        {
            this.SetData(nameof(this.LastUpdatedAt), value.ToString("dd-MM-yyyyTHH:mm:ss"));
        }
    }

    /// <summary>
    /// Gets or sets the dictionary containing all data for this object.
    /// </summary>
    public Dictionary<string, string> Data { get; set; } = [];

    /// <summary>
    /// Retrieves data value for the specified key.
    /// </summary>
    /// <param name="key">The key to retrieve data for.</param>
    /// <returns>The data value if found; otherwise, null.</returns>
    public string? GetData(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        if (this.Data.TryGetValue(key, out var value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// Sets or updates data value for the specified key. If value is null or empty, the key is removed.
    /// </summary>
    /// <param name="key">The key to set data for.</param>
    /// <param name="value">The value to set. If null or empty, the key will be removed.</param>
    public void SetData(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        if (string.IsNullOrEmpty(value) &&
            this.Data.ContainsKey(key))
        {
            this.Data.Remove(key);
        }
        else
        {
            this.Data[key] = value!;
        }
    }

    /// <summary>
    /// Replaces all data in this object with the provided dictionary.
    /// </summary>
    /// <param name="data">The new data dictionary to set.</param>
    public void SetAllData(Dictionary<string, string> data)
    {
        this.Data = data;
    }
}
