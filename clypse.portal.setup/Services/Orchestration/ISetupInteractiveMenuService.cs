namespace clypse.portal.setup.Services.Orchestration;

/// <summary>
/// Provides an interactive console menu for configuring setup options.
/// </summary>
public interface ISetupInteractiveMenuService
{
    /// <summary>
    /// Runs the interactive menu to edit setup options.
    /// </summary>
    /// <param name="options">Options instance to populate.</param>
    /// <returns><see langword="true"/> when the user chooses to continue with setup; otherwise, <see langword="false"/>.</returns>
    public bool Run(SetupOptions options);
}
