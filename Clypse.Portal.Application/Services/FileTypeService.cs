using System.Globalization;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Files;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IFileTypeService"/>.
/// </summary>
/// <param name="text">Clypse's text, for the names of types and sizes.</param>
public class FileTypeService(IStringLocalizer<ClypseText> text)
    : IFileTypeService
{
    /// <summary>
    /// The content type of markdown.
    /// </summary>
    public const string MarkdownType = "text/markdown";

    /// <summary>
    /// The content type of plain text.
    /// </summary>
    public const string TextType = "text/plain";

    /// <summary>
    /// The content type of a PDF.
    /// </summary>
    public const string PdfType = "application/pdf";

    private const double BytesPerUnit = 1024;

    // The images a browser shows in an <img>. SVG is left out: it is a document, not a picture.
    private static readonly HashSet<string> ImageTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
    };

    private static readonly Dictionary<string, string> TypesByExtension = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".txt"] = TextType,
        [".md"] = MarkdownType,
        [".markdown"] = MarkdownType,
        [".pdf"] = PdfType,
    };

    private static readonly string[] SizeUnits = ["Files.Size.Kilobytes", "Files.Size.Megabytes", "Files.Size.Gigabytes"];

    private readonly IStringLocalizer<ClypseText> text = text ?? throw new ArgumentNullException(nameof(text));

    /// <inheritdoc/>
    public string GetContentType(string? name, string? reported)
    {
        var type = Clean(reported);
        if (type.Length > 0 && !IsUnknown(type))
        {
            return type;
        }

        return TypesByExtension.GetValueOrDefault(Path.GetExtension(name ?? string.Empty), VaultFileService.UnknownContentType);
    }

    /// <inheritdoc/>
    public FileViewKind GetViewKind(VaultFileEntry file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var type = Clean(file.ContentType);
        if (ImageTypes.Contains(type))
        {
            return FileViewKind.Image;
        }

        // Browsers often report markdown as plain text, or as nothing at all: then its name says what it is.
        var byName = GetContentType(file.Name, null);
        var isTextual = type.Length == 0 || IsUnknown(type) || IsType(type, TextType);
        if (IsType(type, MarkdownType) || (isTextual && IsType(byName, MarkdownType)))
        {
            return FileViewKind.Markdown;
        }

        return IsType(type, TextType) || (isTextual && IsType(byName, TextType)) ? FileViewKind.Text : FileViewKind.None;
    }

    /// <inheritdoc/>
    public string GetIcon(VaultFileEntry file)
    {
        return GetViewKind(file) switch
        {
            FileViewKind.Image => "bi-file-earmark-image",
            FileViewKind.Markdown => "bi-file-earmark-richtext",
            FileViewKind.Text => "bi-file-earmark-text",
            _ => IsPdf(file) ? "bi-file-earmark-pdf" : "bi-file-earmark",
        };
    }

    /// <inheritdoc/>
    public string GetTypeName(VaultFileEntry file)
    {
        return GetViewKind(file) switch
        {
            FileViewKind.Image => text["Files.Type.Image"],
            FileViewKind.Markdown => text["Files.Type.Markdown"],
            FileViewKind.Text => text["Files.Type.Text"],
            _ => IsPdf(file) ? text["Files.Type.Pdf"] : text["Files.Type.Other"],
        };
    }

    /// <inheritdoc/>
    public string FormatSize(long bytes)
    {
        if (bytes < BytesPerUnit)
        {
            return text["Files.Size.Bytes", Math.Max(bytes, 0).ToString(CultureInfo.CurrentCulture)];
        }

        var value = bytes / BytesPerUnit;
        var unit = 0;
        while (value >= BytesPerUnit && unit < SizeUnits.Length - 1)
        {
            value /= BytesPerUnit;
            unit++;
        }

        return text[SizeUnits[unit], value.ToString("0.#", CultureInfo.CurrentCulture)];
    }

    // "Text/Plain; charset=utf-8" is "text/plain".
    private static string Clean(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType)
            ? string.Empty
            : contentType.Split(';')[0].Trim().ToLowerInvariant();
    }

    private static bool IsType(string type, string expected) => string.Equals(type, expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsUnknown(string type) => IsType(type, VaultFileService.UnknownContentType);

    private bool IsPdf(VaultFileEntry file)
    {
        var type = Clean(file.ContentType);
        return IsType(type, PdfType) || ((type.Length == 0 || IsUnknown(type)) && IsType(GetContentType(file.Name, null), PdfType));
    }
}
