using Amazon.S3.Model;
using clypse.core.Enums;

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
        chars = ReplaceCharactersFromCharGroup(chars);
        var lengthStr = randstrArgs.Substring(lastCommaIndex + 1, randstrArgs.Length - (lastCommaIndex + 1));
        var length = int.Parse(lengthStr);
        var result = passwordGeneratorService.RandomGeneratorService.GetRandomStringContainingCharacters(length, chars);
        return result;
    }

    private static string ReplaceCharactersFromCharGroup(string chars)
    {
        var replaced = chars;
        foreach (var group in Enum.GetNames<CharacterGroup>())
        {
            var token = $"[{group.ToLower()}]";
            if (!replaced.Contains(token))
            {
                continue;
            }

            replaced = replaced.Replace(token, CharacterGroups.GetGroup(Enum.Parse<CharacterGroup>(group, true)));
        }

        return replaced;
    }
}
