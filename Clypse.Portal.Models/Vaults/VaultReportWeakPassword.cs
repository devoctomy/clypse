namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// A secret whose password is weak, in a <see cref="VaultReport"/>.
/// </summary>
public class VaultReportWeakPassword
{
    /// <summary>
    /// Gets the secret's id.
    /// </summary>
    public string SecretId { get; init; } = string.Empty;

    /// <summary>
    /// Gets how strong the password is.
    /// </summary>
    public PasswordStrength Strength { get; init; } = PasswordStrength.None;
}
