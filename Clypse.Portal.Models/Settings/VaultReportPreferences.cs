namespace Clypse.Portal.Models.Settings;

/// <summary>
/// How a vault's report judges its secrets: kept on the device.
/// </summary>
public class VaultReportPreferences
{
    /// <summary>
    /// The shortest age that can be chosen, in months.
    /// </summary>
    public const int MinimumMonths = 1;

    /// <summary>
    /// Gets or sets after how many months without being updated a password is reported as old.
    /// </summary>
    public int OldPasswordMonths { get; set; } = 3;
}
