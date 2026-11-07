namespace Clypse.Portal.Models.Settings;

/// <summary>
/// Clypse's settings: the <c>Clypse</c> section of <c>wwwroot/appsettings.json</c>.
/// </summary>
public class ClypseSettings
{
    /// <summary>
    /// The name of the configuration section.
    /// </summary>
    public const string SectionName = "Clypse";

    /// <summary>
    /// Gets or sets the templates the password generator offers for memorable passwords.
    /// </summary>
    public List<MemorablePasswordTemplate> MemorablePasswordTemplates { get; set; } = [];

    /// <summary>
    /// Gets or sets how long, in seconds, a copied password or other secret value stays on the clipboard before it is
    /// cleared, until the user chooses otherwise in Settings (<see cref="ClipboardPreferences"/>).
    /// </summary>
    public int ClipboardClearSeconds { get; set; } = 30;
}
