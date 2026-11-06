using System.Globalization;
using Blazing.Mvvm.ComponentModel;
using Clypse.Core.Enums;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Vaults;
using CommunityToolkit.Mvvm.Input;
using Devoctomy.Blazwiz.Core.Text.Interfaces;
using Devoctomy.Blazwiz.UI.Text.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for a vault's page: its secrets, read into the <see cref="IVaultSession"/> once it is unlocked, with search
/// and a tag filter, and which dialog is open (the secret editor or import). Leaving the page closes the session, so the
/// decrypted secrets are not kept in memory.
/// </summary>
public partial class VaultViewModel : ViewModelBase
{
    private readonly IVaultService vaultService;
    private readonly IVaultSession session;
    private readonly IErrorText errorText;
    private readonly ITextPlurals plurals;
    private readonly IStringLocalizer<ClypseText> text;

    private string? vaultId;
    private bool isMissing;
    private string search = string.Empty;
    private string? selectedTag;
    private VaultDialog activeDialog;
    private string? editingSecretId;
    private string? errorMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultViewModel"/> class.
    /// </summary>
    /// <param name="vaultService">Finds and locks the vault.</param>
    /// <param name="session">Holds the vault's secrets.</param>
    /// <param name="errorText">Describes errors.</param>
    /// <param name="plurals">Words counts.</param>
    /// <param name="text">Clypse's text.</param>
    public VaultViewModel(
        IVaultService vaultService,
        IVaultSession session,
        IErrorText errorText,
        ITextPlurals plurals,
        IStringLocalizer<ClypseText> text)
    {
        this.vaultService = vaultService ?? throw new ArgumentNullException(nameof(vaultService));
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
        this.plurals = plurals ?? throw new ArgumentNullException(nameof(plurals));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
        this.session.Changed += OnSessionChanged;
    }

    /// <summary>
    /// The dialogs the page can show.
    /// </summary>
    public enum VaultDialog
    {
        /// <summary>
        /// No dialog.
        /// </summary>
        None,

        /// <summary>
        /// The secret editor, for a new secret or <see cref="EditingSecretId"/>.
        /// </summary>
        Editor,

        /// <summary>
        /// Import from a CSV file.
        /// </summary>
        Import,
    }

    /// <summary>
    /// Gets the vault, or null while it is found (or if it does not exist).
    /// </summary>
    public Vault? Vault => session.Vault?.Id == vaultId ? session.Vault : null;

    /// <summary>
    /// Gets a value indicating whether the vault does not exist.
    /// </summary>
    public bool IsMissing
    {
        get => isMissing;
        private set => SetProperty(ref isMissing, value);
    }

    /// <summary>
    /// Gets a value indicating whether the vault is unlocked.
    /// </summary>
    public bool IsUnlocked => Vault?.IsUnlocked == true;

    /// <summary>
    /// Gets the page's title: the vault's name.
    /// </summary>
    public string Title => Vault?.Name ?? text["Vault.Title"];

    /// <summary>
    /// Gets the page's subtitle: the vault's description, if it has one.
    /// </summary>
    public string Subtitle => string.IsNullOrWhiteSpace(Vault?.Description) ? text["Vault.Subtitle"] : Vault.Description;

    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    public string Search
    {
        get => search;
        set
        {
            if (SetProperty(ref search, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(Secrets));
            }
        }
    }

    /// <summary>
    /// Gets the tag the secrets are filtered by, or null for all.
    /// </summary>
    public string? SelectedTag
    {
        get => selectedTag;
        private set
        {
            if (SetProperty(ref selectedTag, value))
            {
                OnPropertyChanged(nameof(Secrets));
            }
        }
    }

