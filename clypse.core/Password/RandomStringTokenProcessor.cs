namespace clypse.core.Password;

/// <summary>
/// Processor for handling random string tokens in password generation.
/// </summary>
public class RandomStringTokenProcessor : IPasswordGeneratorTokenProcessor
{
    /// <summary>
    /// Determines if the processor can handle the given token.
    /// </summary>
    /// <param name="token">The token to check.</param>
    /// <returns>True if the processor can handle the token; otherwise, false.</returns>
    public bool IsApplicable(string token)
    {
        return token.StartsWith("randstr(");
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
        var randstrArgs = token.Replace("randstr", string.Empty).Trim('(', ')');
        var lastCommaIndex = randstrArgs.LastIndexOf(',');
        var chars = randstrArgs.Substring(0, lastCommaIndex);
        var lengthStr = randstrArgs.Substring(lastCommaIndex + 1, randstrArgs.Length - (lastCommaIndex + 1));
        var length = int.Parse(lengthStr);
        var result = passwordGeneratorService.RandomGeneratorService.GetRandomStringContainingCharacters(length, chars);
        return result;
    }
}
