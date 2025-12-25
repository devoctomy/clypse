using System.Reflection;

namespace clypse.portal.setup.Services.CommandLineParser;

public interface IDefaultArgumentParserService
{
    DefaultArgumentParserServiceSetDefaultOptionResult SetDefaultOption(
        Type optionsType,
        object optionsInstance,
        Dictionary<PropertyInfo, CommandLineParserOptionAttribute> allOptions,
        string argumentString,
        List<CommandLineParserOptionAttribute> allSetOptions);
}
