using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using clypse.core.Cryptogtaphy;
using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Default implementation of IPasswordGeneratorService.
/// </summary>
public partial class PasswordGeneratorService : IPasswordGeneratorService, IDisposable
{
    private readonly IRandomGeneratorService randomGeneratorService;
    private readonly Dictionary<string, List<string>> dictionaryCache = [];
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordGeneratorService"/> class.
    /// </summary>
    /// <param name="randomGeneratorService">An instance of IRandomGeneratorService for generating random values.</param>
    public PasswordGeneratorService(IRandomGeneratorService randomGeneratorService)
    {
        this.randomGeneratorService = randomGeneratorService;
    }

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
        this.ThrowIfDisposed();
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

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the RandomGeneratorService and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                ((IDisposable)this.randomGeneratorService)?.Dispose();
            }

            this.disposed = true;
        }
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
                            var randomWord = this.randomGeneratorService.GetRandomArrayEntry<string>(words.ToArray());
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
                        processedToken.Append(this.randomGeneratorService.GetRandomStringContainingCharacters(length, chars));
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

    /// <summary>
    /// Throws an ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, nameof(RandomGeneratorService));
    }
}
