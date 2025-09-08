using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Results of a password complexity estimation.
/// </summary>
public class PasswordComplexityEstimatorResults
{
    /// <summary>
    /// Gets or sets the estimated entropy of the password.
    /// </summary>
    public double EstimatedEntropy { get; set; } = 0;

    /// <summary>
    /// Gets or sets the estimated entropy of the password.
    /// </summary>
    public PasswordComplexityEstimation ComplexityEstimation { get; set; } = PasswordComplexityEstimation.None;

    /// <summary>
    /// Gets or sets additional information about the estimation.
    /// </summary>
    public string AdditionalInfo { get; set; } = string.Empty;
}
