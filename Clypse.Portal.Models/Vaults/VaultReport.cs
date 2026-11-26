namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// What a vault's report found among its secrets: weak, reused and old passwords, and reused user names. It names
/// secrets by id only, and holds no passwords.
/// </summary>
public class VaultReport
{
    /// <summary>
    /// Gets the report of no vault.
    /// </summary>
    public static VaultReport Empty { get; } = new VaultReport();

    /// <summary>
    /// Gets the id of the vault reported on, or null for no vault.
    /// </summary>
    public string? VaultId { get; init; }

    /// <summary>
    /// Gets when the report was made, or null if it never was.
    /// </summary>
    public DateTimeOffset? GeneratedAt { get; init; }

    /// <summary>
    /// Gets how many secrets were looked at.
    /// </summary>
    public int SecretCount { get; init; }

    /// <summary>
    /// Gets after how many months without being updated a password was counted as old.
    /// </summary>
    public int OldPasswordMonths { get; init; }

    /// <summary>
    /// Gets the secrets whose password is weak, or in the list of weak known passwords.
    /// </summary>
    public IReadOnlyList<VaultReportWeakPassword> WeakPasswords { get; init; } = [];

    /// <summary>
    /// Gets the user names used by more than one secret, each as the secrets using it. User names that are email
    /// addresses are left out.
    /// </summary>
    public IReadOnlyList<VaultReportReuse> ReusedUserNames { get; init; } = [];

    /// <summary>
    /// Gets the passwords used by more than one secret, each as the secrets using it.
    /// </summary>
    public IReadOnlyList<VaultReportReuse> ReusedPasswords { get; init; } = [];

    /// <summary>
    /// Gets the secrets whose password has not been updated for <see cref="OldPasswordMonths"/> months.
    /// </summary>
    public IReadOnlyList<VaultReportOldPassword> OldPasswords { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the report found anything.
    /// </summary>
    public bool HasIssues => WeakPasswords.Count > 0 || ReusedUserNames.Count > 0 || ReusedPasswords.Count > 0 || OldPasswords.Count > 0;
}
