using System.Reflection;

namespace clypse.portal.setup.Services.CommandLineParser;

public interface IOptionalArgumentSetterService
{
    void SetOptionalValues(
        Type optionsType,
        object optionsInstance,
        Dictionary<PropertyInfo, CommandLineParserOptionAttribute> allOptions);
}
