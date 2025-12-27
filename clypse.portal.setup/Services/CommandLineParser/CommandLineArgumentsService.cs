using System.Reflection;

namespace clypse.portal.setup.Services.CommandLineParser;

public class CommandLineArgumentsService : ICommandLineArgumentsService
{
    public string GetArguments(string fullCommandLine)
    {
        var curExePath = Assembly.GetEntryAssembly()!.Location;
        var arguments = fullCommandLine.Replace(curExePath, string.Empty).Trim();
        return arguments;
    }
}
