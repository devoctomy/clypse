namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// A secret whose password has not been changed for a while, in a <see cref="VaultReport"/>.
/// </summary>
public class VaultReportOldPassword
{
    /// <summary>
    /// Gets the secret's id.
    /// </summary>
    public string SecretId { get; init; } = string.Empty;

    /// <summary>
    /// Gets when the password was last set or changed, in UTC: for a secret saved before that was recorded, when the
    /// secret was last updated.
    /// </summary>
    public DateTime PasswordUpdatedAt { get; init; }
}
