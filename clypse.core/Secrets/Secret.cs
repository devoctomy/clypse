using System.Text.Json.Serialization;
using clypse.core.Base;
using clypse.core.Enums;

namespace clypse.core.Secrets;

public class Secret : ClypseObject
{
    [RequiredData]
    [JsonIgnore]
    public SecretType SecretType
    {
        get
        {
            var secretType = this.GetData(nameof(this.SecretType));
            return Enum.Parse<SecretType>(secretType!, true);
        }

        set
        {
            this.SetData(nameof(this.SecretType), value.ToString());
        }
    }

    [RequiredData]
    [JsonIgnore]
    public string? Name
    {
        get { return this.GetData(nameof(this.Name)); }
        set { this.SetData(nameof(this.Name), value); }
    }

    [JsonIgnore]
    public string? Description
    {
        get { return this.GetData(nameof(this.Description)); }
        set { this.SetData(nameof(this.Description), value); }
    }

    [JsonIgnore]
    public List<string> Tags
    {
        get
        {
            var tags = this.GetData(nameof(this.Tags));
            if (string.IsNullOrEmpty(tags))
            {
                return [];
            }

            return [.. tags.Split(',')];
        }
    }

    public Secret()
    {
        this.SecretType = SecretType.None;
    }

    public bool AddTag(string tag)
    {
        var tags = this.Tags;
        if (tags.Contains(tag))
        {
            return false;
        }

        tags.Add(tag);
        this.UpdateTags(tags);
        return true;
    }

    public void ClearTags()
    {
        this.UpdateTags([]);
    }

    public void UpdateTags(List<string> tags)
    {
        var tagsCsv = string.Join(',', tags);
        this.SetData(nameof(this.Tags), tagsCsv);
    }
}
