namespace Clypse.Portal.Models.Settings;

/// <summary>
/// Whether, and when, a copied password or other secret value is cleared from the clipboard: kept on the device.
/// </summary>
public class ClipboardPreferences
{
    /// <summary>
    /// The shortest time that can be chosen, in seconds.
    /// </summary>
    public const int MinimumSeconds = 5;

    /// <summary>
    /// Gets or sets a value indicating whether a copied secret value is cleared from the clipboard.
    /// </summary>
    public bool ClearSecrets { get; set; } = true;

    /// <summary>
    /// Gets or sets how long, in seconds, a copied secret value stays on the clipboard before it is cleared.
    /// </summary>
    public int ClearAfterSeconds { get; set; } = 30;
}
