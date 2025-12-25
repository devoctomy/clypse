using clypse.portal.setup.Services.CommandLineParser;

namespace clypse.portal.setup.UnitTests.Services.CommandLineParser;

public class CommandLineTestOptions3
{
    [CommandLineParserOption(
        LongName = "enum",
        ShortName = "e",
        Required = false,
        IsDefault = true,
        DisplayName = "Enum")]
    public TestEnum EnumValue { get; set; }
}
