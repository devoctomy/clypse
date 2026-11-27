using System.Text.RegularExpressions;
using Clypse.Portal.Application.Services;

namespace Clypse.Portal.Application.UnitTests.Services;

public class MarkdownRendererTests
{
    private readonly MarkdownRenderer sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenNoMarkdown_WhenRendered_ThenTheHtmlIsEmpty(string? markdown)
    {
        // Act
        var html = sut.ToHtml(markdown);

        // Assert
        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void GivenMarkdown_WhenRendered_ThenItIsHtml()
    {
        // Act
        var html = sut.ToHtml("# Title\n\nSome **bold** and _italic_ and ~~struck~~ text.");

        // Assert
        Assert.Contains(">Title</h1>", html, StringComparison.Ordinal);
        Assert.Contains("<strong>bold</strong>", html, StringComparison.Ordinal);
        Assert.Contains("<em>italic</em>", html, StringComparison.Ordinal);
        Assert.Contains("<del>struck</del>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenATableAndATaskList_WhenRendered_ThenBothAreRendered()
    {
        // Act
        var html = sut.ToHtml("| a | b |\n| - | - |\n| 1 | 2 |\n\n- [x] done\n- [ ] todo");

        // Assert
        Assert.Contains("<table>", html, StringComparison.Ordinal);
        Assert.Contains("type=\"checkbox\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenRawHtml_WhenRendered_ThenItIsNotPassedThrough()
    {
        // Act
        var html = sut.ToHtml("<script>alert(1)</script>\n\nHello <b onmouseover=\"x()\">there</b>");

        // Assert
        Assert.DoesNotContain("<script>", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<b ", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("[click](javascript:alert(1))")]
    [InlineData("[click](JavaScript:alert(1))")]
    [InlineData("[click](java\tscript:alert(1))")]
    [InlineData("[click](data:text/html;base64,PHNjcmlwdD4=)")]
    [InlineData("[click](vbscript:msgbox)")]
    [InlineData("<javascript:alert(1)>")]
    [InlineData("[click](<java\u0001script:alert(1)>)")]
    [InlineData("![picture](data:image/svg+xml;base64,PHN2Zz4=)")]
    public void GivenAnUnsafeLink_WhenRendered_ThenItPointsNowhere(string markdown)
    {
        // Act
        var html = sut.ToHtml(markdown);

        // Assert
        Assert.DoesNotMatch(new Regex("(href|src)=\"(?!#\")", RegexOptions.IgnoreCase), html);
    }

    [Theory]
    [InlineData("[site](https://example.com)", "href=\"https://example.com\"")]
    [InlineData("[mail](mailto:someone@example.com)", "href=\"mailto:someone@example.com\"")]
    [InlineData("[section](#heading)", "href=\"#heading\"")]
    [InlineData("[page](other/page.md)", "href=\"other/page.md\"")]
    [InlineData("[query](page?time=10:30)", "href=\"page?time=10:30\"")]
    [InlineData("[empty]()", "href=\"\"")]
    public void GivenASafeLink_WhenRendered_ThenItIsKept(string markdown, string expected)
    {
        // Act
        var html = sut.ToHtml(markdown);

        // Assert
        Assert.Contains(expected, html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("![x](https://example.com/x.png){onerror=alert(1)}")]
    [InlineData("[click](https://example.com){onclick=alert(1)}")]
    [InlineData("# Title {onmouseover=alert(1)}")]
    [InlineData("Some text\n{onclick=alert(1)}")]
    public void GivenGenericAttributes_WhenRendered_ThenNoEventHandlerIsRendered(string markdown)
    {
        // Act
        var html = sut.ToHtml(markdown);

        // Assert
        Assert.DoesNotMatch(new Regex(@"<[^>]*\son\w+\s*=", RegexOptions.IgnoreCase), html);
    }
}
