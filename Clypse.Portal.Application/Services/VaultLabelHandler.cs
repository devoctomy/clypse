using Clypse.Portal.Application.Services.Interfaces;
using Devoctomy.Blazwiz.Auth.Models;
using Devoctomy.Blazwiz.Auth.Sessions.Interfaces;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Labels a vault on this device with its name whenever it is unlocked, wherever that happens (its page, Settings or
/// the passkey), so it is shown by name everywhere on this device from then on, even while locked.
/// </summary>
/// <param name="manager">Says when a vault is unlocked.</param>
/// <param name="vaultService">Reads the vault's name, and labels it.</param>
public sealed class VaultLabelHandler(IE2eProtectedProfileManager manager, IVaultService vaultService)
    : ISessionLifecycleHandler, IDisposable
{
    private readonly IE2eProtectedProfileManager manager = manager ?? throw new ArgumentNullException(nameof(manager));
    private readonly IVaultService vaultService = vaultService ?? throw new ArgumentNullException(nameof(vaultService));
    private bool listening;

    /// <inheritdoc/>
    public Task OnSessionStartedAsync(UserSession session)
    {
        if (!listening)
        {
            listening = true;
            manager.LockStateChanged += OnLockStateChanged;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnSigningOutAsync()
    {
        Stop();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Stop();
    }

    private void Stop()
    {
        if (listening)
        {
            listening = false;
            manager.LockStateChanged -= OnLockStateChanged;
        }
    }

    private async void OnLockStateChanged(object? sender, E2eKeyScope scope)
    {
        if (!manager.IsUnlocked(scope))
        {
            return;
        }

        try
        {
            // Finding an unlocked vault reads its name, and labels it.
            await vaultService.FindAsync(scope.Id);
        }
        catch (Exception)
        {
            // Labelled again the next time it is unlocked or listed.
        }
    }
}
