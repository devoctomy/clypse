using System.Diagnostics.CodeAnalysis;

namespace clypse.portal.setup.Services.CommandLineParser;

[ExcludeFromCodeCoverage]
public class ParseResults
{
    public object? Options { get; set; }
    public Exception? Exception { get; set; }
    public Dictionary<string, string> InvalidOptions { get; } = new Dictionary<string, string>();

    public T OptionsAs<T>()
    {
        ArgumentNullException.ThrowIfNull(Options);

        if (Options is not T)
        {
            throw new InvalidCastException($"Cannot cast Options of type {Options?.GetType().FullName ?? "null"} to type {typeof(T).FullName}");
        }

        return (T)Options;
    }
}
