using System.Reflection;

namespace clypse.portal.setup.Services.CommandLineParser;

public interface IPropertyValueSetterService
{
    bool SetPropertyValue<T>(
        T option,
        PropertyInfo property,
        string? value);
}
