namespace clypse.core.Password;

/// <summary>
/// Defines a contract for processing tokens used in password generation.
/// </summary>
public interface IPasswordGeneratorTokenProcessor
{
    /// <summary>
    /// Determines if the processor can handle the given token.
    /// </summary>
    /// <param name="token">The token to check.</param>
    /// <returns>True if the processor can handle the token; otherwise, false.</returns>
    public bool IsApplicable(string token);

    /// <summary>
    /// Processes the given token and returns the result.
    /// </summary>
    /// <param name="passwordGeneratorService">The password generator service to use for processing.</param>
    /// <param name="token">The token to process.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The processed result of the token.</returns>
    public Task<string> ProcessAsync(
        IPasswordGeneratorService passwordGeneratorService,
        string token,
        CancellationToken cancellationToken);
}
