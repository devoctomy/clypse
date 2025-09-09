using System.Reflection;
using System.Runtime.CompilerServices;
using clypse.core.Compression;
using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Service for estimating the complexity of passwords.
/// </summary>
public class StandardWesternPasswordComplexityEstimatorService : IPasswordComplexityEstimatorService
{
    private static HashSet<string>? weakKnownPasswords;

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

        if (checkForPwnedPasswords && await IsWeakKnownPasswordAsync(password, cancellationToken))
        {
            return new PasswordComplexityEstimatorResults
            {
                EstimatedEntropy = entropy,
                ComplexityEstimation = PasswordComplexityEstimation.VeryWeak,
                AdditionalInfo = "This password is found in a list of weak known passwords.",
            };
        }

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

        return new PasswordComplexityEstimatorResults
        {
            EstimatedEntropy = entropy,
            ComplexityEstimation = complexity,
            AdditionalInfo = string.Empty,
        };
    }

    private static async Task<bool> IsWeakKnownPasswordAsync(
        string password,
        CancellationToken cancellationToken)
    {
        weakKnownPasswords ??= await LoadWeakKnownPasswordsDictionaryAsync(cancellationToken);

        return weakKnownPasswords.Contains(password);
    }

    private static async Task<HashSet<string>> LoadWeakKnownPasswordsDictionaryAsync(CancellationToken cancellationToken)
    {
        var dictionaryKey = $"clypse.core.Data.Dictionaries.weakknownpasswords.txt.gz";
        var assembly = Assembly.GetExecutingAssembly();
        using Stream? compressedStream = assembly.GetManifestResourceStream(dictionaryKey) ?? throw new InvalidOperationException($"Resource '{dictionaryKey}' not found.");
        var compressionService = new GZipCompressionService();
        var decompressedStream = new MemoryStream();
        await compressionService.DecompressAsync(compressedStream, decompressedStream, cancellationToken);
        decompressedStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(decompressedStream);
        string content = await reader.ReadToEndAsync(cancellationToken);
        var lines = content.Split("\r\n");
        return [.. lines];
    }
}
