namespace clypse.portal;

/// <summary>
/// Global constants and utilities for the Clypse Portal application.
/// </summary>
public static class Globals
{
#if DEBUG
    /// <summary>
    /// Indicates whether this is a debug build.
    /// </summary>
    public const bool IsDebugBuild = true;
#else
    /// <summary>
    /// Indicates whether this is a debug build.
    /// </summary>
    public const bool IsDebugBuild = false;
#endif
}
