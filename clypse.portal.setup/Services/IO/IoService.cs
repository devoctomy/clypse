using System.Diagnostics.CodeAnalysis;

namespace clypse.portal.setup.Services.IO;

/// <inheritdoc cref="IIoService" />
[ExcludeFromCodeCoverage()]
public class IoService : IIoService
{
    /// <inheritdoc />
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    /// <inheritdoc />
    public void Delete(string path)
    {
        File.Delete(path);
    }

    /// <inheritdoc />
    public Stream OpenWrite(string path)
    {
        return File.OpenWrite(path);
    }

    /// <inheritdoc />
    public void WriteAllText(string path, string contents)
    {
        File.WriteAllText(path, contents);
    }

    /// <inheritdoc />
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    /// <inheritdoc />
    public string GetCurrentDirectory()
    {
        return Directory.GetCurrentDirectory();
    }

    /// <inheritdoc />
    public string? GetParentDirectory(string path)
    {
        return Directory.GetParent(path)?.FullName;
    }

    /// <inheritdoc />
    public string? GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }

    /// <inheritdoc />
    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }

    /// <inheritdoc />
    public async Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        return text;
    }

    /// <inheritdoc />
    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    /// <inheritdoc />
    public string[] GetFiles(
        string path,
        string searchPattern)
    {
        return Directory.GetFiles(path, searchPattern);
    }
}