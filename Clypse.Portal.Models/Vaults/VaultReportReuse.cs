namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// A user name or password used by more than one secret, in a <see cref="VaultReport"/>. The value itself is not held:
/// read it from any of the secrets.
/// </summary>
public class VaultReportReuse
{
    /// <summary>
    /// Gets the ids of the secrets using it: two or more.
    /// </summary>
    public IReadOnlyList<string> SecretIds { get; init; } = [];
}
