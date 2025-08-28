using System.Text.Json.Serialization;
using clypse.core.Base;
using clypse.core.Enums;

namespace clypse.core.Secrets;

/// <summary>
/// Generic secret which other secrets may be derived from.
/// </summary>
public class Secret : ClypseObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Secret"/> class.
    /// </summary>
    public Secret()
    {
        this.SecretType = SecretType.None;
    }

    /// <summary>
    /// Gets or Sets SecretType for this secret.
    /// </summary>
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

    /// <summary>
    /// Gets or sets Name for this secret.
    /// </summary>
    [RequiredData]
    [JsonIgnore]
    public string? Name
    {
        get { return this.GetData(nameof(this.Name)); }
        set { this.SetData(nameof(this.Name), value); }
    }

    /// <summary>
    /// Gets or sets Description of this secret.
    /// </summary>
    [JsonIgnore]
    public string? Description
    {
        get { return this.GetData(nameof(this.Description)); }
        set { this.SetData(nameof(this.Description), value); }
    }

    /// <summary>
    /// Gets list of Tags for this secret.
    /// </summary>
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

    /// <summary>
    /// Add a tag to this secret.
    /// </summary>
    /// <param name="tag">Tag to add.</param>
    /// <returns>True when successfully added.</returns>
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

    /// <summary>
    /// Clear all tags for this secret.
    /// </summary>
    public void ClearTags()
    {
        this.UpdateTags([]);
    }

    /// <summary>
    /// Update all tags for this secret, replacing all existing tags.
    /// </summary>
    /// <param name="tags">Tags to update this secret with.</param>
    public void UpdateTags(List<string> tags)
    {
        var tagsCsv = string.Join(',', tags);
        this.SetData(nameof(this.Tags), tagsCsv);
    }
}
