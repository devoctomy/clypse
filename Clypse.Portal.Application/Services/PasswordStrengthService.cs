using System.Globalization;
using Clypse.Core.Enums;
using Clypse.Core.Password;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Vaults;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IPasswordStrengthService"/> over clypse's complexity estimator and its list of weak
/// known passwords.
/// </summary>
/// <param name="estimator">Estimates complexity.</param>
/// <param name="weakPasswords">The list of weak known passwords.</param>
/// <param name="text">Clypse's text.</param>
public class PasswordStrengthService(
    IPasswordComplexityEstimatorService estimator,
    IWeakKnownPasswordsService weakPasswords,
    IStringLocalizer<ClypseText> text)
    : IPasswordStrengthService
{
    private readonly IPasswordComplexityEstimatorService estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
    private readonly IWeakKnownPasswordsService weakPasswords = weakPasswords ?? throw new ArgumentNullException(nameof(weakPasswords));
    private readonly IStringLocalizer<ClypseText> text = text ?? throw new ArgumentNullException(nameof(text));

    /// <inheritdoc/>
    public async Task<PasswordStrength> EstimateAsync(string? password, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(password))
        {
            return PasswordStrength.None;
        }

        var result = await estimator.EstimateAsync(password, false, ct);
        return new PasswordStrength { Estimation = result.ComplexityEstimation, Entropy = result.EstimatedEntropy };
    }

    /// <inheritdoc/>
    public async Task<PasswordStrength> EstimateCheckingKnownPasswordsAsync(string? password, CancellationToken ct = default)
    {
        var strength = await EstimateAsync(password, ct);
        if (string.IsNullOrEmpty(password) || !await weakPasswords.ContainsAsync(password, ct))
        {
            return strength;
        }

        return new PasswordStrength { Estimation = PasswordComplexityEstimation.VeryWeak, Entropy = strength.Entropy, IsKnownWeak = true };
    }

    /// <inheritdoc/>
    public string Describe(PasswordStrength strength)
    {
        ArgumentNullException.ThrowIfNull(strength);

        if (strength.IsKnownWeak)
        {
            return text["Strength.KnownWeak"];
        }

        return strength.Estimation switch
        {
            PasswordComplexityEstimation.None => string.Empty,
            PasswordComplexityEstimation.Unknown => text["Strength.Unknown"],
            _ => text["Strength.Described", text["Strength." + strength.Estimation], Math.Round(strength.Entropy).ToString(CultureInfo.CurrentCulture)],
        };
    }
}
