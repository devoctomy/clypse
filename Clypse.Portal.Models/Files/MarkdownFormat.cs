namespace Clypse.Portal.Models.Files;

/// <summary>
/// Formatting that the text editor's toolbar can apply to the selected text of a markdown document.
/// </summary>
public enum MarkdownFormat
{
    /// <summary>
    /// Bold (<c>**text**</c>).
    /// </summary>
    Bold,

    /// <summary>
    /// Italic (<c>_text_</c>).
    /// </summary>
    Italic,

    /// <summary>
    /// Strikethrough (<c>~~text~~</c>).
    /// </summary>
    Strikethrough,

    /// <summary>
    /// Inline code (<c>`text`</c>).
    /// </summary>
    InlineCode,

    /// <summary>
    /// Level 1 heading (<c># </c>).
    /// </summary>
    Heading1,

    /// <summary>
    /// Level 2 heading (<c>## </c>).
    /// </summary>
    Heading2,

    /// <summary>
    /// Level 3 heading (<c>### </c>).
    /// </summary>
    Heading3,

    /// <summary>
    /// Bulleted list (<c>- </c>).
    /// </summary>
    BulletList,

    /// <summary>
    /// Numbered list (<c>1. </c>).
    /// </summary>
    NumberedList,

    /// <summary>
    /// Task list (<c>- [ ] </c>).
    /// </summary>
    TaskList,

    /// <summary>
    /// Block quote (<c>&gt; </c>).
    /// </summary>
    Quote,

    /// <summary>
    /// Fenced code block.
    /// </summary>
    CodeBlock,

    /// <summary>
    /// Link (<c>[text](url)</c>).
    /// </summary>
    Link,

    /// <summary>
    /// Horizontal rule (<c>---</c>).
    /// </summary>
    HorizontalRule,
}
