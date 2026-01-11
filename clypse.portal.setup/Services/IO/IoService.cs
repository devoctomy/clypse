using System.Diagnostics.CodeAnalysis;

namespace clypse.portal.setup.Services.IO;

[ExcludeFromCodeCoverage()]
public class IoService : IIoService
{
    public void Delete(string path)
    {
        File.Delete(path);
    }

    public Stream OpenWrite(string path)
    {
        return File.OpenWrite(path);
    }
}