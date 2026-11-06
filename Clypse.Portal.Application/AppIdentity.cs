namespace Clypse.Portal.Application;

/// <summary>
/// Identifies this application. Each app created from the template gets its own name and a new ID.
/// </summary>
public static class AppIdentity
{
    /// <summary>
    /// The application name.
    /// </summary>
    public const string Name = "clypse";

    /// <summary>
    /// The application's unique ID. Keys derived from WebAuthn are salted with it, so changing it makes everything
    /// encrypted with them (such as remembered passwords) unreadable.
    /// </summary>
    public const string Id = "d5154ec0-091c-4f5f-bcc8-4700deddd61a";
}
