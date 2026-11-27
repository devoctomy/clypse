using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Models.Files;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Turns a text file's content into the text an editor holds, and back. A file's byte order mark and Windows line
/// endings are not shown in an editor; they are remembered, and put back when the text is saved, so editing a file does
/// not change them.
/// </summary>
public interface ITextFileEncodingService
{
    /// <summary>
    /// Reads a file's content as UTF-8 text.
    /// </summary>
    /// <param name="content">The file's content.</param>
    /// <returns>The text as an editor holds it, and what the file had.</returns>
    /// <exception cref="ClypseException">The content is not text (<see cref="ClypseErrorCodes.TextFileNotText"/>), so
    /// it is refused rather than opened, and damaged when it is saved again.</exception>
    DecodedText Decode(byte[] content);

    /// <summary>
    /// Turns an editor's text into a file's content, as UTF-8.
    /// </summary>
    /// <param name="text">The text, with plain line breaks.</param>
    /// <param name="original">What the file had when it was read: its byte order mark and line endings are put back.</param>
    /// <returns>The file's content.</returns>
    byte[] Encode(string text, DecodedText original);
}
