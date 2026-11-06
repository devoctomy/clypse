using System.Text.Json;
using Clypse.Core.Base;
using Clypse.Core.Base.Exceptions;
using Clypse.Core.Extensions;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Sync.Interfaces;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="ISecretService"/> over a vault's file store, which encrypts and decrypts with the
/// vault's own keys. A secret's file holds <see cref="ClypseObject.Data"/>, as clypse has always stored secrets.
/// </summary>
/// <param name="vault">The vault.</param>
/// <param name="fileStore">The vault's file store.</param>
/// <param name="timeProvider">The clock, for when a secret was last updated.</param>
public class SecretService(Vault vault, ISyncFileStore fileStore, TimeProvider timeProvider)
    : ISecretService
{
    /// <summary>
    /// The folder, under a vault's root, holding its secrets.
    /// </summary>
    public const string SecretsFolder = "secrets/";

    /// <summary>
    /// The extension of a secret's file.
    /// </summary>
    public const string SecretExtension = ".json";

    private readonly Vault vault = vault ?? throw new ArgumentNullException(nameof(vault));
    private readonly ISyncFileStore fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
    private readonly TimeProvider timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private string SecretsRoot => vault.Root + SecretsFolder;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ListIdsAsync(CancellationToken ct = default)
    {
        var files = await fileStore.ListAsync(ct);
        return files
            .Where(f => !f.IsFolder)
            .Select(f => GetSecretId(f.Key))
            .OfType<string>()
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<Secret?> GetAsync(string id, CancellationToken ct = default)
    {
        var file = await fileStore.ReadAsync(KeyOf(id), ct);
        if (file == null)
        {
            return null;
        }

        Secret? secret;
        try
        {
            secret = JsonSerializer.Deserialize<Secret>(file.Content);
            secret = secret?.CastSecretToCorrectType();
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or NotSupportedException or FormatException)
        {
            throw new ClypseException(ClypseErrorCodes.SecretUnreadable, ex);
        }

        if (secret == null || secret.Id != id)
        {
            throw new ClypseException(ClypseErrorCodes.SecretUnreadable);
        }

        return secret;
    }

    /// <inheritdoc/>
    public async Task<Secret> SaveAsync(Secret secret, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(secret);

        var now = timeProvider.GetUtcNow();
        secret.LastUpdatedAt = now.UtcDateTime;
        var key = KeyOf(secret.Id);
        Validate(secret);
        await fileStore.WriteAsync(key, Serialize(secret), now, ct);
        return secret;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        return fileStore.DeleteAsync(KeyOf(id), ct);
    }

    /// <inheritdoc/>
    public async Task<int> ImportAsync(IReadOnlyList<Secret> secrets, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(secrets);

        // Everything is checked before anything is written, so a bad import changes nothing.
        foreach (var secret in secrets)
        {
            KeyOf(secret.Id);
            Validate(secret);
        }

        var written = 0;
        foreach (var secret in secrets)
        {
            ct.ThrowIfCancellationRequested();
            await SaveAsync(secret, ct);
            written++;
            progress?.Report(written);
        }

        return written;
    }

    /// <inheritdoc/>
    public string? GetSecretId(string key)
    {
        if (key == null
            || !key.StartsWith(SecretsRoot, StringComparison.Ordinal)
            || !key.EndsWith(SecretExtension, StringComparison.Ordinal))
        {
            return null;
        }

        var id = key[SecretsRoot.Length..^SecretExtension.Length];
        return IsValidId(id) ? id : null;
    }

    private static bool IsValidId(string? id) => Guid.TryParseExact(id, "D", out _);

    private static byte[] Serialize(Secret secret) => JsonSerializer.SerializeToUtf8Bytes(secret);

    private static void Validate(Secret secret)
    {
        try
        {
            new ClypseObjectValidator(secret).Validate();
        }
        catch (ClypseObjectValidatorException ex)
        {
            throw new ClypseException(ClypseErrorCodes.SecretInvalid, ex, string.Join(", ", ex.MissingProperties));
        }
    }

    private string KeyOf(string id)
    {
        // Only a GUID may name a file: it keeps a secret's name out of its path, and a path out of its id.
        if (!IsValidId(id))
        {
            throw new ClypseException(ClypseErrorCodes.SecretIdInvalid);
        }

        return SecretsRoot + id + SecretExtension;
    }
}
