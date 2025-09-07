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
    private readonly IEnumerable<IPasswordGeneratorTokenProcessor> tokenProcessors;
    private readonly Dictionary<string, List<string>> dictionaryCache = [];
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordGeneratorService"/> class.
    /// </summary>
    /// <param name="randomGeneratorService">An instance of IRandomGeneratorService for generating random values.</param>
    /// <param name="tokenProcessors">A collection of token processors for handling different token types in password generation.</param>
    public PasswordGeneratorService(
        IRandomGeneratorService randomGeneratorService,
        IEnumerable<IPasswordGeneratorTokenProcessor> tokenProcessors)
    {
        this.randomGeneratorService = randomGeneratorService;
        this.tokenProcessors = tokenProcessors;
    }

    /// <summary>
    /// Gets the random generator service.
    /// </summary>
    public IRandomGeneratorService RandomGeneratorService => this.randomGeneratorService;

    /// <summary>
    /// Gets a dictionary of words from cache or loads it and caches it.
    /// </summary>
    /// <param name="dictionaryType">The type of dictionary to load.</param>
    /// <returns>A list of words from the specified dictionary.</returns>
    public List<string> GetOrLoadDictionary(DictionaryType dictionaryType)
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
            var processedToken = this.ProcessToken(curToken.Value);
            password = ReplaceAt(
                password,
                curToken.Index,
                curToken.Length,
                processedToken);
        }

        return password;
    }

    /// <summary>
    /// Generates a random password based on the specified character groups and length.
    /// </summary>
    /// <param name="groups">Character groups to include in the password.</param>
    /// <param name="length">Length of the password to generate.</param>
    /// <returns>A randomly generated password.</returns>
    public string GenerateRandomPassword(
        CharacterGroup groups,
        int length)
    {
        var lowercase = "abcdefghijklmnopqrstuvwxyz";
        var uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var digits = "0123456789";
        var special = "!@#$%^&*()-_=+[]{}|;:,.<>?";
        var characterGroup = string.Empty;

        if (groups.HasFlag(CharacterGroup.Lowercase))
        {
            characterGroup += lowercase;
        }

        if (groups.HasFlag(CharacterGroup.Uppercase))
        {
            characterGroup += uppercase;
        }

        if (groups.HasFlag(CharacterGroup.Digits))
        {
            characterGroup += digits;
        }

        if (groups.HasFlag(CharacterGroup.Special))
        {
            characterGroup += special;
        }

        var password = new StringBuilder();
        while (password.Length < length)
        {
            var index = this.randomGeneratorService.GetRandomInt(0, characterGroup.Length);
            password.Append(characterGroup[index]);
        }

        return password.ToString();
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

    private List<string> LoadDictionary(DictionaryType dictionaryType)
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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, nameof(this.randomGeneratorService));
    }

    private string ProcessToken(string token)
    {
        StringBuilder processedToken = new StringBuilder();
        var tokenValue = token.Trim('{', '}');
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
                    var processor = this.tokenProcessors.FirstOrDefault(x => x.IsApplicable(curPart));
                    if (processor != null)
                    {
                        processedToken.Append(processor.Process(this, curPart));
                    }

                    break;
            }
        }

        return processedToken.ToString();
    }
}
