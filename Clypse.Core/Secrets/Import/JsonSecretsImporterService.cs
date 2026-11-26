using System.Globalization;
using System.Text.Json;
using Clypse.Core.Base;

namespace Clypse.Core.Secrets.Import;

/// <summary>
/// Implementation of <see cref="IJsonSecretsImporterService"/>.
/// </summary>
public class JsonSecretsImporterService : IJsonSecretsImporterService
{
    private static readonly HashSet<string> DateFields = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(ClypseObject.CreatedAt),
        nameof(ClypseObject.LastUpdatedAt),
        nameof(WebSecret.PasswordUpdatedAt),
    };

    private static readonly HashSet<string> Fields = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(Secret.SecretType),
        nameof(Secret.Name),
        nameof(Secret.Description),
        nameof(Secret.Tags),
        nameof(Secret.Comments),
        nameof(WebSecret.UserName),
        nameof(WebSecret.EmailAddress),
        nameof(WebSecret.WebsiteUrl),
        nameof(WebSecret.LoginUrl),
        nameof(WebSecret.Password),
        nameof(AwsCredentials.AccessKeyId),
        nameof(AwsCredentials.SecretAccessKey),
        nameof(ClypseObject.CreatedAt),
        nameof(ClypseObject.LastUpdatedAt),
        nameof(WebSecret.PasswordUpdatedAt),
    };

    /// <inheritdoc/>
    public IReadOnlyList<Dictionary<string, string>> Read(string data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var rows = JsonSerializer.Deserialize<List<Dictionary<string, string>?>>(data)
            ?? throw new JsonException("The file holds no secrets.");
        return rows.Select(Keep).ToList();
    }

    private static Dictionary<string, string> Keep(Dictionary<string, string>? row)
    {
        if (row == null)
        {
            throw new JsonException("A secret is null.");
        }

        var kept = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in row.Where(r => Fields.Contains(r.Key) && !string.IsNullOrEmpty(r.Value)))
        {
            if (DateFields.Contains(key) && !DateTime.TryParseExact(value, ClypseObject.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                throw new FormatException($"'{key}' is not in the format {ClypseObject.DateFormat}.");
            }

            kept[key] = value;
        }

        return kept;
    }
}
