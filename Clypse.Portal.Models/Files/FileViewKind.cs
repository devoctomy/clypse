namespace Clypse.Portal.Models.Files;

/// <summary>
/// How a file is shown in the app.
/// </summary>
public enum FileViewKind
{
    /// <summary>
    /// It cannot be shown in the app: only its details are.
    /// </summary>
    None,

    /// <summary>
    /// An image, shown in the viewer.
    /// </summary>
    Image,

    /// <summary>
    /// Plain text, opened in the editor.
    /// </summary>
    Text,

    /// <summary>
    /// Markdown, opened in the editor with its preview.
    /// </summary>
    Markdown,
}
