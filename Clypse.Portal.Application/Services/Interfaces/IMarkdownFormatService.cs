using Clypse.Portal.Models.Files;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Works out the edit a formatting button makes to a markdown document: e.g. Bold puts <c>**</c> round the selected
/// text, or takes them away again if they are already there.
/// </summary>
public interface IMarkdownFormatService
{
    /// <summary>
    /// Works out the edit that applies a format to the selected text, or removes it if it is already applied.
    /// </summary>
    /// <param name="text">The editor's full text.</param>
    /// <param name="selectionStart">The start of the selection.</param>
    /// <param name="selectionEnd">The end of the selection (the same as the start when nothing is selected).</param>
    /// <param name="format">The format.</param>
    /// <returns>The edit, with the selection to make afterwards.</returns>
    MarkdownTextEdit Apply(string text, int selectionStart, int selectionEnd, MarkdownFormat format);
}
