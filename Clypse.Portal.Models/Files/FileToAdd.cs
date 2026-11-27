namespace Clypse.Portal.Models.Files;

/// <summary>
/// A file the user picked to add to a vault. Its content is only read once the file has been checked.
/// </summary>
public class FileToAdd
{
    /// <summary>
    /// Gets the file's name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the content type the browser reported, if any.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the file's size, in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Gets what reads the file's content.
    /// </summary>
    public Func<CancellationToken, Task<byte[]>> ReadAsync { get; init; } = _ => Task.FromResult(Array.Empty<byte>());
}
