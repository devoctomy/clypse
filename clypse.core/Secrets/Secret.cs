using clypse.core.Base;
using clypse.core.Enums;
using System.Text.Json.Serialization;

namespace clypse.core.Secrets;

public class Secret : ClypseObject
{
    [RequiredData]
    [JsonIgnore]
    public SecretType SecretType
    {
        get 
        {
            var secretType = GetData(nameof(SecretType));
            return Enum.Parse<SecretType>(secretType!, true);
        }
        set { SetData(nameof(SecretType), value.ToString()); }
    }

    [RequiredData]
    [JsonIgnore]
    public string? Name
    {
        get { return GetData(nameof(Name)); }
        set { SetData(nameof(Name), value); }
    }

    [JsonIgnore]
    public string? Description
    {
        get { return GetData(nameof(Description)); }
        set { SetData(nameof(Description), value); }
    }

    [JsonIgnore]
    public List<string> Tags
    {
        get
        {
            var tags = GetData(nameof(Tags));
            if(string.IsNullOrEmpty(tags))
            {
                return new List<string>();
            }

            return [.. tags.Split(',')];
        }
    }

    public Secret()
    {
        SecretType = SecretType.None;
    }

    public bool AddTag(string tag)
    {
        var tags = Tags;
        if(tags.Contains(tag))
        {
            return false;
        }

        tags.Add(tag);
        UpdateTags(tags);
        return true;
    }

    public void ClearTags()
    {
        UpdateTags(new List<string>());
    }

    private void UpdateTags(List<string> tags)
    {
        var tagsCsv = string.Join(',', tags);
        SetData(nameof(Tags), tagsCsv);
    }
}
