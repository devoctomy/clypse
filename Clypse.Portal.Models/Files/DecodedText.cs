namespace Clypse.Portal.Models.Files;

/// <summary>
/// A text file's content as the text an editor holds, with what the file had that an editor does not show, so it can be
/// put back when the text is saved.
/// </summary>
public class DecodedText
{
    /// <summary>
    /// Gets or sets the text, without a byte order mark and with plain line breaks, as a text area gives them.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the file starts with a byte order mark.
    /// </summary>
    public bool HasByteOrderMark { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the file's lines end the Windows way (a carriage return and a line feed).
    /// </summary>
    public bool UsesWindowsLineEndings { get; set; }
}
