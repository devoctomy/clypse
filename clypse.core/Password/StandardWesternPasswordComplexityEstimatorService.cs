using System.Reflection;
using clypse.core.Data;
using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Service for estimating the complexity of passwords.
/// </summary>
#pragma warning disable CS9113 // Parameter is unrea. It is used but is excluded in DEBUG configuration.
public class StandardWesternPasswordComplexityEstimatorService(
    IEmbeddedResorceLoaderService embeddedResorceLoaderService)
    : IPasswordComplexityEstimatorService
#pragma warning restore CS9113 // Parameter is unread.
{
    /// <summary>
    /// Estimates the entropy of the given password.
    /// </summary>
    /// <param name="password">The password to estimate.</param>
    /// <returns>A double value representing the estimated entropy.</returns>
    public double EstimateEntropy(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return 0;
        }

        var charsByGroup = this.GetCharsByGroup();
        var charCountsByGroup = this.GetCharCountsByGroup(password, charsByGroup);
        if (charCountsByGroup == null)
        {
            return -1;
        }

        var possibleCharsPerChar = 0;
        foreach (var curGroup in charCountsByGroup.Keys)
        {
            var charsInGroup = charsByGroup[curGroup].Length;
            possibleCharsPerChar += charsInGroup;
        }

        var entropy = password.Length * Math.Log(possibleCharsPerChar, 2);
        return entropy;
    }

    /// <summary>
    /// Estimates the complexity of the given password.
    /// </summary>
    /// <param name="password">The password to estimate.</param>
    /// <param name="checkForPwnedPasswords">Whether to check the password against a database of known compromised passwords.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="PasswordComplexityEstimatorResults"/> value representing the estimated complexity and any additional information.</returns>    /// <exception cref="ArgumentNullException">Thrown if the password is null.</exception>
    public async Task<PasswordComplexityEstimatorResults> EstimateAsync(
        string password,
        bool checkForPwnedPasswords,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(password))
        {
            return new PasswordComplexityEstimatorResults
            {
                EstimatedEntropy = 0,
                ComplexityEstimation = PasswordComplexityEstimation.None,
                AdditionalInfo = "No password was provided.",
            };
        }

        var entropy = (int)Math.Round(this.EstimateEntropy(password), 0);
        var complexity = PasswordComplexityEstimation.Unknown;

        if (checkForPwnedPasswords && await this.IsWeakKnownPasswordAsync(password, cancellationToken))
        {
            return new PasswordComplexityEstimatorResults
            {
                EstimatedEntropy = entropy,
                ComplexityEstimation = PasswordComplexityEstimation.VeryWeak,
                AdditionalInfo = "This password is found in a list of weak known passwords.",
            };
        }

        complexity = CalculateComplexity(entropy, complexity);

        return new PasswordComplexityEstimatorResults
        {
            EstimatedEntropy = entropy,
            ComplexityEstimation = complexity,
            AdditionalInfo = string.Empty,
        };
    }

    private static PasswordComplexityEstimation CalculateComplexity(
        int entropy,
        PasswordComplexityEstimation complexity)
    {
        if (entropy < 0)
        {
            complexity = PasswordComplexityEstimation.Unknown;
        }
        else if (entropy <= 75)
        {
            complexity = PasswordComplexityEstimation.VeryWeak;
        }
        else if (entropy <= 91)
        {
            complexity = PasswordComplexityEstimation.Weak;
        }
        else if (entropy <= 95)
        {
            complexity = PasswordComplexityEstimation.Medium;
        }
        else if (entropy <= 105)
        {
            complexity = PasswordComplexityEstimation.Strong;
        }
        else if (entropy > 105)
        {
            complexity = PasswordComplexityEstimation.VeryStrong;
        }

        return complexity;
    }

    private async Task<bool> IsWeakKnownPasswordAsync(
        string password,
        CancellationToken cancellationToken)
    {
        HashSet<string>? weakKnownPasswords = default;

#if DEBUG
        await Task.Yield();
        weakKnownPasswords =
        [
            "password123",
        ];
#else
        weakKnownPasswords ??= await embeddedResorceLoaderService.LoadCompressedHashSetAsync(
            ResourceKeys.CompressedWeakKnownPasswordsResourceKey,
            Assembly.GetExecutingAssembly(),
            cancellationToken);
#endif

        return weakKnownPasswords.Contains(password);
    }

    private Dictionary<CharacterGroup, string> GetCharsByGroup()
    {
        var charsByGroup = new Dictionary<CharacterGroup, string>();
        var allGroups = Enum.GetValues<CharacterGroup>();
        foreach (var group in allGroups)
        {
            if (group == CharacterGroup.None)
            {
                continue;
            }

            var chars = CharacterGroups.GetGroup(group);
            charsByGroup.Add(group, chars);
        }

        return charsByGroup;
    }

    private Dictionary<CharacterGroup, int>? GetCharCountsByGroup(
        string password,
        Dictionary<CharacterGroup, string> charsByGroup)
    {
        var charCountsByGroup = new Dictionary<CharacterGroup, int>();
        foreach (var curChar in password)
        {
            var group = charsByGroup.FirstOrDefault(x => x.Value.Contains(curChar)).Key;
            if (group == CharacterGroup.None)
            {
                return null;
            }

            if (!charCountsByGroup.TryGetValue(group, out int value))
            {
                value = 0;
                charCountsByGroup[group] = value;
            }

            charCountsByGroup[group] = ++value;
        }

        return charCountsByGroup;
    }
}
