namespace Clypse.Portal.Models.Settings;

/// <summary>
/// A template for memorable passwords, e.g. <c>{dict(adjective):random}{randstr(0123456789,2)}{dict(noun):random}</c>.
/// </summary>
public class MemorablePasswordTemplate
{
    /// <summary>
    /// Gets or sets the name the generator shows it by.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template.
    /// </summary>
    public string Template { get; set; } = string.Empty;
}
