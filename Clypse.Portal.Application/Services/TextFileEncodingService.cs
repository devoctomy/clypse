using System.Text;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Files;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="ITextFileEncodingService"/>.
/// </summary>
public class TextFileEncodingService : ITextFileEncodingService
{
    private const char ByteOrderMark = '﻿';
    private const string WindowsLineEnding = "\r\n";
    private const string LineEnding = "\n";

    // Strict, so content that is not text is refused rather than read with its bad bytes replaced.
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    /// <inheritdoc/>
    public DecodedText Decode(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        string fileText;
        try
        {
            fileText = StrictUtf8.GetString(content);
        }
        catch (DecoderFallbackException ex)
        {
            throw new ClypseException(ClypseErrorCodes.TextFileNotText, ex);
        }

        var hasByteOrderMark = fileText.Length > 0 && fileText[0] == ByteOrderMark;
        var withoutMark = hasByteOrderMark ? fileText[1..] : fileText;
        return new DecodedText
        {
            Text = withoutMark.Replace(WindowsLineEnding, LineEnding, StringComparison.Ordinal),
            HasByteOrderMark = hasByteOrderMark,
            UsesWindowsLineEndings = fileText.Contains(WindowsLineEnding, StringComparison.Ordinal),
        };
    }

    /// <inheritdoc/>
    public byte[] Encode(string text, DecodedText original)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(original);

        var fileText = original.UsesWindowsLineEndings ? text.Replace(LineEnding, WindowsLineEnding, StringComparison.Ordinal) : text;
        return Encoding.UTF8.GetBytes(original.HasByteOrderMark ? ByteOrderMark + fileText : fileText);
    }
}
