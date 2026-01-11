namespace clypse.portal.setup.Services.IO;

public interface IIoService
{
    public void Delete(string path);

    public Stream OpenWrite(string path);
}