    /// <summary>
    /// Gets every tag used in the vault, in order.
    /// </summary>
    public IReadOnlyList<string> Tags => session.Secrets
        .SelectMany(s => s.Tags)
        .Select(t => t.Trim())
        .Where(t => t.Length > 0)
        .Distinct(StringComparer.CurrentCultureIgnoreCase)
        .Order(StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    /// <summary>
    /// Gets the secrets that match the search and the tag, by name.
    /// </summary>
    public IReadOnlyList<Secret> Secrets => session.Secrets
        .Where(Matches)
        .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    /// <summary>
    /// Gets how many secrets the vault has, read so far.
    /// </summary>
    public int SecretCount => session.Secrets.Count;

    /// <summary>
    /// Gets a value indicating whether the secrets are being read.
    /// </summary>
    public bool IsLoading => session.IsLoading;

    /// <summary>
    /// Gets how far through reading the secrets is, from 0 to 100.
    /// </summary>
    public int LoadingPercent => session.TotalCount == 0 ? 0 : session.LoadedCount * 100 / session.TotalCount;

    /// <summary>
    /// Gets what reading the secrets is doing, e.g. "Decrypting 12 of 40 secrets...".
    /// </summary>
    public string LoadingText => plurals.Format(text, "Vault.Loading", session.TotalCount, session.LoadedCount);

    /// <summary>
    /// Gets how many secrets could not be read, as the warning says it, or null if they all could.
    /// </summary>
    public string? UnreadableText => session.UnreadableCount == 0
        ? null
        : plurals.Format(text, "Vault.Unreadable", session.UnreadableCount);

    /// <summary>
    /// Gets how many secrets the vault has, as the page says it.
    /// </summary>
    public string CountText => plurals.Format(text, "Vault.Count", session.Secrets.Count);

    /// <summary>
    /// Gets the open dialog.
    /// </summary>
    public VaultDialog ActiveDialog
    {
        get => activeDialog;
        private set => SetProperty(ref activeDialog, value);
    }

    /// <summary>
    /// Gets the id of the secret the editor is open for, or null for a new secret.
    /// </summary>
    public string? EditingSecretId
    {
        get => editingSecretId;
        private set => SetProperty(ref editingSecretId, value);
    }

    /// <summary>
    /// Gets an error to show, or null.
    /// </summary>
    public string? ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    /// <summary>
    /// Gets the icon of a kind of secret.
    /// </summary>
    /// <param name="type">The kind of secret.</param>
    /// <returns>The Bootstrap icon's class.</returns>
    public static string GetIcon(SecretType type) => type switch
    {
        SecretType.Web => "bi-globe2",
        SecretType.Aws => "bi-cloud-fill",
        _ => "bi-key-fill",
    };

    /// <summary>
    /// Gets the line shown under a secret's name: who it is for, or where.
    /// </summary>
    /// <param name="secret">The secret.</param>
    /// <returns>The line, or null.</returns>
    public static string? GetSummary(Secret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        return secret switch
        {
            WebSecret web => FirstOf(web.UserName, web.EmailAddress, HostOf(web.WebsiteUrl), web.Description),
            AwsCredentials aws => FirstOf(aws.AccessKeyId, aws.Description),
            _ => secret.Description,
        };
    }

    /// <summary>
    /// Opens a vault: finds it, and reads its secrets once it is unlocked.
    /// </summary>
    /// <param name="id">The vault's id.</param>
    /// <returns>A <see cref="Task"/> that completes once the vault is found; its secrets are read in the background.</returns>
    public async Task LoadAsync(string id)
    {
        if (id == vaultId && (Vault != null || IsMissing))
        {
            return;
        }

        vaultId = id;
        ActiveDialog = VaultDialog.None;
        Search = string.Empty;
        SelectedTag = null;
        ErrorMessage = null;
        session.Close();

        var vault = await vaultService.FindAsync(id);
        IsMissing = vault == null;
        if (vault != null)
        {
            _ = OpenAsync(vault);
        }

        RaiseVaultChanged();
    }

    /// <summary>
    /// Filters the secrets by a tag, or shows all.
    /// </summary>
    /// <param name="tag">The tag, or null for all.</param>
    [RelayCommand]
    public void SelectTag(string? tag)
    {
        SelectedTag = tag == null || string.Equals(tag, SelectedTag, StringComparison.CurrentCultureIgnoreCase) ? null : tag;
    }

    /// <summary>
    /// Locks the vault: its keys are wiped, and its secrets forgotten.
    /// </summary>
    [RelayCommand]
    public void Lock()
    {
        if (Vault == null)
        {
            return;
        }

        ActiveDialog = VaultDialog.None;
        vaultService.Lock(Vault.Id);
    }

    /// <summary>
    /// Opens the editor for a new secret.
    /// </summary>
    [RelayCommand]
    public void NewSecret()
    {
        EditingSecretId = null;
        ActiveDialog = VaultDialog.Editor;
    }

    /// <summary>
    /// Opens the editor for a secret.
    /// </summary>
    /// <param name="id">The secret's id.</param>
    [RelayCommand]
    public void EditSecret(string id)
    {
        EditingSecretId = id;
        ActiveDialog = VaultDialog.Editor;
    }

    /// <summary>
    /// Opens the import dialog.
    /// </summary>
    [RelayCommand]
    public void ShowImport()
    {
        ActiveDialog = VaultDialog.Import;
    }

    /// <summary>
    /// Closes the open dialog.
    /// </summary>
    [RelayCommand]
    public void CloseDialog()
    {
        ActiveDialog = VaultDialog.None;
        EditingSecretId = null;
    }

    /// <summary>
    /// Hides the error.
    /// </summary>
    [RelayCommand]
    public void DismissError()
    {
        ErrorMessage = null;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            session.Changed -= OnSessionChanged;
            session.Close();
        }

        base.Dispose(disposing);
    }

