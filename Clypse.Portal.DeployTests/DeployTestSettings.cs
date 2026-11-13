namespace Clypse.Portal.DeployTests;

/// <summary>
/// What the deploy tests are given, from environment variables: the deployed portal and the test user, an account kept
/// only for these tests.
/// </summary>
public sealed class DeployTestSettings
{
    public const string PortalUrlVariable = "CLYPSE_DEPLOYTEST_PORTAL_URL";
    public const string EmailVariable = "CLYPSE_DEPLOYTEST_EMAIL";
    public const string PasswordVariable = "CLYPSE_DEPLOYTEST_PASSWORD";

    /// <summary>
    /// Gets the portal's address, without a trailing slash.
    /// </summary>
    public required string PortalUrl { get; init; }

    public required string Email { get; init; }

    public required string Password { get; init; }

    /// <summary>
    /// Reads the settings, or null when any is missing (the tests are then skipped).
    /// </summary>
    /// <returns>The settings, or null.</returns>
    public static DeployTestSettings? FromEnvironment()
    {
        var url = Environment.GetEnvironmentVariable(PortalUrlVariable);
        var email = Environment.GetEnvironmentVariable(EmailVariable);
        var password = Environment.GetEnvironmentVariable(PasswordVariable);
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        return new DeployTestSettings
        {
            PortalUrl = url.TrimEnd('/'),
            Email = email,
            Password = password,
        };
    }
}
