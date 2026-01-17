namespace clypse.portal.setup.Services.IO;

/// <summary>
/// Provides filesystem helper operations.
/// </summary>
public interface IIoService
{
    /// <summary>
    /// Determines whether a file exists at the specified path.
    /// </summary>
    public bool FileExists(string path);

    /// <summary>
    /// Determines whether a directory exists at the specified path.
    /// </summary>
    public bool DirectoryExists(string path);

    /// <summary>
    /// Creates a directory at the specified path if it does not exist.
    /// </summary>
    public void CreateDirectory(string path);

    /// <summary>
    /// Deletes the file at the specified path.
    /// </summary>
    public void Delete(string path);

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    public string GetCurrentDirectory();

    /// <summary>
    /// Gets the parent directory of the specified path.
    /// </summary>
    public string? GetParentDirectory(string path);

    /// <summary>
    /// Gets the directory name for the specified path.
    /// </summary>
    public string? GetDirectoryName(string path);

    /// <summary>
    /// Combines multiple path segments into a single path.
    /// </summary>
    public string CombinePath(params string[] paths);

    /// <summary>
    /// Reads all text from a file asynchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all text from a file synchronously.
    /// </summary>
    public string ReadAllText(string path);

    /// <summary>
    /// Opens a stream for writing to a file.
    /// </summary>
    public Stream OpenWrite(string path);

    /// <summary>
    /// Writes text to a file, overwriting existing content.
    /// </summary>
    public void WriteAllText(string path, string contents);

    /// <summary>
    /// Gets files within a directory that match a search pattern.
    /// </summary>
    public string[] GetFiles(string path, string searchPattern);
}
