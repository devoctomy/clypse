using Clypse.Portal.Models.Vaults;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Keeps a report on the open vault's secrets (<see cref="IVaultSession"/>): weak passwords, reused passwords, reused
/// user names that are not email addresses, and passwords not updated for a while
/// (<see cref="IVaultReportPreferencesService"/>). It is made again once the secrets have loaded, whenever one is added,
/// changed, deleted or synced, and when the preferences change. Closing or locking the vault empties it.
/// </summary>
public interface IVaultReportService
{
    /// <summary>
    /// Raised when <see cref="Report"/> or <see cref="IsUpdating"/> changes.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Gets the latest report: <see cref="VaultReport.Empty"/> when no vault is open.
    /// </summary>
    VaultReport Report { get; }

    /// <summary>
    /// Gets a value indicating whether the report is being made, or waits for the vault's secrets to load.
    /// </summary>
    bool IsUpdating { get; }

    /// <summary>
    /// Makes the report again from the open vault's secrets now.
    /// </summary>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when the report is made, or another has replaced it.</returns>
    Task RefreshAsync(CancellationToken ct = default);
}
