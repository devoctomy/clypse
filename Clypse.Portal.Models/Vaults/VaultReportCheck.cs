namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// A check a vault's report makes, in the order the report shows them.
/// </summary>
public enum VaultReportCheck
{
    /// <summary>
    /// Passwords in the list of weak known passwords.
    /// </summary>
    KnownWeakPasswords,

    /// <summary>
    /// Other weak passwords.
    /// </summary>
    WeakPasswords,

    /// <summary>
    /// Passwords used by more than one secret.
    /// </summary>
    ReusedPasswords,

    /// <summary>
    /// User names, other than email addresses, used by more than one secret.
    /// </summary>
    ReusedUserNames,

    /// <summary>
    /// Passwords not changed for a while.
    /// </summary>
    OldPasswords,
}
