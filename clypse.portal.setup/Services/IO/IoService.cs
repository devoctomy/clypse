using System.Diagnostics.CodeAnalysis;

namespace clypse.portal.setup.Services.IO;

[ExcludeFromCodeCoverage()]
public class IoService : IIoService
{
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void Delete(string path)
    {
        File.Delete(path);
    }

    public Stream OpenWrite(string path)
    {
        return File.OpenWrite(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public string GetCurrentDirectory()
    {
        return Directory.GetCurrentDirectory();
    }

    public string? GetParentDirectory(string path)
    {
        return Directory.GetParent(path)?.FullName;
    }

    public string? GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }

    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }

    public async Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        return text;
    }
}