namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// A secret named in a vault's report.
/// </summary>
public class VaultReportSecret
{
    /// <summary>
    /// Gets the secret's id.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets the secret's name.
    /// </summary>
    public string Name { get; init; } = string.Empty;
}
