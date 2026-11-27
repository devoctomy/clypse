using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Files;
using Devoctomy.Blazwiz.Testing;

namespace Clypse.Portal.Application.UnitTests.Services;

/// <summary>
/// Tests use « and » to mark the selection before and after formatting, e.g. "say «hello»" selects "hello".
/// A caret with nothing selected is written as «».
/// </summary>
public class MarkdownFormatServiceTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly MarkdownFormatService sut;

    public MarkdownFormatServiceTests()
    {
        sut = new MarkdownFormatService(text.For<ClypseText>());
    }

    [Fact]
    public void GivenNoText_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new MarkdownFormatService(null!)).ParamName);
    }

    [Theory]
    [InlineData("say «hello» there", "say **«hello»** there")]
    [InlineData("say «hello »there", "say **«hello»** there")]
    [InlineData("say **«hello»** there", "say «hello» there")]
    [InlineData("say «**hello**» there", "say «hello» there")]
    [InlineData("say «»", "say **«bold text»**")]
    public void GivenASelection_WhenBold_ThenTheMarkersAreToggled(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.Bold);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("«word»", "_«word»_")]
    [InlineData("_«word»_", "«word»")]
    [InlineData("«»", "_«italic text»_")]
    public void GivenASelection_WhenItalic_ThenTheUnderscoresAreToggled(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.Italic);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("«old»", "~~«old»~~")]
    [InlineData("~~«old»~~", "«old»")]
    [InlineData("«»", "~~«strikethrough text»~~")]
    public void GivenASelection_WhenStrikethrough_ThenTheTildesAreToggled(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.Strikethrough);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("run «dotnet test»", "run `«dotnet test»`")]
    [InlineData("run `«dotnet test»`", "run «dotnet test»")]
    [InlineData("run «»", "run `«code»`")]
    public void GivenASelection_WhenInlineCode_ThenTheBackticksAreToggled(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.InlineCode);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Ti«»tle", MarkdownFormat.Heading1, "# Title«»")]
    [InlineData("# Ti«»tle", MarkdownFormat.Heading1, "Title«»")]
    [InlineData("# Ti«»tle", MarkdownFormat.Heading2, "## Title«»")]
    [InlineData("### Ti«»tle", MarkdownFormat.Heading3, "Title«»")]
    [InlineData("before\nTi«»tle\nafter", MarkdownFormat.Heading2, "before\n## Title«»\nafter")]
    [InlineData("###### Ti«»tle", MarkdownFormat.Heading1, "# Title«»")]
    [InlineData("#\tTi«»tle", MarkdownFormat.Heading1, "Title«»")]
    [InlineData("#Ti«»tle", MarkdownFormat.Heading1, "# #Title«»")]
    [InlineData("####### Ti«»tle", MarkdownFormat.Heading1, "# ####### Title«»")]
    public void GivenACaretInALine_WhenHeading_ThenTheWholeLinesLevelIsSetOrRemoved(string before, MarkdownFormat format, string expected)
    {
        // Act
        var result = Format(before, format);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("«one\ntwo»", "«- one\n- two»")]
    [InlineData("«- one\n- two»", "«one\ntwo»")]
    [InlineData("«one\n\ntwo»", "«- one\n\n- two»")]
    [InlineData("o«n»e", "«- one»")]
    [InlineData("«1. one\n2. two»", "«- one\n- two»")]
    [InlineData("«  indented»", "«  - indented»")]
    [InlineData("«* one\n+ two»", "«one\ntwo»")]
    [InlineData("«  - one\n  - two»", "«  one\n  two»")]
    [InlineData("«- [ ] milk»", "«- milk»")]
    [InlineData("«-one»", "«- -one»")]
    [InlineData("«one\n»two", "«- one»\ntwo")]
    public void GivenLines_WhenBulletList_ThenTheBulletsAreToggled(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.BulletList);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("«one\ntwo\nthree»", "«1. one\n2. two\n3. three»")]
    [InlineData("«1. one\n2. two»", "«one\ntwo»")]
    [InlineData("«- one\n- two»", "«1. one\n2. two»")]
    [InlineData("«1) one\n2) two»", "«one\ntwo»")]
    [InlineData("«10. ten»", "«ten»")]
    [InlineData("«- [x] done»", "«1. done»")]
    [InlineData("«1.one»", "«1. 1.one»")]
    public void GivenLines_WhenNumberedList_ThenEachLineIsNumbered(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.NumberedList);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("«milk\neggs»", "«- [ ] milk\n- [ ] eggs»")]
    [InlineData("«- [ ] milk\n- [x] eggs»", "«milk\neggs»")]
    [InlineData("«- milk»", "«- [ ] milk»")]
    [InlineData("«- [X] done»", "«done»")]
    [InlineData("«* [x] done»", "«done»")]
    [InlineData("«1. milk»", "«- [ ] milk»")]
    [InlineData("«- [] milk»", "«- [ ] [] milk»")]
    public void GivenLines_WhenTaskList_ThenTheCheckboxesAreToggled(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.TaskList);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("«wise\nwords»", "«> wise\n> words»")]
    [InlineData("«> wise\n> words»", "«wise\nwords»")]
    [InlineData("«>wise»", "«wise»")]
    [InlineData("«>> nested»", "«> nested»")]
    [InlineData("«> wise\nwords»", "«> > wise\n> words»")]
    [InlineData("«> wise\n\n> words»", "«wise\n\nwords»")]
    [InlineData("«»", "> «»")]
    public void GivenLines_WhenQuote_ThenTheQuoteIsToggled(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.Quote);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("«var x = 1;»", "```\n«var x = 1;»\n```")]
    [InlineData("text «»", "text \n```\n«code»\n```")]
    [InlineData("«```\nvar x = 1;\n```»", "«var x = 1;»")]
    [InlineData("«```csharp\nvar x = 1;\n```»", "«var x = 1;»")]
    [InlineData("«```\none\n\ntwo\n```  »", "«one\n\ntwo»")]
    [InlineData("«```\nvar x = 1;```»", "«var x = 1;»")]
    [InlineData("«```\n```»", "«»")]
    [InlineData("«```\nunclosed»", "```\n«```\nunclosed»\n```")]
    [InlineData("«```x```»", "```\n«```x```»\n```")]
    [InlineData("«var x = 1;»\nmore", "```\n«var x = 1;»\n```\nmore")]
    [InlineData("«var x = 1;» more", "```\n«var x = 1;»\n```\n more")]
    public void GivenASelection_WhenCodeBlock_ThenTheFenceIsToggled(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.CodeBlock);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("see «docs»", "see [docs](«https://»)")]
    [InlineData("see «https://example.com»", "see [«link text»](https://example.com)")]
    [InlineData("see «http://example.com»", "see [«link text»](http://example.com)")]
    [InlineData("see «HTTPS://example.com»", "see [«link text»](HTTPS://example.com)")]
    [InlineData("see «»", "see [«link text»](https://)")]
    public void GivenASelection_WhenLink_ThenALinkIsPutIn_WithThePartToTypeSelected(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.Link);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("«»", "---\n\n«»")]
    [InlineData("above«»", "above\n\n---\n\n«»")]
    [InlineData("above\n«»", "above\n\n---\n\n«»")]
    [InlineData("above\n\n«»\nbelow", "above\n\n---\n«»\nbelow")]
    public void GivenACaret_WhenHorizontalRule_ThenTheRuleIsPutOnItsOwnLine(string before, string expected)
    {
        // Act
        var result = Format(before, MarkdownFormat.HorizontalRule);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GivenLinesWithABlankLineBetween_WhenHeading_ThenTheBlankLineIsLeftAlone()
    {
        // Act
        var added = Format("«one\n\ntwo»", MarkdownFormat.Heading1);
        var removed = Format("«# one\n\n# two»", MarkdownFormat.Heading1);

        // Assert
        Assert.Equal("«# one\n\n# two»", added);
        Assert.Equal("«one\n\ntwo»", removed);
    }

    [Fact]
    public void GivenAnEmptyDocument_WhenBulletList_ThenNothingIsAdded()
    {
        // Act
        var result = Format("«»", MarkdownFormat.BulletList);

        // Assert
        Assert.Equal("«»", result);
    }

    [Fact]
    public void GivenAReversedOrOutOfRangeSelection_WhenApplied_ThenTheSelectionIsPutInOrder()
    {
        // Act
        var edit = sut.Apply("hello", 99, 0, MarkdownFormat.Bold);

        // Assert
        Assert.Equal("**hello**", edit.ApplyTo("hello"));
        Assert.Equal(2, edit.SelectionStart);
        Assert.Equal(7, edit.SelectionEnd);
    }

    [Fact]
    public void GivenAnUnknownFormat_WhenApplied_ThenThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Equal("format", Assert.Throws<ArgumentOutOfRangeException>(() => sut.Apply("text", 0, 0, (MarkdownFormat)999)).ParamName);
    }

    [Fact]
    public void GivenNoText_WhenApplied_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Apply(null!, 0, 0, MarkdownFormat.Bold));
    }

    private string Format(string markedText, MarkdownFormat style)
    {
        var start = markedText.IndexOf('«');
        var end = markedText.IndexOf('»') - 1;
        var plain = markedText.Replace("«", string.Empty, StringComparison.Ordinal).Replace("»", string.Empty, StringComparison.Ordinal);

        var edit = sut.Apply(plain, start, end, style);
        var result = edit.ApplyTo(plain);

        return result[..edit.SelectionStart] + "«" + result[edit.SelectionStart..edit.SelectionEnd] + "»" + result[edit.SelectionEnd..];
    }
}
