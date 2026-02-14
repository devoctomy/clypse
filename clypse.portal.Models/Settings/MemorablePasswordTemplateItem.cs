namespace clypse.portal.Models.Settings;

/// <summary>
/// Represents a template for generating memorable passwords.
/// </summary>
public class MemorablePasswordTemplateItem
{
    /// <summary>
    /// Gets or sets the name of the template.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template pattern for password generation.
    /// </summary>
    public string Template { get; set; } = string.Empty;
}
