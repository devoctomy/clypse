using clypse.portal.setup.Enums;

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
    /// <returns>The selected <see cref="SetupMode"/>; returns <see cref="SetupMode.None"/> when the user cancels.</returns>
    public SetupMode Run(SetupOptions options);
}
