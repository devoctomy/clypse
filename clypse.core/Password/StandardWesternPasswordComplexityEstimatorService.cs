using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Service for estimating the complexity of passwords.
/// </summary>
public class StandardWesternPasswordComplexityEstimatorService : IPasswordComplexityEstimatorService
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

        var charCountsByGroup = new Dictionary<CharacterGroup, int>();
        foreach (var curChar in password)
        {
            var group = charsByGroup.FirstOrDefault(x => x.Value.Contains(curChar)).Key;
            if (group == CharacterGroup.None)
            {
                return -1;
            }

            if (!charCountsByGroup.TryGetValue(group, out int value))
            {
                value = 0;
                charCountsByGroup[group] = value;
            }

            charCountsByGroup[group] = ++value;
        }

        var possibleCharsPerChar = 0;
        foreach (var curGroup in charCountsByGroup.Keys)
        {
            var count = charCountsByGroup[curGroup];
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
    /// <returns>A <see cref="PasswordComplexityEstimatorResults"/> value representing the estimated complexity and any additional information.</returns>    /// <exception cref="ArgumentNullException">Thrown if the password is null.</exception>
    public PasswordComplexityEstimatorResults Estimate(string password)
    {
        var entropy = (int)Math.Round(this.EstimateEntropy(password), 0);
        var complexity = PasswordComplexityEstimation.Unknown;

        if (entropy < 0)
        {
            complexity = PasswordComplexityEstimation.Unknown;
        }
        else if (entropy == 0)
        {
            complexity = PasswordComplexityEstimation.None;
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

        return new PasswordComplexityEstimatorResults
        {
            EstimatedEntropy = entropy,
            ComplexityEstimation = complexity,
            AdditionalInfo = string.Empty,
        };
    }
}
