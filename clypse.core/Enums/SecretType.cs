namespace clypse.core.Enums;

/// <summary>
/// Represents the different types of secrets that can be stored in the system.
/// </summary>
public enum SecretType
{
    /// <summary>
    /// No specific secret type or default type.
    /// </summary>
    None = 0,

    /// <summary>
    /// Web-based secret for website login credentials.
    /// </summary>
    Web = 1,

    /// <summary>
    /// AWS secret for Amazon Web Services credentials.
    /// </summary>
    Aws = 2,
}
