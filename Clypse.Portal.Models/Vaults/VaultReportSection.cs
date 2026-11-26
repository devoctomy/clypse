namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// One check of a vault's report, worded for the report dialog.
/// </summary>
public class VaultReportSection
{
    /// <summary>
    /// Gets the check.
    /// </summary>
    public VaultReportCheck Check { get; init; }

    /// <summary>
    /// Gets the check's title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Gets what the check found, e.g. "No weak passwords found.".
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// Gets what the check looks for, and why it matters.
    /// </summary>
    public string About { get; init; } = string.Empty;

    /// <summary>
    /// Gets what the check found, one item per secret or per reused value: empty if it passed.
    /// </summary>
    public IReadOnlyList<VaultReportItem> Items { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the check passed.
    /// </summary>
    public bool Passed => Items.Count == 0;
}
