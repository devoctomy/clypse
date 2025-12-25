using System.Reflection;

namespace clypse.portal.setup.Services.CommandLineParser;

public class CommandLineParserService : ICommandLineParserService
{
    private readonly IDefaultArgumentParserService _defaultArgumentParserService;
    private readonly IArgumentMapperService _argumentMapper;
    private readonly IOptionalArgumentSetterService _optionalArgumentSetterSevice;

    public CommandLineParserService(
        IDefaultArgumentParserService defaultArgumentParserService,
        IArgumentMapperService arumentMapper,
        IOptionalArgumentSetterService optionalArgumentSetterSevice)
    {
        _defaultArgumentParserService = defaultArgumentParserService;
        _argumentMapper = arumentMapper;
        _optionalArgumentSetterSevice = optionalArgumentSetterSevice;
    }

    public static CommandLineParserService CreateDefaultInstance()
    {
        var propertyValueSetterService = new PropertyValueSetterService();
        return new CommandLineParserService(
            new DefaultArgumentParserService(propertyValueSetterService),
            new ArgumentMapperService(
                new ArgumentMapperOptions(),
                new SingleArgumentParserService(),
                propertyValueSetterService),
            new OptionalArgumentSetterService(propertyValueSetterService));
    }

    public bool TryParseArgumentsAsOptions(
        Type optionsType,
        string? argumentString,
        out ParseResults? results)
    {
        // This block is unlikely to happen at runtime as arguments string
        // will always contain application path
        if (string.IsNullOrWhiteSpace(argumentString))
        {
            results = default;
            return false;
        }

        var options = Activator.CreateInstance(optionsType);
        if (options == null)
        {
            throw new InvalidOperationException($"Failed to create instance of type '{optionsType.FullName}'.");
        }

        results = new ParseResults
        {
            Options = options
        };
        var allOptions = GetAllOptions(optionsType);
        var allSetOptions = new List<CommandLineParserOptionAttribute>();
        string invalidOption = string.Empty;
        var result = _defaultArgumentParserService.SetDefaultOption(
            optionsType,
            results.Options,
            allOptions,
            argumentString,
            allSetOptions);
        if (!result.Success)
        {
            var defaultOption = allOptions.SingleOrDefault(x => x.Value.IsDefault);
            results.Exception = new ArgumentException(
                $"Failed to set default argument '{defaultOption.Value.DisplayName}'.",
                $"{defaultOption.Value.DisplayName}");
            results.InvalidOptions.Add(defaultOption.Value.DisplayName, invalidOption);
            return false;
        }

        _optionalArgumentSetterSevice.SetOptionalValues(
            optionsType,
            results.Options,
            allOptions);

        _argumentMapper.MapArguments(
            optionsType,
            results.Options,
            allOptions,
            result.UpdatedArgumentsString,
            allSetOptions);

        var missingRequired = allOptions.Where(x => x.Value.Required && !allSetOptions.Any(y => y.LongName == x.Value.LongName)).ToList();
        if (missingRequired.Any())
        {
            results.Exception = new ArgumentException($"Required arguments missing ({string.Join(',', missingRequired.Select(x => x.Value.LongName))}).");
        }

        return !missingRequired.Any();
    }

    private static Dictionary<PropertyInfo, CommandLineParserOptionAttribute> GetAllOptions(Type optionsType)
    {
        var propeties = new Dictionary<PropertyInfo, CommandLineParserOptionAttribute>();
        var allProperties = optionsType.GetProperties();
        foreach (var curProperty in allProperties)
        {
            var optionAttributes = (CommandLineParserOptionAttribute[])curProperty.GetCustomAttributes(typeof(CommandLineParserOptionAttribute), true);
            var optionAttribute = optionAttributes.FirstOrDefault();
            if (optionAttribute != null)
            {
                propeties.Add(
                    curProperty,
                    optionAttribute);
            }
        }
        return propeties;
    }
}
