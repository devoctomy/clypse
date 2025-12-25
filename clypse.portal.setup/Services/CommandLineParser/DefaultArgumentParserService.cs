using System.Reflection;

namespace clypse.portal.setup.Services.CommandLineParser;

public class DefaultArgumentParserService : IDefaultArgumentParserService
{
    private readonly IPropertyValueSetterService _propertyValueSetter;

    public DefaultArgumentParserService(IPropertyValueSetterService propertyValueSetter)
    {
        _propertyValueSetter = propertyValueSetter;
    }

    public DefaultArgumentParserServiceSetDefaultOptionResult SetDefaultOption(
        Type optionsType,
        object optionsInstance,
        Dictionary<PropertyInfo, CommandLineParserOptionAttribute> allOptions,
        string argumentString,
        List<CommandLineParserOptionAttribute> allSetOptions)
    {
        var defaultOptionValue = string.Empty;
        if (!argumentString.StartsWith("-", StringComparison.Ordinal))
        {
            var argContainsSpace = argumentString.IndexOf(' ') > -1;
            defaultOptionValue = argContainsSpace
                ? argumentString[..argumentString.IndexOf(' ')]
                : argumentString;
            argumentString = argContainsSpace
                ? argumentString[(argumentString.IndexOf(' ') + 1)..]
                : String.Empty;
        }
        var defaultOption = allOptions.SingleOrDefault(x => x.Value.IsDefault);
        if (defaultOption.Key != null && !string.IsNullOrEmpty(defaultOptionValue))
        {
            if(_propertyValueSetter.SetPropertyValue(
                optionsInstance,
                defaultOption.Key,
                defaultOptionValue))
            {
                allSetOptions.Add(defaultOption.Value);
                return new DefaultArgumentParserServiceSetDefaultOptionResult
                {
                    Success = true,
                    UpdatedArgumentsString = argumentString
                };
            }

            return new DefaultArgumentParserServiceSetDefaultOptionResult
            {
                Success = false,
                InvalidValue = defaultOptionValue,
                UpdatedArgumentsString = argumentString
            };
        }

        // Default wasn't required
        return new DefaultArgumentParserServiceSetDefaultOptionResult
        {
            Success = true,
            UpdatedArgumentsString = argumentString
        };
    }
}
