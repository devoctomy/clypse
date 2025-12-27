namespace clypse.portal.setup.Services;

public interface IProgram
{
    Func<string> GetCommandLine { get; set; }
    Task<int> Run();
}
