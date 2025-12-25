namespace clypse.portal.setup.Services.CommandLineParser;

public interface ICommandLineParserService
{
    bool TryParseArgumentsAsOptions(
        Type optionsType,
        string argumentString,
        out ParseResults? options);
}
