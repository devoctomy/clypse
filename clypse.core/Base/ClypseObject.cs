using System.Text.Json.Serialization;

namespace clypse.core.Base;

public class ClypseObject
{
    [RequiredData]
    [JsonIgnore]
    public string Id
    {
        get { return GetData(nameof(Id))!; }
        set { SetData(nameof(Id), value); }
    }

    [RequiredData]
    [JsonIgnore]
    public DateTime CreatedAt
    {
        get
        {
            var value = GetData(nameof(CreatedAt));
            return DateTime.ParseExact(value!, "dd-MM-yyyyTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }
        set { SetData(nameof(CreatedAt), value.ToString("dd-MM-yyyyTHH:mm:ss")); }
    }

    [RequiredData]
    [JsonIgnore]
    public DateTime LastUpdatedAt
    {
        get
        {
            var value = GetData(nameof(LastUpdatedAt));
            return DateTime.ParseExact(value!, "dd-MM-yyyyTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }
        set { SetData(nameof(LastUpdatedAt), value.ToString("dd-MM-yyyyTHH:mm:ss")); }
    }

    public Dictionary<string, string> Data { get; set; } = [];

    public ClypseObject()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public string? GetData(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        if (Data.TryGetValue(key, out var value))
        {
            return value;
        }

        return null;
    }

    public void SetData(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        if(string.IsNullOrEmpty(value) &&
            Data.ContainsKey(key))
        {
            Data.Remove(key);
        }
        else
        {
            Data[key] = value!;
        }       
    }
}
