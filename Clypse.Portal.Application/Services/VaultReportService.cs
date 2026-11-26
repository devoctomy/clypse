using System.Globalization;
using System.Net.Mail;
using Clypse.Core.Base;
using Clypse.Core.Enums;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Vaults;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IVaultReportService"/>. Each password's strength is estimated once and kept while it is
/// in the vault, so making the report again after a change only estimates new passwords. A report started while
/// another is being made replaces it.
/// </summary>
public sealed class VaultReportService : IVaultReportService, IDisposable
{
    private readonly IVaultSession session;
    private readonly IPasswordStrengthService strengthService;
    private readonly IVaultReportPreferencesService preferencesService;
    private readonly TimeProvider timeProvider;
    private readonly Lock gate = new Lock();
    private Dictionary<string, PasswordStrength> strengths = new Dictionary<string, PasswordStrength>(StringComparer.Ordinal);
    private string? strengthsVaultId;
    private int generation;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultReportService"/> class, and makes the first report.
    /// </summary>
    /// <param name="session">The open vault and its secrets.</param>
    /// <param name="strengthService">Estimates how strong the passwords are.</param>
    /// <param name="preferencesService">Says when a password counts as old.</param>
    /// <param name="timeProvider">The clock, for how old passwords are.</param>
    public VaultReportService(
        IVaultSession session,
        IPasswordStrengthService strengthService,
        IVaultReportPreferencesService preferencesService,
        TimeProvider timeProvider)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.strengthService = strengthService ?? throw new ArgumentNullException(nameof(strengthService));
        this.preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.session.Changed += OnChanged;
        this.preferencesService.Changed += OnChanged;
        OnChanged(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public VaultReport Report { get; private set; } = VaultReport.Empty;

    /// <inheritdoc/>
    public bool IsUpdating { get; private set; }

    /// <inheritdoc/>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var run = Interlocked.Increment(ref generation);
        var vault = session.Vault;
        if (vault == null)
        {
            ForgetStrengths(null);
            Publish(run, VaultReport.Empty, false);
            return;
        }

        if (session.IsLoading)
        {
            // Made once the secrets have loaded: the session says so when they have.
            Publish(run, Report.VaultId == vault.Id ? Report : new VaultReport { VaultId = vault.Id }, true);
            return;
        }

        ForgetStrengths(vault.Id);
        Publish(run, Report.VaultId == vault.Id ? Report : new VaultReport { VaultId = vault.Id }, true);
        try
        {
            var report = await MakeAsync(run, vault.Id, session.Secrets, ct);
            if (report != null)
            {
                Publish(run, report, false);
            }
        }
        catch (Exception)
        {
            // Cancelled, or a password could not be estimated: the last report stands.
            Publish(run, Report, false);
            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        session.Changed -= OnChanged;
        preferencesService.Changed -= OnChanged;
        Interlocked.Increment(ref generation);
        ForgetStrengths(null);
    }

    private static bool IsWeak(PasswordStrength strength) =>
        strength.IsKnownWeak || strength.Estimation is PasswordComplexityEstimation.VeryWeak or PasswordComplexityEstimation.Weak;

    private static bool IsEmailAddress(string value) =>
        MailAddress.TryCreate(value, out var address) && string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);

    // A secret saved before the password's own time was recorded has its last update: the latest it can have changed.
    // Null if neither can be read; such a password is never counted as old.
    private static DateTime? PasswordUpdatedAt(Secret secret) =>
        ParseDate(secret.GetData(nameof(WebSecret.PasswordUpdatedAt))) ?? ParseDate(secret.GetData(nameof(Secret.LastUpdatedAt)));

    private static DateTime? ParseDate(string? value) =>
        value != null && DateTime.TryParseExact(value, ClypseObject.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var at)
            ? at
            : null;

    // The values come in the secrets' name order, and so do the secrets in each group; the most used come first.
    private static List<VaultReportReuse> FindReused(IEnumerable<(Secret Secret, string Value)> values, IEqualityComparer<string> comparer) => values
        .GroupBy(v => v.Value, comparer)
        .Where(g => g.Count() > 1)
        .Select(g => new VaultReportReuse { SecretIds = g.Select(v => v.Secret.Id).ToList() })
        .OrderByDescending(g => g.SecretIds.Count)
        .ToList();

