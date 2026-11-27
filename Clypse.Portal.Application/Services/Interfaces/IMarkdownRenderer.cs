namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Renders markdown as HTML, for the preview of a vault's markdown documents. The HTML is safe to show: raw HTML in the
/// markdown is not passed through, and links can only go to web pages and email addresses.
/// </summary>
public interface IMarkdownRenderer
{
    /// <summary>
    /// Renders markdown as HTML.
    /// </summary>
    /// <param name="markdown">The markdown.</param>
    /// <returns>The HTML, empty for no markdown.</returns>
    string ToHtml(string? markdown);
}
