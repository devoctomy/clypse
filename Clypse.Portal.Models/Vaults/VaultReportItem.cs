namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// Something a check of a vault's report found: one secret, or the secrets sharing a password or user name.
/// </summary>
public class VaultReportItem
{
    /// <summary>
    /// Gets the item's heading, e.g. the reused user name or "Used by 3 secrets"; null when the item is one secret.
    /// </summary>
    public string? Heading { get; init; }

    /// <summary>
    /// Gets more about the item, e.g. how weak the password is or when it was last changed; null if nothing.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>
    /// Gets the secrets, to open from the report.
    /// </summary>
    public IReadOnlyList<VaultReportSecret> Secrets { get; init; } = [];
}
