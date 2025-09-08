namespace clypse.core.Enums;

/// <summary>
/// Represents the estimated complexity level of a password.
/// </summary>
public enum PasswordComplexityEstimation
{
    /// <summary>
    /// No complexity estimation available.
    /// </summary>
    None = 0,

    /// <summary>
    /// Unknown complexity.
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// Very weak password.
    /// </summary>
    VeryWeak = 2,

    /// <summary>
    /// Weak password.
    /// </summary>
    Weak = 3,

    /// <summary>
    /// Medium complexity password.
    /// </summary>
    Medium = 4,

    /// <summary>
    /// Strong password.
    /// </summary>
    Strong = 5,

    /// <summary>
    /// Very strong password.
    /// </summary>
    VeryStrong = 6,
}
