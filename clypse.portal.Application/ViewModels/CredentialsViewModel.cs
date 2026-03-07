using Blazing.Mvvm.ComponentModel;
using clypse.core.Enums;
using clypse.core.Secrets;
using clypse.core.Vault;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels.Messages;
using clypse.portal.Models.Enums;
using clypse.portal.Models.Import;
using clypse.portal.Models.Vault;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the credentials component, managing secret CRUD operations.
/// </summary>
public partial class CredentialsViewModel : ViewModelBase,
    IRecipient<ShowCreateCredentialMessage>,
    IRecipient<ShowImportMessage>
{
    private readonly IVaultStateService vaultStateService;
    private readonly IMessenger messenger;

    private bool showImportDialog;
    private bool isLoadingSecret;
    private bool isSavingSecret;
    private bool showDeleteConfirmation;
    private bool isDeletingSecret;
    private string secretIdToDelete = string.Empty;
    private string deleteConfirmationMessage = string.Empty;
    private bool showSecretDialog;
    private Secret? currentSecret;
    private CrudDialogMode secretDialogMode = CrudDialogMode.Create;
    private string searchTerm = string.Empty;
    private List<VaultIndexEntry> filteredEntries = [];

    /// <summary>
    /// Initializes a new instance of <see cref="CredentialsViewModel"/>.
    /// </summary>
    /// <param name="vaultStateService">The vault state service.</param>
    /// <param name="messenger">The messenger used for cross-component communication.</param>
    public CredentialsViewModel(IVaultStateService vaultStateService, IMessenger messenger)
    {
        this.vaultStateService = vaultStateService ?? throw new ArgumentNullException(nameof(vaultStateService));
        this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        this.messenger.Register<ShowCreateCredentialMessage>(this);
        this.messenger.Register<ShowImportMessage>(this);
        this.vaultStateService.VaultStateChanged += OnVaultStateChanged;
    }

    /// <summary>Gets a value indicating whether the import dialog is shown.</summary>
    public bool ShowImportDialog { get => showImportDialog; private set => SetProperty(ref showImportDialog, value); }

    /// <summary>Gets a value indicating whether a secret is being loaded.</summary>
    public bool IsLoadingSecret { get => isLoadingSecret; private set => SetProperty(ref isLoadingSecret, value); }

    /// <summary>Gets a value indicating whether a secret is being saved.</summary>
    public bool IsSavingSecret { get => isSavingSecret; private set => SetProperty(ref isSavingSecret, value); }

    /// <summary>Gets a value indicating whether delete confirmation is shown.</summary>
    public bool ShowDeleteConfirmation { get => showDeleteConfirmation; private set => SetProperty(ref showDeleteConfirmation, value); }

    /// <summary>Gets a value indicating whether a secret is being deleted.</summary>
    public bool IsDeletingSecret { get => isDeletingSecret; private set => SetProperty(ref isDeletingSecret, value); }

    /// <summary>Gets the delete confirmation message.</summary>
    public string DeleteConfirmationMessage { get => deleteConfirmationMessage; private set => SetProperty(ref deleteConfirmationMessage, value); }

    /// <summary>Gets a value indicating whether the secret dialog is shown.</summary>
    public bool ShowSecretDialog { get => showSecretDialog; private set => SetProperty(ref showSecretDialog, value); }

    /// <summary>Gets the secret currently being viewed or edited.</summary>
    public Secret? CurrentSecret { get => currentSecret; private set => SetProperty(ref currentSecret, value); }

    /// <summary>Gets the mode for the secret dialog.</summary>
    public CrudDialogMode SecretDialogMode { get => secretDialogMode; private set => SetProperty(ref secretDialogMode, value); }

    /// <summary>Gets or sets the current search term.</summary>
    public string SearchTerm
    {
        get => searchTerm;
        set
        {
            SetProperty(ref searchTerm, value);
            HandleSearch();
        }
    }

    /// <summary>Gets the filtered list of vault index entries.</summary>
    public List<VaultIndexEntry> FilteredEntries { get => filteredEntries; private set => SetProperty(ref filteredEntries, value); }

    /// <summary>Gets the current vault metadata from vault state.</summary>
    public VaultMetadata? CurrentVault => vaultStateService.CurrentVault;

    /// <inheritdoc/>
    public override Task OnInitializedAsync()
    {
        RefreshFilteredEntries();
        return Task.CompletedTask;
    }

    /// <summary>Handles the search input change.</summary>
    public void HandleSearch()
    {
        if (vaultStateService.CurrentVault?.IndexEntries == null)
        {
            FilteredEntries = [];
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            FilteredEntries = vaultStateService.CurrentVault.IndexEntries
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return;
        }

        var term = SearchTerm.Trim().ToLower();
        FilteredEntries = vaultStateService.CurrentVault.IndexEntries
            .Where(entry =>
                (entry.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (entry.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (entry.Tags?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Clears the search filter.</summary>
    [RelayCommand]
    public void ClearSearch()
    {
        SearchTerm = string.Empty;
    }

    /// <summary>Opens the secret dialog to view a secret.</summary>
    /// <param name="secretId">The ID of the secret to view.</param>
    [RelayCommand]
    public async Task ViewSecretAsync(string secretId)
    {
        if (vaultStateService.VaultManager == null || vaultStateService.LoadedVault == null || string.IsNullOrEmpty(vaultStateService.CurrentVaultKey))
        {
            return;
        }

        try
        {
            IsLoadingSecret = true;
            var secret = await vaultStateService.VaultManager.GetSecretAsync(vaultStateService.LoadedVault, secretId, vaultStateService.CurrentVaultKey, CancellationToken.None);

            if (secret != null)
            {
                CurrentSecret = secret;
                SecretDialogMode = CrudDialogMode.View;
                ShowSecretDialog = true;
            }
        }
        catch
        {
            // Error handling via loading state
        }
        finally
        {
            IsLoadingSecret = false;
        }
    }

    /// <summary>Opens the secret dialog to edit a secret.</summary>
    /// <param name="secretId">The ID of the secret to edit.</param>
    [RelayCommand]
    public async Task EditSecretAsync(string secretId)
    {
        if (vaultStateService.VaultManager == null || vaultStateService.LoadedVault == null || string.IsNullOrEmpty(vaultStateService.CurrentVaultKey))
        {
            return;
        }

        try
        {
            IsLoadingSecret = true;
            var secret = await vaultStateService.VaultManager.GetSecretAsync(vaultStateService.LoadedVault, secretId, vaultStateService.CurrentVaultKey, CancellationToken.None);

            if (secret != null)
            {
                CurrentSecret = secret;
                SecretDialogMode = CrudDialogMode.Update;
                ShowSecretDialog = true;
            }
        }
        catch
        {
            // Error handling via loading state
        }
        finally
        {
            IsLoadingSecret = false;
        }
    }

    /// <summary>Opens the create secret dialog.</summary>
    public void ShowCreateDialog()
    {
        CurrentSecret = new WebSecret();
        SecretDialogMode = CrudDialogMode.Create;
        ShowSecretDialog = true;
    }

    /// <summary>Opens the import secrets dialog.</summary>
    public void ShowImportDialogInternal()
    {
        ShowImportDialog = true;
    }

    /// <summary>Closes the secret dialog.</summary>
    [RelayCommand]
    public void CloseSecretDialog()
    {
        ShowSecretDialog = false;
        CurrentSecret = null;
    }

    /// <summary>Closes the import dialog.</summary>
    [RelayCommand]
    public void CloseImportDialog()
    {
        ShowImportDialog = false;
    }

    /// <summary>Handles saving a secret (create or update based on current mode).</summary>
    /// <param name="secret">The secret to save.</param>
    [RelayCommand]
    public async Task HandleSecretDialogSaveAsync(Secret secret)
    {
        switch (SecretDialogMode)
        {
            case CrudDialogMode.Create:
                await HandleCreateSecretAsync(secret);
                break;
            case CrudDialogMode.Update:
                await HandleSaveSecretAsync(secret);
                break;
            case CrudDialogMode.View:
                CloseSecretDialog();
                break;
        }
    }

    /// <summary>Handles importing secrets from the import dialog.</summary>
    /// <param name="result">The result of the import operation.</param>
    [RelayCommand]
    public async Task HandleImportSecretsAsync(ImportResult result)
    {
        if (vaultStateService.VaultManager == null || vaultStateService.LoadedVault == null || string.IsNullOrEmpty(vaultStateService.CurrentVaultKey))
        {
            return;
        }

        try
        {
            var addResult = vaultStateService.LoadedVault.AddRawSecrets(result.MappedSecrets, SecretType.Web);

            if (addResult)
            {
                await vaultStateService.VaultManager.SaveAsync(vaultStateService.LoadedVault, vaultStateService.CurrentVaultKey, null, CancellationToken.None);
                await RefreshVaultStateAsync();
            }

            CloseImportDialog();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing secrets: {ex.Message}");
        }
    }

    /// <summary>Shows the delete confirmation for a secret.</summary>
    /// <param name="secretId">The ID of the secret to delete.</param>
    /// <param name="secretName">The display name of the secret to delete.</param>
    public void ShowDeleteConfirmationFor(string secretId, string secretName)
    {
        secretIdToDelete = secretId;
        DeleteConfirmationMessage = $"Are you sure you want to delete the secret '{secretName}'?";
        ShowDeleteConfirmation = true;
    }

    /// <summary>Cancels the delete confirmation.</summary>
    [RelayCommand]
    public void CancelDeleteConfirmation()
    {
        ShowDeleteConfirmation = false;
        secretIdToDelete = string.Empty;
        DeleteConfirmationMessage = string.Empty;
    }

    /// <summary>Deletes the confirmed secret.</summary>
    [RelayCommand]
    public async Task HandleDeleteSecretAsync()
    {
        if (vaultStateService.VaultManager == null || vaultStateService.LoadedVault == null || string.IsNullOrEmpty(vaultStateService.CurrentVaultKey) || string.IsNullOrEmpty(secretIdToDelete))
        {
            CancelDeleteConfirmation();
            return;
        }

        try
        {
            IsDeletingSecret = true;
            var deleted = vaultStateService.LoadedVault.DeleteSecret(secretIdToDelete);

            if (deleted)
            {
                await vaultStateService.VaultManager.SaveAsync(vaultStateService.LoadedVault, vaultStateService.CurrentVaultKey, null, CancellationToken.None);
                await RefreshVaultStateAsync();
            }

            CancelDeleteConfirmation();
        }
        catch
        {
            // Error handling
        }
        finally
        {
            IsDeletingSecret = false;
        }
    }

    /// <inheritdoc/>
    public void Receive(ShowCreateCredentialMessage message)
    {
        ShowCreateDialog();
    }

    /// <inheritdoc/>
    public void Receive(ShowImportMessage message)
    {
        ShowImportDialogInternal();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            messenger.Unregister<ShowCreateCredentialMessage>(this);
            messenger.Unregister<ShowImportMessage>(this);
            vaultStateService.VaultStateChanged -= OnVaultStateChanged;
        }

        base.Dispose(disposing);
    }

    private void OnVaultStateChanged(object? sender, EventArgs e)
    {
        RefreshFilteredEntries();
        OnPropertyChanged(nameof(CurrentVault));
    }

    private void RefreshFilteredEntries()
    {
        if (vaultStateService.CurrentVault?.IndexEntries != null)
        {
            FilteredEntries = vaultStateService.CurrentVault.IndexEntries
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            FilteredEntries = [];
        }
    }

    private async Task HandleSaveSecretAsync(Secret editedSecret)
    {
        if (vaultStateService.VaultManager == null || vaultStateService.LoadedVault == null || string.IsNullOrEmpty(vaultStateService.CurrentVaultKey))
        {
            return;
        }

        try
        {
            IsSavingSecret = true;
            vaultStateService.LoadedVault.UpdateSecret(editedSecret);
            await vaultStateService.VaultManager.SaveAsync(vaultStateService.LoadedVault, vaultStateService.CurrentVaultKey, null, CancellationToken.None);
            CloseSecretDialog();
            await RefreshVaultStateAsync();
        }
        catch
        {
            // Error handling
        }
        finally
        {
            IsSavingSecret = false;
        }
    }

    private async Task HandleCreateSecretAsync(Secret newSecret)
    {
        if (vaultStateService.VaultManager == null || vaultStateService.LoadedVault == null || string.IsNullOrEmpty(vaultStateService.CurrentVaultKey))
        {
            return;
        }

        try
        {
            IsSavingSecret = true;
            vaultStateService.LoadedVault.AddSecret(newSecret);
            await vaultStateService.VaultManager.SaveAsync(vaultStateService.LoadedVault, vaultStateService.CurrentVaultKey, null, CancellationToken.None);
            CloseSecretDialog();
            await RefreshVaultStateAsync();
        }
        catch
        {
            // Error handling
        }
        finally
        {
            IsSavingSecret = false;
        }
    }

    private async Task RefreshVaultStateAsync()
    {
        if (vaultStateService.VaultManager == null || vaultStateService.CurrentVault == null || string.IsNullOrEmpty(vaultStateService.CurrentVaultKey))
        {
            return;
        }

        try
        {
            var reloadedVault = await vaultStateService.VaultManager.LoadAsync(
                vaultStateService.CurrentVault.Id,
                vaultStateService.CurrentVaultKey,
                CancellationToken.None);

            vaultStateService.UpdateLoadedVault(reloadedVault);
            RefreshFilteredEntries();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing vault: {ex.Message}");
        }
    }
}