    private static string? FirstOf(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? HostOf(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) || Uri.TryCreate("https://" + url, UriKind.Absolute, out uri)
            ? uri.Host
            : url;
    }

    private static bool Contains(string? value, string search) =>
        value != null && CultureInfo.CurrentCulture.CompareInfo.IndexOf(value, search, CompareOptions.IgnoreCase) >= 0;

    private bool Matches(Secret secret)
    {
        if (SelectedTag != null && !secret.Tags.Any(t => string.Equals(t.Trim(), SelectedTag, StringComparison.CurrentCultureIgnoreCase)))
        {
            return false;
        }

        var term = Search.Trim();
        if (term.Length == 0)
        {
            return true;
        }

        return Contains(secret.Name, term)
            || Contains(secret.Description, term)
            || secret.Tags.Any(t => Contains(t, term))
            || (secret is WebSecret web && (Contains(web.UserName, term) || Contains(web.EmailAddress, term) || Contains(web.WebsiteUrl, term)));
    }

    private async Task OpenAsync(Vault vault)
    {
        try
        {
            await session.OpenAsync(vault);
        }
        catch (Exception ex)
        {
            ErrorMessage = errorText.Describe(ex);
        }
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        if (SelectedTag != null && !Tags.Contains(SelectedTag, StringComparer.CurrentCultureIgnoreCase))
        {
            selectedTag = null;
        }

        if (!IsUnlocked && ActiveDialog != VaultDialog.None)
        {
            // Locked (by hand, on idle or in another tab) while a dialog was open: nothing in it can be saved now.
            CloseDialog();
        }

        RaiseVaultChanged();
    }

    private void RaiseVaultChanged()
    {
        OnPropertyChanged(nameof(Vault));
        OnPropertyChanged(nameof(IsUnlocked));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(Tags));
        OnPropertyChanged(nameof(SelectedTag));
        OnPropertyChanged(nameof(Secrets));
        OnPropertyChanged(nameof(SecretCount));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(LoadingPercent));
        OnPropertyChanged(nameof(LoadingText));
        OnPropertyChanged(nameof(UnreadableText));
        OnPropertyChanged(nameof(CountText));
    }
}
