namespace Clypse.Core.Configuration;

/// <summary>
/// Which embedded text lists are loaded in full.
/// </summary>
/// <remarks>
/// The weak known passwords list has over five million entries. Loading it takes a long time in a Debug WebAssembly
/// build, so a Debug build of the portal turns <see cref="UseFullWeakKnownPasswordsList"/> off and checks against
/// a small stand-in list instead. Release builds use the full list.
/// </remarks>
public class TextListOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether passwords are checked against the full weak known passwords list (the
    /// default), rather than the small stand-in list in <see cref="StandInWeakKnownPasswords"/>.
    /// </summary>
    public bool UseFullWeakKnownPasswordsList { get; set; } = true;

    /// <summary>
    /// Gets the stand-in weak known passwords used when <see cref="UseFullWeakKnownPasswordsList"/> is off.
    /// </summary>
    public ISet<string> StandInWeakKnownPasswords { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "password123",
    };
}
