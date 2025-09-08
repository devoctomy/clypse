using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using clypse.core.Cryptogtaphy;
using clypse.core.Enums;
using clypse.core.Extensions;

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
    /// <param name="shuffleTokens">Whether to shuffle the tokens in the generated password.</param>
    /// <returns>Returns a password adhering to the format specified by the provided template.</returns>
    public string GenerateMemorablePassword(
        string template,
        bool shuffleTokens)
    {
        this.ThrowIfDisposed();
        try
        {
            var password = shuffleTokens ? this.ShuffleTemplateTokens(template) : template;
            var tokens = ExtractTokensFromTemplate(password);
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
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Generates a random password based on the specified character groups and length.
    /// </summary>
    /// <param name="groups">Character groups to include in the password.</param>
    /// <param name="length">Length of the password to generate.</param>
    /// <param name="atLeastOneOfEachGroup">If true, ensures that at least one character from each selected group is included in the password.</param>
    /// <returns>A randomly generated password.</returns>
    public string GenerateRandomPassword(
        CharacterGroup groups,
        int length,
        bool atLeastOneOfEachGroup)
    {
        var groupsFromFlags = groups.GetGroupsFromFlags();
        if (length < groupsFromFlags.Count)
        {
            throw new ArgumentException("Length must be at least equal to the number of selected character groups.", nameof(length));
        }

        var password = new StringBuilder();
        var groupsChars = new List<string>();

        if (groups.HasFlag(CharacterGroup.Lowercase))
        {
            this.AddCharGroupChars(
                CharacterGroup.Lowercase,
                groupsChars,
                atLeastOneOfEachGroup,
                password);
        }

        if (groups.HasFlag(CharacterGroup.Uppercase))
        {
            this.AddCharGroupChars(
                CharacterGroup.Uppercase,
                groupsChars,
                atLeastOneOfEachGroup,
                password);
        }

        if (groups.HasFlag(CharacterGroup.Digits))
        {
            this.AddCharGroupChars(
                CharacterGroup.Digits,
                groupsChars,
                atLeastOneOfEachGroup,
                password);
        }

        if (groups.HasFlag(CharacterGroup.Special))
        {
            this.AddCharGroupChars(
                CharacterGroup.Special,
                groupsChars,
                atLeastOneOfEachGroup,
                password);
        }

        while (password.Length < length)
        {
            var group = groupsChars[this.randomGeneratorService.GetRandomInt(0, groupsChars.Count)];
            var index = this.randomGeneratorService.GetRandomInt(0, group.Length);
            var insertIndex = this.randomGeneratorService.GetRandomInt(0, password.Length + 1);
            password.Insert(insertIndex, group[index]);
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

    private void AddCharGroupChars(
        CharacterGroup group,
        List<string> groupsChars,
        bool atLeastOneOfEachGroup,
        StringBuilder password)
    {
        var curGroupChars = CharacterGroups.GetGroup(group);
        groupsChars.Add(curGroupChars);
        if (atLeastOneOfEachGroup)
        {
            var randomChar = curGroupChars[this.randomGeneratorService.GetRandomInt(0, curGroupChars.Length)];
            password.Append(randomChar);
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

    private string ShuffleTemplateTokens(string template)
    {
        var tokens = ExtractTokensFromTemplate(template);

        if (tokens.Count <= 1)
        {
            return template;
        }

        var listSwapped = new List<string>();
        var swapped = template;
        for (var i = 0; i < tokens.Count * 10; i++)
        {
            var token1 = this.randomGeneratorService.GetRandomArrayEntry<Match>(tokens.ToArray());
            var token2 = this.randomGeneratorService.GetRandomArrayEntry<Match>(tokens.ToArray());
            while (token1 == token2)
            {
                token2 = this.randomGeneratorService.GetRandomArrayEntry<Match>(tokens.ToArray());
            }

            swapped = token1.SwapWith(token2, swapped);
            tokens = ExtractTokensFromTemplate(swapped);
            listSwapped.Add(token1.Value);
        }

        return swapped;
    }

    private string ProcessToken(string token)
    {
        StringBuilder processedToken = new StringBuilder();
        var tokenValue = token.Trim('{', '}');
        var tokenParts = tokenValue.Split(':');
        foreach (var curPart in tokenParts)
        {
            var curPartLower = curPart.ToLower();
            if (curPartLower == "random")
            {
                var allCasingOptions = new[] { "upper", "lower", "initialupper", "initiallower" };
                curPartLower = this.randomGeneratorService.GetRandomArrayEntry<string>(allCasingOptions);
            }

            switch (curPartLower)
            {
                case "upper":
                    processedToken = new StringBuilder(processedToken.ToString().ToUpper());
                    break;

                case "lower":
                    processedToken = new StringBuilder(processedToken.ToString().ToLower());
                    break;

                case "initialupper":
                    var initialUpper = processedToken.ToString().ToLower();
                    initialUpper = char.ToUpper(initialUpper[0]) + initialUpper[1..];
                    processedToken = new StringBuilder(initialUpper);
                    break;

                case "initiallower":
                    var initialLower = processedToken.ToString().ToUpper();
                    initialLower = char.ToLower(initialLower[0]) + initialLower[1..];
                    processedToken = new StringBuilder(initialLower);
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
