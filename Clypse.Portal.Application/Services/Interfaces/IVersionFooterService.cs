namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Puts clypse's version in the footer of the sign-in pages, before the copyright message.
/// </summary>
public interface IVersionFooterService
{
    /// <summary>
    /// Puts the app's version before the copyright message (e.g. <c>Version 0.9.2 - © 2026 Clypse</c>). Call it once as
    /// the app starts, after the user's language has been applied.
    /// </summary>
    void Apply();
}
