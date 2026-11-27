namespace Clypse.Portal.Models.Files;

/// <summary>
/// The text and selection of the text editor, read from the browser.
/// </summary>
public class EditorSelection
{
    /// <summary>
    /// Gets or sets the full text of the editor.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start of the selection (UTF-16 offset).
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// Gets or sets the end of the selection (UTF-16 offset). Equal to <see cref="Start"/> when nothing is selected.
    /// </summary>
    public int End { get; set; }
}
