namespace clypse.portal.setup.Services.CommandLineParser;

public interface IHelpMessageFormatter
{
    string Format<T>();
    string Format(Type optionsType);
}
