using Clypse.Portal.Models.Files;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// What kind of file a vault's file is: its content type, how it is shown in the app, and how the list describes it.
/// </summary>
public interface IFileTypeService
{
    /// <summary>
    /// Gets the content type to record for a file being added: what the browser reported, or one worked out from the
    /// name's extension when it reported nothing useful.
    /// </summary>
    /// <param name="name">The file's name.</param>
    /// <param name="reported">The content type the browser reported, if any.</param>
    /// <returns>The content type, e.g. <c>image/jpeg</c>.</returns>
    string GetContentType(string? name, string? reported);

    /// <summary>
    /// Gets how a file is shown in the app. Only these kinds are ever rendered.
    /// </summary>
    /// <param name="file">The file's metadata.</param>
    /// <returns>How it is shown.</returns>
    FileViewKind GetViewKind(VaultFileEntry file);

    /// <summary>
    /// Gets the icon of a file's type.
    /// </summary>
    /// <param name="file">The file's metadata.</param>
    /// <returns>The Bootstrap icon's class.</returns>
    string GetIcon(VaultFileEntry file);

    /// <summary>
    /// Gets what a file's type is called, e.g. "Image".
    /// </summary>
    /// <param name="file">The file's metadata.</param>
    /// <returns>The type's name, in the user's language.</returns>
    string GetTypeName(VaultFileEntry file);

    /// <summary>
    /// Describes a size, e.g. "2.4 MB".
    /// </summary>
    /// <param name="bytes">The size, in bytes.</param>
    /// <returns>The size as text.</returns>
    string FormatSize(long bytes);
}
