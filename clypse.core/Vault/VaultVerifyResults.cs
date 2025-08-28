namespace clypse.core.Vault;

/// <summary>
/// Contains the results of a vault verification operation, including integrity check results and any inconsistencies found.
/// </summary>
public class VaultVerifyResults
{
    /// <summary>
    /// Gets or sets a value indicating whether the verification was successful (no integrity issues found).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of secrets that are referenced in the index but missing from storage.
    /// </summary>
    public int MissingSecrets { get; set; }

    /// <summary>
    /// Gets or sets the number of secrets where the metadata in the index doesn't match the actual secret data.
    /// </summary>
    public int MismatchedSecrets { get; set; }

    /// <summary>
    /// Gets or sets the list of secret IDs that exist in storage but are not referenced in the index.
    /// </summary>
    public List<string> UnindexedSecrets { get; set; } = [];
}
