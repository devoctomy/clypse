using System.Reflection;

namespace clypse.portal.setup.Services.CommandLineParser;

public class OptionalArgumentSetterService : IOptionalArgumentSetterService
{
    private readonly IPropertyValueSetterService _propertyValueSetter;

    public OptionalArgumentSetterService(IPropertyValueSetterService propertyValueSetter)
    {
        _propertyValueSetter = propertyValueSetter;
    }

    public void SetOptionalValues(
        Type optionsType,
        object optionsInstance,
        Dictionary<PropertyInfo, CommandLineParserOptionAttribute> allOptions)
    {
        var optional = allOptions.Where(x => !x.Value.Required).ToList();
        foreach (var curOptional in optional)
        {
            var defaultValue = curOptional.Value.DefaultValue != null ?
                curOptional.Value.DefaultValue.ToString() :
                string.Empty;
            _propertyValueSetter.SetPropertyValue(
                optionsInstance,
                curOptional.Key,
                defaultValue!);
        }
    }
}
