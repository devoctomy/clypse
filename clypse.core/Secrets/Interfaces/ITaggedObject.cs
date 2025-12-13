namespace clypse.core.Secrets.Interfaces;

/// <summary>
/// Interface for objects that can be tagged.
/// </summary>
public interface ITaggedObject
{
    /// <summary>
    /// Add a tag to the object.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    /// <returns>True if the tag was successfully added; otherwise, false.</returns>
    public bool AddTag(string tag);

    /// <summary>
    /// Removes a tag from the object.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    /// <returns>True if the tag was successfully removed; otherwise, false.</returns>
    public bool RemoveTag(string tag);

    /// <summary>
    /// Clears all tags from the object.
    /// </summary>
    public void ClearTags();

    /// <summary>
    /// Updates the tags associated with the object.
    /// </summary>
    /// <param name="tags">The new list of tags.</param>
    public void UpdateTags(List<string> tags);
}
