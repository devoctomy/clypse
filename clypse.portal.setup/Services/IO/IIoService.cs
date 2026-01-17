namespace clypse.portal.setup.Services.IO;

public interface IIoService
{
    public bool FileExists(string path);

    public bool DirectoryExists(string path);

    public void CreateDirectory(string path);

    public void Delete(string path);

    public string GetCurrentDirectory();

    public string? GetParentDirectory(string path);

    public string? GetDirectoryName(string path);

    public string CombinePath(params string[] paths);

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    public string ReadAllText(string path);

    public Stream OpenWrite(string path);

    public void WriteAllText(string path, string contents);

    public string[] GetFiles(string path, string searchPattern);
}
