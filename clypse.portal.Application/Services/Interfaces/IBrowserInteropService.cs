namespace clypse.portal.Application.Services.Interfaces;

/// <summary>
/// Provides browser-specific interop functionality for DOM manipulation.
/// </summary>
public interface IBrowserInteropService
{
    /// <summary>
    /// Applies a theme to the HTML document root element.
    /// </summary>
    /// <param name="theme">The theme name to apply (e.g., "light" or "dark").</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetThemeAsync(string theme);
}
