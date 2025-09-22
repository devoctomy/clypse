using clypse.core.Data;
using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Processor for handling dictionary tokens in password generation.
/// </summary>
public class DictionaryTokenProcessor : IPasswordGeneratorTokenProcessor
{
    private readonly IEmbeddedResorceLoaderService embeddedResorceLoaderService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryTokenProcessor"/> class.
    /// </summary>
    /// <param name="embeddedResorceLoaderService">The service used to load embedded resources.</param>
    public DictionaryTokenProcessor(IEmbeddedResorceLoaderService embeddedResorceLoaderService)
    {
        this.embeddedResorceLoaderService = embeddedResorceLoaderService;
    }

    /// <summary>
    /// Determines if the processor can handle the given token.
    /// </summary>
    /// <param name="token">The token to check.</param>
    /// <returns>True if the processor can handle the token; otherwise, false.</returns>
    public bool IsApplicable(string token)
    {
        return token.StartsWith("dict(");
    }

    /// <summary>
    /// Processes the given token and returns the result.
    /// </summary>
    /// <param name="passwordGeneratorService">The password generator service to use for processing.</param>
    /// <param name="token">The token to process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The processed result of the token.</returns>
    public async Task<string> ProcessAsync(
        IPasswordGeneratorService passwordGeneratorService,
        string token,
        CancellationToken cancellationToken)
    {
        var dictionary = token.Replace("dict", string.Empty).Trim('(', ')');
        if (dictionary.Contains('|'))
        {
            var allDicts = dictionary.Split('|', StringSplitOptions.RemoveEmptyEntries);
            dictionary = passwordGeneratorService.RandomGeneratorService.GetRandomArrayEntry<string>(allDicts);
        }

        if (Enum.TryParse<DictionaryType>(dictionary, true, out var dictType))
        {
            var words = await this.embeddedResorceLoaderService.LoadHashSetAsync(
                GetResourceKey(dictType),
                typeof(DictionaryTokenProcessor).Assembly,
                cancellationToken);
            if (words == null || words.Count == 0)
            {
                return string.Empty;
            }

            var randomWord = passwordGeneratorService.RandomGeneratorService.GetRandomArrayEntry<string>(words.ToArray());
            return randomWord;
        }

        return string.Empty;
    }

    private static string GetResourceKey(DictionaryType dictionaryType)
    {
        return dictionaryType switch
        {
            DictionaryType.Adjective => ResourceKeys.AdjectivesResourceKey,
            DictionaryType.Verb => ResourceKeys.VerbResourceKey,
            DictionaryType.Noun => ResourceKeys.NounResourceKey,
            _ => throw new NotImplementedException($"Dictionary type {dictionaryType} is not supported."),
        };
    }
}
