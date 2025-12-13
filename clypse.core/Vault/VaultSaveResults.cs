namespace clypse.core.Vault;

/// <summary>
/// Contains the results of a vault save operation, including success status and counts of secrets processed.
/// </summary>
public class VaultSaveResults
{
    /// <summary>
    /// Gets or sets a value indicating whether the save operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of new secrets that were created during the save operation.
    /// </summary>
    public int SecretsCreated { get; set; }

    /// <summary>
    /// Gets or sets the number of existing secrets that were updated during the save operation.
    /// </summary>
    public int SecretsUpdated { get; set; }

    /// <summary>
    /// Gets or sets the number of secrets that were deleted during the save operation.
    /// </summary>
    public int SecretsDeleted { get; set; }
}
