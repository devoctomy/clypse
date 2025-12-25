using System.Reflection;

namespace clypse.portal.setup.Services.CommandLineParser;

public interface IArgumentMapperService
{
    ////void MapArguments<T>(
    ////    T optionsInstance,
    ////    Dictionary<PropertyInfo, CommandLineParserOptionAttribute> allOptions,
    ////    string argumentString,
    ////    List<CommandLineParserOptionAttribute> allSetOptions);

    void MapArguments(
        Type optionsType,
        object optionsInstance,
        Dictionary<PropertyInfo, CommandLineParserOptionAttribute> allOptions,
        string argumentString,
        List<CommandLineParserOptionAttribute> allSetOptions);
}
