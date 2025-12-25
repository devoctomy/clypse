using clypse.portal.setup.Services.CommandLineParser;

namespace clypse.portal.setup.UnitTests.Services.CommandLineParser;

public class CommandLineTestBadOptions
{
    [CommandLineParserOption(LongName = "Unsupported", ShortName = "u", Required = true, IsDefault = true)]
    public Guid UnsupportedValue { get; set; }
}