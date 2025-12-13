namespace clypse.core.Enums;

/// <summary>
/// Represents a specific password derivation algorithm.
/// </summary>
public enum KeyDerivationAlgorithm
{
    /// <summary>
    /// Rfc2898
    /// </summary>
    Rfc2898 = 1,

    /// <summary>
    /// Argon2
    /// </summary>
    Argon2id = 2,
}