    // Null if another report replaced this one while it was made.
    private async Task<VaultReport?> MakeAsync(int run, string vaultId, IReadOnlyList<Secret> secrets, CancellationToken ct)
    {
        var preferences = await preferencesService.GetAsync();
        var now = timeProvider.GetUtcNow();
        var oldBefore = now.UtcDateTime.AddMonths(-preferences.OldPasswordMonths);
        var byName = secrets
            .OrderBy(s => s.Name ?? string.Empty, StringComparer.Create(CultureInfo.CurrentCulture, CompareOptions.IgnoreCase))
            .ToList();

        var passwords = byName
            .Select(s => (Secret: s, Value: s.GetData(nameof(WebSecret.Password))))
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => (p.Secret, Value: p.Value!))
            .ToList();
        var userNames = byName
            .Select(s => (Secret: s, Value: s.GetData(nameof(WebSecret.UserName))?.Trim()))
            .Where(u => !string.IsNullOrEmpty(u.Value) && !IsEmailAddress(u.Value))
            .Select(u => (u.Secret, Value: u.Value!))
            .ToList();

        var estimated = new Dictionary<string, PasswordStrength>(StringComparer.Ordinal);
        foreach (var password in passwords.Select(p => p.Value).Distinct(StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            var strength = KnownStrength(vaultId, password);
            if (strength == null)
            {
                strength = await strengthService.EstimateCheckingKnownPasswordsAsync(password, ct);
                if (Volatile.Read(ref generation) != run)
                {
                    return null;
                }

                KeepStrength(vaultId, password, strength);
            }

            estimated[password] = strength;
        }

        if (Volatile.Read(ref generation) != run)
        {
            return null;
        }

        // Only the passwords still in the vault are kept.
        lock (gate)
        {
            if (strengthsVaultId == vaultId)
            {
                strengths = estimated;
            }
        }

        return new VaultReport
        {
            VaultId = vaultId,
            GeneratedAt = now,
            SecretCount = secrets.Count,
            OldPasswordMonths = preferences.OldPasswordMonths,
            WeakPasswords = passwords
                .Where(p => IsWeak(estimated[p.Value]))
                .Select(p => new VaultReportWeakPassword { SecretId = p.Secret.Id, Strength = estimated[p.Value] })
                .ToList(),
            ReusedUserNames = FindReused(userNames, StringComparer.OrdinalIgnoreCase),
            ReusedPasswords = FindReused(passwords, StringComparer.Ordinal),
            OldPasswords = passwords
                .Select(p => (p.Secret, At: PasswordUpdatedAt(p.Secret) ?? DateTime.MaxValue))
                .Where(p => p.At < oldBefore)
                .OrderBy(p => p.At)
                .Select(p => new VaultReportOldPassword { SecretId = p.Secret.Id, PasswordUpdatedAt = p.At })
                .ToList(),
        };
    }

    private PasswordStrength? KnownStrength(string vaultId, string password)
    {
        lock (gate)
        {
            return strengthsVaultId == vaultId && strengths.TryGetValue(password, out var strength) ? strength : null;
        }
    }

    private void KeepStrength(string vaultId, string password, PasswordStrength strength)
    {
        lock (gate)
        {
            if (strengthsVaultId == vaultId)
            {
                strengths[password] = strength;
            }
        }
    }

    // Another vault (or none) open: the passwords estimated for the last one are forgotten.
    private void ForgetStrengths(string? vaultId)
    {
        lock (gate)
        {
            if (strengthsVaultId != vaultId)
            {
                strengths = new Dictionary<string, PasswordStrength>(StringComparer.Ordinal);
                strengthsVaultId = vaultId;
            }
        }
    }

    private void Publish(int run, VaultReport report, bool isUpdating)
    {
        if (Volatile.Read(ref generation) != run || (ReferenceEquals(report, Report) && isUpdating == IsUpdating))
        {
            return;
        }

        Report = report;
        IsUpdating = isUpdating;

        // Raised through a copy of the handler, not ?.Invoke, which SonarC# S3264 does not count as raising it.
        var changed = Changed;
        if (changed != null)
        {
            changed(this, EventArgs.Empty);
        }
    }

    private async void OnChanged(object? sender, EventArgs e)
    {
        try
        {
            await RefreshAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // Event handlers must not throw. The report is made again on the next change.
        }
    }
}
