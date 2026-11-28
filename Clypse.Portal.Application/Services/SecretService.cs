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
        return file == null ? null : Read(id, file.Content);
    }

    /// <inheritdoc/>
    public Secret Read(string id, byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        Secret? secret;
        try
        {
            secret = JsonSerializer.Deserialize<Secret>(content);
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

        KeyOf(secret.Id);
        Validate(secret);
        return await WriteAsync(secret, await ReadStoredAsync(secret.Id, ct), ct);
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

            await WriteImportedAsync(secret, ct);
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

    /// <inheritdoc/>
    public string GetKey(string id) => KeyOf(id);

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

    // A new or changed password was updated now. An unchanged one keeps its time; one saved before that time was
    // recorded takes the stored secret's last update, the latest it can have changed.
    private static void StampPassword(Secret secret, Secret? stored, DateTime now)
    {
        const string PasswordKey = nameof(WebSecret.Password);
        const string UpdatedKey = nameof(WebSecret.PasswordUpdatedAt);
        var password = secret.GetData(PasswordKey);
        if (string.IsNullOrEmpty(password))
        {
            secret.SetData(UpdatedKey, null);
        }
        else if (stored != null && string.Equals(password, stored.GetData(PasswordKey), StringComparison.Ordinal))
        {
            secret.SetData(UpdatedKey, stored.GetData(UpdatedKey) ?? stored.GetData(nameof(Secret.LastUpdatedAt)));
        }
        else
        {
            secret.SetData(UpdatedKey, now.ToString(ClypseObject.DateFormat, System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    // An unreadable stored secret is overwritten as if there were none.
    private async Task<Secret?> ReadStoredAsync(string id, CancellationToken ct)
    {
        try
        {
            return await GetAsync(id, ct);
        }
        catch (ClypseException ex) when (ex.Code == ClypseErrorCodes.SecretUnreadable)
        {
            return null;
        }
    }

    // Imported secrets are new, and keep the times they bring: a password without a time of its own was set when the
    // secret was last updated.
    private async Task WriteImportedAsync(Secret secret, CancellationToken ct)
    {
        const string UpdatedKey = nameof(WebSecret.PasswordUpdatedAt);
        if (string.IsNullOrEmpty(secret.GetData(nameof(WebSecret.Password))))
        {
            secret.SetData(UpdatedKey, null);
        }
        else if (secret.GetData(UpdatedKey) == null)
        {
            secret.SetData(UpdatedKey, secret.GetData(nameof(Secret.LastUpdatedAt)));
        }

        await fileStore.WriteAsync(KeyOf(secret.Id), Serialize(secret), timeProvider.GetUtcNow(), ct);
    }

    private async Task<Secret> WriteAsync(Secret secret, Secret? stored, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        StampPassword(secret, stored, now.UtcDateTime);
        secret.LastUpdatedAt = now.UtcDateTime;
        await fileStore.WriteAsync(KeyOf(secret.Id), Serialize(secret), now, ct);
        return secret;
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
