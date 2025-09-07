using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using clypse.core.Cryptogtaphy;
using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Default implementation of IPasswordGeneratorService.
/// </summary>
public partial class PasswordGeneratorService : IPasswordGeneratorService
{
    private readonly Dictionary<string, List<string>> dictionaryCache = [];

    /// <summary>
    /// Loads a dictionary of words based on the specified dictionary type.
    /// </summary>
    /// <param name="dictionaryType">The type of dictionary to load.</param>
    /// <returns>A list of words from the specified dictionary.</returns>
    public List<string> LoadDictionary(DictionaryType dictionaryType)
    {
        var dictionaryKey = $"clypse.core.Data.Dictionaries.{dictionaryType.ToString().ToLower()}.txt";
        var assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(dictionaryKey) ?? throw new InvalidOperationException($"Resource '{dictionaryKey}' not found.");
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
        }

        return lines;
    }

    /// <summary>
    /// Generates a memorable password based on the provided template.
    /// </summary>
    /// <param name="template">Template to use for password generation.</param>
    /// <returns>Returns a password adhering to the format specified by the provided template.</returns>
    public string GenerateMemorablePassword(string template)
    {
        var password = template;
        var tokens = ExtractTokensFromTemplate(template);
        for (var i = tokens.Count - 1; i >= 0; i--)
        {
            var curToken = tokens[i];
            var processedToken = this.ProcessToken(curToken);
            password = ReplaceAt(
                password,
                curToken.Index,
                curToken.Length,
                processedToken);
        }

        return password;
    }

    private static List<Match> ExtractTokensFromTemplate(string template)
    {
        var matches = TokenExtractionRegex().Matches(template);
        return matches.ToList();
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex TokenExtractionRegex();

    private static string ReplaceAt(
        string input,
        int index,
        int length,
        string replacement)
    {
        return string.Concat(
            input.AsSpan(0, index),
            replacement,
            input.AsSpan(index + length));
    }

    private string ProcessToken(Match token)
    {
        StringBuilder processedToken = new StringBuilder();
        var tokenValue = token.Value.Trim('{', '}');
        var tokenParts = tokenValue.Split(':');
        foreach (var curPart in tokenParts)
        {
            switch (curPart.ToLower())
            {
                case "upper":
                    processedToken = new StringBuilder(processedToken.ToString().ToUpper());
                    break;

                case "lower":
                    processedToken = new StringBuilder(processedToken.ToString().ToLower());
                    break;

                default:
                    if (curPart.StartsWith("dict("))
                    {
                        var dictionary = curPart.Replace("dict", string.Empty).Trim('(', ')');
                        if (Enum.TryParse<DictionaryType>(dictionary, true, out var dictType))
                        {
                            var words = this.GetOrLoadDictionary(dictType);
                            var randomWord = CryptoHelpers.GetRandomArrayEntry<string>(words.ToArray());
                            processedToken.Append(randomWord);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Invalid dictionary type: {dictionary}");
                        }
                    }
                    else if (curPart.StartsWith("randstr("))
                    {
                        var randstrArgs = curPart.Replace("randstr", string.Empty).Trim('(', ')');
                        var argsParts = randstrArgs.Split(',');
                        var chars = argsParts[0];
                        var length = int.Parse(argsParts[1]);
                        processedToken.Append(CryptoHelpers.GetRandomStringContainingCharacters(length, chars));
                    }

                    break;
            }
        }

        return processedToken.ToString();
    }

    private List<string> GetOrLoadDictionary(DictionaryType dictionaryType)
    {
        var key = dictionaryType.ToString();
        if (!this.dictionaryCache.TryGetValue(key, out List<string>? value))
        {
            var words = this.LoadDictionary(dictionaryType);
            value = words;
            this.dictionaryCache[key] = value;
        }

        return value;
    }
}
