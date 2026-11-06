namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// What the user calls a vault: kept, encrypted like its secrets, in the <c>.vault</c> file at its root.
/// </summary>
public class VaultDetails
{
    /// <summary>
    /// Gets or sets the vault's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vault's description.
    /// </summary>
    public string? Description { get; set; }
}
