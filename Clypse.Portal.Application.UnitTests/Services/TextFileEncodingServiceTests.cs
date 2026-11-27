using System.Text;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Models.Files;

namespace Clypse.Portal.Application.UnitTests.Services;

public class TextFileEncodingServiceTests
{
    private static readonly byte[] ByteOrderMark = [0xEF, 0xBB, 0xBF];

    private readonly TextFileEncodingService sut = new();

    [Fact]
    public void GivenPlainUtf8Text_WhenDecoded_ThenItIsTheText_WithNothingToPutBack()
    {
        // Act
        var decoded = sut.Decode(Encoding.UTF8.GetBytes("milk\neggs\ncafé"));

        // Assert
        Assert.Equal("milk\neggs\ncafé", decoded.Text);
        Assert.False(decoded.HasByteOrderMark);
        Assert.False(decoded.UsesWindowsLineEndings);
    }

    [Fact]
    public void GivenAByteOrderMarkAndWindowsLineEndings_WhenDecoded_ThenTheTextHasNeither_AndBothAreRemembered()
    {
        // Arrange
        byte[] content = [.. ByteOrderMark, .. Encoding.UTF8.GetBytes("one\r\ntwo\r\n")];

        // Act
        var decoded = sut.Decode(content);

        // Assert
        Assert.Equal("one\ntwo\n", decoded.Text);
        Assert.True(decoded.HasByteOrderMark);
        Assert.True(decoded.UsesWindowsLineEndings);
    }

    [Fact]
    public void GivenNoContent_WhenDecoded_ThenTheTextIsEmpty()
    {
        // Act
        var decoded = sut.Decode([]);

        // Assert
        Assert.Equal(string.Empty, decoded.Text);
        Assert.False(decoded.HasByteOrderMark);
        Assert.False(decoded.UsesWindowsLineEndings);
    }

    [Fact]
    public void GivenContentThatIsNotText_WhenDecoded_ThenItIsRefused()
    {
        // Act
        var ex = Assert.Throws<ClypseException>(() => sut.Decode([0xFF, 0xFE, 0x00, 0xC3, 0x28]));

        // Assert
        Assert.Equal(ClypseErrorCodes.TextFileNotText, ex.Code);
        Assert.IsType<DecoderFallbackException>(ex.InnerException);
    }

    [Fact]
    public void GivenTextFromAPlainFile_WhenEncoded_ThenItIsUtf8_WithNoMarkAndPlainLineBreaks()
    {
        // Act
        var content = sut.Encode("one\ntwo\ncafé", new DecodedText());

        // Assert
        Assert.Equal(Encoding.UTF8.GetBytes("one\ntwo\ncafé"), content);
    }

    [Theory]
    [InlineData(true, false, "﻿one\ntwo")]
    [InlineData(false, true, "one\r\ntwo")]
    [InlineData(true, true, "﻿one\r\ntwo")]
    public void GivenWhatTheFileHad_WhenItsTextIsEncoded_ThenItIsPutBack(bool hasByteOrderMark, bool usesWindowsLineEndings, string expected)
    {
        // Arrange
        var original = new DecodedText { HasByteOrderMark = hasByteOrderMark, UsesWindowsLineEndings = usesWindowsLineEndings };

        // Act
        var content = sut.Encode("one\ntwo", original);

        // Assert
        Assert.Equal(Encoding.UTF8.GetBytes(expected), content);
    }

    [Fact]
    public void GivenAFilesContent_WhenDecodedAndEncodedAgainUnchanged_ThenItIsTheSameBytes()
    {
        // Arrange
        byte[] content = [.. ByteOrderMark, .. Encoding.UTF8.GetBytes("# Title\r\n\r\nSome **bold** text.\r\n")];

        // Act
        var decoded = sut.Decode(content);
        var encoded = sut.Encode(decoded.Text, decoded);

        // Assert
        Assert.Equal(content, encoded);
    }

    [Fact]
    public void GivenNullArguments_WhenUsed_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("content", Assert.Throws<ArgumentNullException>(() => sut.Decode(null!)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => sut.Encode(null!, new DecodedText())).ParamName);
        Assert.Equal("original", Assert.Throws<ArgumentNullException>(() => sut.Encode("text", null!)).ParamName);
    }
}
