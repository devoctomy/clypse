using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Processor for handling dictionary tokens in password generation.
/// </summary>
public class DictionaryTokenProcessor : IPasswordGeneratorTokenProcessor
{
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
    /// <returns>The processed result of the token.</returns>
    public string Process(
        IPasswordGeneratorService passwordGeneratorService,
        string token)
    {
        var dictionary = token.Replace("dict", string.Empty).Trim('(', ')');
        if (dictionary.Contains("|"))
        {
            var allDicts = dictionary.Split('|', StringSplitOptions.RemoveEmptyEntries);
            dictionary = passwordGeneratorService.RandomGeneratorService.GetRandomArrayEntry<string>(allDicts);
        }

        if (Enum.TryParse<DictionaryType>(dictionary, true, out var dictType))
        {
            var words = passwordGeneratorService.GetOrLoadDictionary(dictType);
            var randomWord = passwordGeneratorService.RandomGeneratorService.GetRandomArrayEntry<string>(words.ToArray());
            return randomWord;
        }

        return string.Empty;
    }
}
