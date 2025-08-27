using System.Text.Json.Serialization;

namespace clypse.core.Base;

public class ClypseObject
{
    public ClypseObject()
    {
        this.Id = Guid.NewGuid().ToString();
        this.CreatedAt = DateTime.UtcNow;
        this.LastUpdatedAt = DateTime.UtcNow;
    }

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

    public Dictionary<string, string> Data { get; set; } = [];

    public string? GetData(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        if (this.Data.TryGetValue(key, out var value))
        {
            return value;
        }

        return null;
    }

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

    public void SetAllData(Dictionary<string, string> data)
    {
        this.Data = data;
    }
}
