using System.Text.Json;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Encryption;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.Configuration;
using Devoctomy.Blazwiz.Sync.Encryption.Interfaces;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IVaultService"/>. Every protected profile the user creates is a vault (clypse defines
/// no profiles in code). A vault's files are under <c>profiles/{id}/</c>, and its name and description are in
/// <see cref="DetailsFile"/> there, encrypted like its secrets. Whenever the name is read (or set), it is kept as the
/// profile's label on this device, so the vault is shown by name while locked too.
/// </summary>
/// <param name="profiles">The user's protected profiles.</param>
/// <param name="manager">Says which are unlocked, and locks them.</param>
/// <param name="registry">The sync profiles, for each vault's file store.</param>
/// <param name="passkeys">Asks for the passkey when its key is needed, and says whether there is one here.</param>
public class VaultService(
    ISyncProtectedProfileService profiles,
    IE2eProtectedProfileManager manager,
    ISyncProfileRegistry registry,
    IPasskeyKeysService passkeys)
    : IVaultService
{
    /// <summary>
    /// The file in a vault's root holding its name and description.
    /// </summary>
    public const string DetailsFile = ".vault";

    private readonly ISyncProtectedProfileService profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
    private readonly IE2eProtectedProfileManager manager = manager ?? throw new ArgumentNullException(nameof(manager));
    private readonly IPasskeyKeysService passkeys = passkeys ?? throw new ArgumentNullException(nameof(passkeys));
    private readonly ISyncProfileRegistry registry = registry ?? throw new ArgumentNullException(nameof(registry));

    /// <summary>
    /// Gets the key every file of a vault is under.
    /// </summary>
    /// <param name="id">The vault's id.</param>
    /// <returns>The key, e.g. <c>profiles/{id}/</c>.</returns>
    public static string RootOf(string id) => $"{SyncDefaults.ProtectedProfilesFolder}/{id}/";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Vault>> GetVaultsAsync(CancellationToken ct = default)
    {
        var vaults = new List<Vault>();
        foreach (var profile in profiles.Profiles.Where(p => p.IsCreatedByUser))
        {
            vaults.Add(await DescribeAsync(profile, ct));
        }

        return vaults;
    }

    /// <inheritdoc/>
    public async Task<Vault?> FindAsync(string id, CancellationToken ct = default)
    {
        var profile = FindProfile(id);
        return profile == null ? null : await DescribeAsync(profile, ct);
    }

    /// <inheritdoc/>
    public async Task<Vault> CreateAsync(
        string name,
        string? description,
        string passphrase,
        IProgress<E2eProgress>? progress = null,
        CancellationToken ct = default)
    {
        var details = ValidDetails(name, description);
        var scope = await profiles.CreateAsync(passphrase, progress, ct);
        await WriteDetailsAsync(scope.Id, details, ct);
        await profiles.SetLabelAsync(scope, details.Name);
        return await FindAsync(scope.Id, ct)
            ?? throw new InvalidOperationException($"Vault {scope.Id} was created but is not listed.");
    }

    /// <inheritdoc/>
    public async Task<Vault> UpdateAsync(string id, string name, string? description, CancellationToken ct = default)
    {
        var profile = FindProfile(id) ?? throw new ClypseException(ClypseErrorCodes.VaultNotFound);
        var details = ValidDetails(name, description);
        await WriteDetailsAsync(id, details, ct);
        await profiles.SetLabelAsync(profile.Scope, details.Name);
        return await DescribeAsync(profile, ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var profile = FindProfile(id) ?? throw new ClypseException(ClypseErrorCodes.VaultNotFound);
        await profiles.DeleteAsync(profile.Scope, ct);
    }

    /// <inheritdoc/>
    public async Task<bool> UnlockWithBiometricsAsync(string id)
    {
        var profile = FindProfile(id) ?? throw new ClypseException(ClypseErrorCodes.VaultNotFound);
        try
        {
            if (!await passkeys.EnsureKeyAsync(E2eKeyNames.KeyEncryptionKey))
            {
                return false;
            }

            await profiles.UnlockWithDeviceAsync(profile.Scope);
            return true;
        }
        catch (Exception)
        {
            // E.g. another passkey than the one it was turned on with: the passphrase unlocks it.
            return false;
        }
    }

    /// <inheritdoc/>
    public Task ForgetBiometricsAsync(string id)
    {
        var profile = FindProfile(id) ?? throw new ClypseException(ClypseErrorCodes.VaultNotFound);
        return profiles.DisableDeviceUnlockAsync(profile.Scope);
    }

    /// <inheritdoc/>
    public void Lock(string id)
    {
        var profile = FindProfile(id) ?? throw new ClypseException(ClypseErrorCodes.VaultNotFound);
        manager.Lock(profile.Scope);
    }

    private static VaultDetails ValidDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ClypseException(ClypseErrorCodes.VaultNameRequired);
        }

        return new VaultDetails
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
        };
    }

    private SyncProtectedProfile? FindProfile(string id)
    {
        return profiles.Profiles.FirstOrDefault(p => p.IsCreatedByUser && p.Name == id);
    }

    private async Task WriteDetailsAsync(string id, VaultDetails details, CancellationToken ct)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(details);
        await registry.GetFileStore(id).WriteAsync(RootOf(id) + DetailsFile, json, ct: ct);
    }

    private async Task<Vault> DescribeAsync(SyncProtectedProfile profile, CancellationToken ct)
    {
        var vault = new Vault
        {
            Id = profile.Scope.Id,
            Root = RootOf(profile.Scope.Id),
            IsUnlocked = manager.IsUnlocked(profile.Scope),
        };

        var details = vault.IsUnlocked ? await ReadDetailsAsync(profile.Name, vault.Root, ct) : null;
        if (details != null && details.Name != profile.Label)
        {
            await profiles.SetLabelAsync(profile.Scope, details.Name);
        }

        vault.Name = details?.Name ?? profile.Label ?? vault.Id;
        vault.Description = details?.Description;
        vault.CanUnlockWithBiometrics = !vault.IsUnlocked && await CanUnlockWithBiometricsAsync(profile);
        return vault;
    }

    private async Task<bool> CanUnlockWithBiometricsAsync(SyncProtectedProfile profile)
    {
        try
        {
            return await profiles.HasDeviceUnlockAsync(profile.Scope) && await passkeys.IsAvailableAsync();
        }
        catch (Exception)
        {
            // This device's copy of its keys cannot be read: the passphrase still unlocks it.
            return false;
        }
    }

    private async Task<VaultDetails?> ReadDetailsAsync(string profileName, string root, CancellationToken ct)
    {
        try
        {
            var file = await registry.GetFileStore(profileName).ReadAsync(root + DetailsFile, ct);
            return file == null ? null : JsonSerializer.Deserialize<VaultDetails>(file.Content);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // Not synced here yet, or locked meanwhile: shown by its label or id until it can be read.
            return null;
        }
    }
}
