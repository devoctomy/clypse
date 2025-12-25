namespace clypse.portal.setup.Services.CommandLineParser;

[AttributeUsage(AttributeTargets.Property)]
public class CommandLineParserOptionAttribute : Attribute
{
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public object? DefaultValue { get; set; } 
    public bool Required { get; set; }
    public bool IsDefault { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string HelpText { get; set; } = string.Empty;
}
