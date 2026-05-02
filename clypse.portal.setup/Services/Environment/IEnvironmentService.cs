namespace clypse.portal.setup.Services.Environment;

/// <summary>
/// Provides access to environment variables.
/// </summary>
public interface IEnvironmentService
{
    /// <summary>
    /// Gets an environment variable value.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    /// <param name="target">The target environment variable scope.</param>
    /// <returns>The value of the environment variable, or <see langword="null"/> if not found.</returns>
    string? GetEnvironmentVariable(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process);

    /// <summary>
    /// Gets a value indicating whether the current operating system is Windows.
    /// </summary>
    bool IsWindows { get; }
}
