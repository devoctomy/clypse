using Clypse.Core.Enums;

namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// How strong a password is estimated to be.
/// </summary>
public class PasswordStrength
{
    /// <summary>
    /// Gets the strength of no password.
    /// </summary>
    public static PasswordStrength None { get; } = new PasswordStrength { Estimation = PasswordComplexityEstimation.None };

    /// <summary>
    /// Gets the estimate.
    /// </summary>
    public PasswordComplexityEstimation Estimation { get; init; }

    /// <summary>
    /// Gets the estimated entropy, in bits.
    /// </summary>
    public double Entropy { get; init; }

    /// <summary>
    /// Gets how full the strength meter is, from 0 to 100.
    /// </summary>
    public int Percent => Estimation switch
    {
        PasswordComplexityEstimation.VeryWeak => 15,
        PasswordComplexityEstimation.Weak => 35,
        PasswordComplexityEstimation.Medium => 60,
        PasswordComplexityEstimation.Strong => 80,
        PasswordComplexityEstimation.VeryStrong => 100,
        _ => 0,
    };

    /// <summary>
    /// Gets the meter's tone: a Bootstrap colour name.
    /// </summary>
    public string Tone => Estimation switch
    {
        PasswordComplexityEstimation.VeryWeak or PasswordComplexityEstimation.Weak => "danger",
        PasswordComplexityEstimation.Medium => "warning",
        PasswordComplexityEstimation.Strong or PasswordComplexityEstimation.VeryStrong => "success",
        _ => "secondary",
    };
}
