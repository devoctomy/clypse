namespace clypse.portal.setup.Services.CommandLineParser;

public class DefaultArgumentParserServiceSetDefaultOptionResult
{
    public bool Success { get; set; }
    public string InvalidValue { get; set; } = string.Empty;
    public string UpdatedArgumentsString { get; set; } = string.Empty;
}
