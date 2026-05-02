namespace clypse.portal.setup.Services.Environment;

/// <inheritdoc cref="IEnvironmentService" />
public class EnvironmentService : IEnvironmentService
{
    /// <inheritdoc />
    public string? GetEnvironmentVariable(string variable, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        return System.Environment.GetEnvironmentVariable(variable, target);
    }

    /// <inheritdoc />
    public bool IsWindows => OperatingSystem.IsWindows();
}
