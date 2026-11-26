using System.Globalization;
using Blazing.Mvvm.ComponentModel;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Vaults;
using CommunityToolkit.Mvvm.Input;
using Devoctomy.Blazwiz.Core.Text.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for a vault's report dialog: each check of the open vault's report (<see cref="IVaultReportService"/>),
/// worded, with what it found. A check's section opens to show the secrets; every section starts closed. The report
/// is shown again whenever it is made again, e.g. after a secret is saved.
/// </summary>
public partial class VaultReportViewModel : ViewModelBase
{
    private readonly IVaultReportService reportService;
    private readonly IVaultSession session;
    private readonly IPasswordStrengthService strengthService;
    private readonly ITextPlurals plurals;
    private readonly IStringLocalizer<ClypseText> text;
    private readonly HashSet<VaultReportCheck> expanded = [];
    private IReadOnlyList<VaultReportSection> sections = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultReportViewModel"/> class.
    /// </summary>
    /// <param name="reportService">Keeps the report.</param>
    /// <param name="session">The open vault's secrets, to name them.</param>
    /// <param name="strengthService">Words how strong a weak password is.</param>
    /// <param name="plurals">Words counts.</param>
    /// <param name="text">Clypse's text.</param>
    public VaultReportViewModel(
        IVaultReportService reportService,
        IVaultSession session,
        IPasswordStrengthService strengthService,
        ITextPlurals plurals,
        IStringLocalizer<ClypseText> text)
    {
        this.reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.strengthService = strengthService ?? throw new ArgumentNullException(nameof(strengthService));
        this.plurals = plurals ?? throw new ArgumentNullException(nameof(plurals));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
        this.reportService.Changed += OnReportChanged;
        Build();
    }

    /// <summary>
    /// Gets a value indicating whether there is a report to show: false until the first is made.
    /// </summary>
    public bool IsReady => reportService.Report.GeneratedAt != null;

    /// <summary>
    /// Gets a value indicating whether the report is being made again.
    /// </summary>
    public bool IsUpdating => reportService.IsUpdating;

    /// <summary>
    /// Gets the checks, in order.
    /// </summary>
    public IReadOnlyList<VaultReportSection> Sections
    {
        get => sections;
        private set => SetProperty(ref sections, value);
    }

    /// <summary>
    /// Gets how many checks passed, as the dialog says it, e.g. "3 of 5 checks passed".
    /// </summary>
    public string PassedText => text["VaultReport.Passed", Sections.Count(s => s.Passed), Sections.Count];

    /// <summary>
    /// Gets how many secrets were checked, as the dialog says it.
    /// </summary>
    public string CheckedText => plurals.Format(text, "VaultReport.Checked", reportService.Report.SecretCount);

    /// <summary>
    /// Gets a value indicating whether every check passed.
    /// </summary>
    public bool AllPassed => Sections.All(s => s.Passed);

    /// <summary>
    /// Gets whether a check's section is open.
    /// </summary>
    /// <param name="check">The check.</param>
    /// <returns>True if it is open.</returns>
    public bool IsExpanded(VaultReportCheck check) => expanded.Contains(check);

    /// <summary>
    /// Opens a check's section, or closes it if it is open.
    /// </summary>
    /// <param name="check">The check.</param>
    [RelayCommand]
    public void Toggle(VaultReportCheck check)
    {
        if (!expanded.Remove(check))
        {
            expanded.Add(check);
        }

        OnPropertyChanged(nameof(Sections));
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            reportService.Changed -= OnReportChanged;
        }

        base.Dispose(disposing);
    }

    private static string? UserNameOf(Secret? secret) => secret?.GetData(nameof(WebSecret.UserName))?.Trim();

    private void OnReportChanged(object? sender, EventArgs e) => Build();

    private void Build()
    {
        var report = reportService.Report;
        var secrets = session.Secrets.ToDictionary(s => s.Id, StringComparer.Ordinal);
        IReadOnlyList<VaultReportSecret> Named(IEnumerable<string> ids) => ids
            .Where(secrets.ContainsKey)
            .Select(id => new VaultReportSecret { Id = id, Name = secrets[id].Name ?? string.Empty })
            .ToList();

        // Nothing more to say about each known weak password, so they are shown together.
        var knownIds = report.WeakPasswords.Where(w => w.Strength.IsKnownWeak).Select(w => w.SecretId).ToList();
        VaultReportItem[] known = knownIds.Count == 0 ? [] : [new VaultReportItem { Secrets = Named(knownIds) }];
        var weak = report.WeakPasswords
            .Where(w => !w.Strength.IsKnownWeak)
            .Select(w => new VaultReportItem { Detail = strengthService.Describe(w.Strength), Secrets = Named([w.SecretId]) });
        var reusedPasswords = report.ReusedPasswords
            .Select(r => new VaultReportItem { Heading = plurals.Format(text, "VaultReport.UsedBy", r.SecretIds.Count), Secrets = Named(r.SecretIds) });
        var reusedUserNames = report.ReusedUserNames
            .Select(r => new VaultReportItem
            {
                Heading = UserNameOf(secrets.GetValueOrDefault(r.SecretIds[0])),
                Detail = plurals.Format(text, "VaultReport.UsedBy", r.SecretIds.Count),
                Secrets = Named(r.SecretIds),
            });
        var old = report.OldPasswords
            .Select(o => new VaultReportItem
            {
                Detail = text["VaultReport.OldPasswords.Detail", DateTime.SpecifyKind(o.PasswordUpdatedAt, DateTimeKind.Utc).ToLocalTime().ToString("d", CultureInfo.CurrentCulture)],
                Secrets = Named([o.SecretId]),
            });

        var months = plurals.Format(text, "VaultReportSettings.Months", report.OldPasswordMonths);
        Sections =
        [
            Section(VaultReportCheck.KnownWeakPasswords, known),
            Section(VaultReportCheck.WeakPasswords, weak),
            Section(VaultReportCheck.ReusedPasswords, reusedPasswords),
            Section(VaultReportCheck.ReusedUserNames, reusedUserNames),
            Section(VaultReportCheck.OldPasswords, old, months),
        ];
        OnPropertyChanged(nameof(IsReady));
        OnPropertyChanged(nameof(IsUpdating));
        OnPropertyChanged(nameof(PassedText));
        OnPropertyChanged(nameof(CheckedText));
        OnPropertyChanged(nameof(AllPassed));
    }

    // Items naming no secret still in the vault (deleted since the report was made) are left out.
    private VaultReportSection Section(VaultReportCheck check, IEnumerable<VaultReportItem> found, string? months = null)
    {
        var items = found.Where(i => i.Secrets.Count > 0).ToList();
        var key = "VaultReport." + check;

        // The reuse checks count the passwords or user names reused; the others, the secrets.
        var count = check is VaultReportCheck.ReusedPasswords or VaultReportCheck.ReusedUserNames
            ? items.Count
            : items.Sum(i => i.Secrets.Count);
        return new VaultReportSection
        {
            Check = check,
            Title = months == null ? text[key + ".Title"] : text[key + ".Title", months],
            Summary = items.Count == 0 ? text[key + ".Passed"] : plurals.Format(text, key + ".Found", count),
            About = months == null ? text[key + ".About"] : text[key + ".About", months],
            Items = items,
        };
    }
}
