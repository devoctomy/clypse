using System.Text.Json;
using Blazing.Mvvm.ComponentModel;
using clypse.core.Cryptography.Interfaces;
using clypse.core.Vault;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels.Messages;
using clypse.portal.Models.Aws;
using clypse.portal.Models.Navigation;
using clypse.portal.Models.Vault;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the home page, managing vault operations and page navigation.
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly IAuthenticationService authService;
    private readonly INavigationService navigationService;
    private readonly IVaultManagerFactoryService vaultManagerFactory;
    private readonly IVaultStorageService vaultStorage;
    private readonly INavigationStateService navigationStateService;
    private readonly IVaultStateService vaultStateService;
    private readonly IJsS3InvokerProvider jsS3InvokerProvider;
    private readonly AwsS3Config awsS3Config;
    private readonly IKeyDerivationService keyDerivationService;
    private readonly IMessenger messenger;

    private string currentPage = "vaults";
    private bool showVerifyDialog;
    private VaultVerifyResults? verifyResults;
    private bool showDeleteVaultDialog;
    private VaultMetadata? vaultToDelete;
    private bool isDeletingVault;
    private string? deleteVaultErrorMessage;
    private bool showCreateVaultDialog;
    private bool isCreatingVault;
    private string? createVaultErrorMessage;

    /// <summary>
    /// Initializes a new instance of <see cref="HomeViewModel"/>.
    /// </summary>
    public HomeViewModel(
        IAuthenticationService authService,
        INavigationService navigationService,
        IVaultManagerFactoryService vaultManagerFactory,
        IVaultStorageService vaultStorage,
        INavigationStateService navigationStateService,
        IVaultStateService vaultStateService,
        IJsS3InvokerProvider jsS3InvokerProvider,
        AwsS3Config awsS3Config,
        IKeyDerivationService keyDerivationService,
        IMessenger messenger)
    {
        this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        this.vaultManagerFactory = vaultManagerFactory ?? throw new ArgumentNullException(nameof(vaultManagerFactory));
        this.vaultStorage = vaultStorage ?? throw new ArgumentNullException(nameof(vaultStorage));
        this.navigationStateService = navigationStateService ?? throw new ArgumentNullException(nameof(navigationStateService));
        this.vaultStateService = vaultStateService ?? throw new ArgumentNullException(nameof(vaultStateService));
        this.jsS3InvokerProvider = jsS3InvokerProvider ?? throw new ArgumentNullException(nameof(jsS3InvokerProvider));
        this.awsS3Config = awsS3Config ?? throw new ArgumentNullException(nameof(awsS3Config));
        this.keyDerivationService = keyDerivationService ?? throw new ArgumentNullException(nameof(keyDerivationService));
        this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

        this.navigationStateService.NavigationActionRequested += OnNavigationActionRequested;
        this.vaultStateService.VaultStateChanged += OnVaultStateChanged;
    }

    /// <summary>Gets the current page name ("vaults" or "credentials").</summary>
    public string CurrentPage { get => currentPage; private set => SetProperty(ref currentPage, value); }

    /// <summary>Gets a value indicating whether the verify dialog is shown.</summary>
    public bool ShowVerifyDialog { get => showVerifyDialog; private set => SetProperty(ref showVerifyDialog, value); }

    /// <summary>Gets the vault verify results.</summary>
    public VaultVerifyResults? VerifyResults { get => verifyResults; private set => SetProperty(ref verifyResults, value); }

    /// <summary>Gets a value indicating whether the delete vault dialog is shown.</summary>
    public bool ShowDeleteVaultDialog { get => showDeleteVaultDialog; private set => SetProperty(ref showDeleteVaultDialog, value); }

    /// <summary>Gets the vault to be deleted.</summary>
    public VaultMetadata? VaultToDelete { get => vaultToDelete; private set => SetProperty(ref vaultToDelete, value); }

    /// <summary>Gets a value indicating whether a vault deletion is in progress.</summary>
    public bool IsDeletingVault { get => isDeletingVault; private set => SetProperty(ref isDeletingVault, value); }

    /// <summary>Gets the error message for vault deletion.</summary>
    public string? DeleteVaultErrorMessage { get => deleteVaultErrorMessage; private set => SetProperty(ref deleteVaultErrorMessage, value); }

    /// <summary>Gets a value indicating whether the create vault dialog is shown.</summary>
    public bool ShowCreateVaultDialog { get => showCreateVaultDialog; private set => SetProperty(ref showCreateVaultDialog, value); }

    /// <summary>Gets a value indicating whether a vault is being created.</summary>
    public bool IsCreatingVault { get => isCreatingVault; private set => SetProperty(ref isCreatingVault, value); }

    /// <summary>Gets the error message for vault creation.</summary>
    public string? CreateVaultErrorMessage { get => createVaultErrorMessage; private set => SetProperty(ref createVaultErrorMessage, value); }

    /// <inheritdoc/>
    public override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await authService.Initialize();
            var isLoggedIn = await authService.CheckAuthentication();

            if (!isLoggedIn)
            {
                navigationService.NavigateTo("/login");
                return;
            }

            UpdateNavigationItems();
        }
    }

    /// <summary>Locks the current vault and navigates back to the vaults page.</summary>
    [RelayCommand]
    public async Task HandleLockVaultAsync()
    {
        vaultStateService.ClearVaultState();
        CurrentPage = "vaults";
        UpdateNavigationItems();
        await Task.CompletedTask;
    }

    /// <summary>Closes the verify dialog.</summary>
    [RelayCommand]
    public void CloseVerifyDialog()
    {
        ShowVerifyDialog = false;
        VerifyResults = null;
    }

    /// <summary>Cancels the vault deletion.</summary>
    [RelayCommand]
    public void CancelDeleteVault()
    {
        ShowDeleteVaultDialog = false;
        VaultToDelete = null;
        DeleteVaultErrorMessage = null;
        IsDeletingVault = false;
    }

    /// <summary>Confirms and executes vault deletion.</summary>
    [RelayCommand]
    public async Task HandleDeleteVaultConfirmAsync()
    {
        if (vaultStateService.CurrentVault == null || vaultStateService.VaultManager == null || vaultStateService.CurrentVaultKey == null || vaultStateService.LoadedVault == null)
        {
            CancelDeleteVault();
            return;
        }

        try
        {
            IsDeletingVault = true;
            DeleteVaultErrorMessage = null;

            await vaultStateService.VaultManager.DeleteAsync(vaultStateService.LoadedVault, vaultStateService.CurrentVaultKey, CancellationToken.None);
            await vaultStorage.RemoveVaultAsync(vaultStateService.CurrentVault.Id);

            CancelDeleteVault();
            vaultStateService.ClearVaultState();
            CurrentPage = "vaults";
            UpdateNavigationItems();
            await HandleRefreshAsync();
        }
        catch (Exception ex)
        {
            DeleteVaultErrorMessage = $"Failed to delete vault: {ex.Message}";
            Console.WriteLine($"Error deleting vault: {ex.Message}");
        }
        finally
        {
            IsDeletingVault = false;
        }
    }

    /// <summary>Cancels vault creation.</summary>
    [RelayCommand]
    public void CancelCreateVault()
    {
        ShowCreateVaultDialog = false;
        CreateVaultErrorMessage = null;
        IsCreatingVault = false;
    }

    /// <summary>Creates a vault from the dialog request.</summary>
    [RelayCommand]
    public async Task HandleCreateVaultFromDialogAsync(VaultCreationRequest request)
    {
        try
        {
            IsCreatingVault = true;
            CreateVaultErrorMessage = null;

            await HandleCreateVaultAsync(request);
            CancelCreateVault();
        }
        catch (Exception ex)
        {
            CreateVaultErrorMessage = $"Failed to create vault: {ex.Message}";
            Console.WriteLine($"Error creating vault: {ex.Message}");
        }
        finally
        {
            IsCreatingVault = false;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            navigationStateService.NavigationActionRequested -= OnNavigationActionRequested;
            vaultStateService.VaultStateChanged -= OnVaultStateChanged;
        }

        base.Dispose(disposing);
    }

    private void OnNavigationActionRequested(object? sender, string action)
    {
        _ = HandleNavigationActionAsync(action);
    }

    private void OnVaultStateChanged(object? sender, EventArgs e)
    {
        // Update navigation items when vault state changes (e.g., after unlock)
        if (vaultStateService.CurrentVault != null && CurrentPage != "credentials")
        {
            CurrentPage = "credentials";
            UpdateNavigationItems();
        }
        else if (vaultStateService.CurrentVault == null && CurrentPage != "vaults")
        {
            CurrentPage = "vaults";
            UpdateNavigationItems();
        }
    }

    private async Task HandleNavigationActionAsync(string action)
    {
        switch (action)
        {
            case "create-vault":
                ShowCreateVaultDialogInternal();
                return;
            case "show-vaults":
                CurrentPage = "vaults";
                break;
            case "delete-vault":
                if (vaultStateService.CurrentVault != null)
                {
                    ShowDeleteVaultDialogInternal();
                }
                return;
            case "create-credential":
                messenger.Send(new ShowCreateCredentialMessage());
                return;
            case "import":
                messenger.Send(new ShowImportMessage());
                return;
            case "lock-vault":
                await HandleLockVaultAsync();
                return;
            case "refresh":
                await HandleRefreshAsync();
                break;
            case "verify":
                await HandleVerifyAsync();
                break;
        }

        UpdateNavigationItems();
    }

    private void UpdateNavigationItems()
    {
        navigationStateService.UpdateNavigationItems(GetNavigationItems());
    }

    private IEnumerable<NavigationItem> GetNavigationItems()
    {
        return currentPage switch
        {
            "vaults" =>
            [
                new() { Text = "Create Vault", Action = "create-vault", Icon = "bi bi-plus-circle" },
                new() { Text = "Refresh", Action = "refresh", Icon = "bi bi-arrow-clockwise" }
            ],
            "credentials" =>
            [
                new() { Text = "Create Credential", Action = "create-credential", Icon = "bi bi-plus-circle" },
                new() { Text = "Import", Action = "import", Icon = "bi bi-upload" },
                new() { Text = "Verify Vault", Action = "verify", Icon = "bi bi-shield-check", ButtonClass = "btn-success" },
                new() { Text = "Lock Vault", Action = "lock-vault", Icon = "bi bi-lock", ButtonClass = "btn-primary" },
                new() { Text = "Delete Vault", Action = "delete-vault", Icon = "bi bi-trash3", ButtonClass = "btn-danger" }
            ],
            _ => []
        };
    }

    private async Task HandleRefreshAsync()
    {
        try
        {
            messenger.Send(new RefreshVaultsMessage());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing vaults: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task HandleVerifyAsync()
    {
        if (vaultStateService.LoadedVault == null || vaultStateService.CurrentVaultKey == null || vaultStateService.VaultManager == null)
        {
            return;
        }

        try
        {
            ShowVerifyDialog = true;
            VerifyResults = null;

            VerifyResults = await vaultStateService.VaultManager.VerifyAsync(
                vaultStateService.LoadedVault,
                vaultStateService.CurrentVaultKey,
                CancellationToken.None);
        }
        catch
        {
            ShowVerifyDialog = false;
            VerifyResults = null;
        }
    }

    private void ShowDeleteVaultDialogInternal()
    {
        if (vaultStateService.CurrentVault == null)
        {
            return;
        }

        VaultToDelete = vaultStateService.CurrentVault;
        DeleteVaultErrorMessage = null;
        ShowDeleteVaultDialog = true;
    }

    private void ShowCreateVaultDialogInternal()
    {
        CreateVaultErrorMessage = null;
        ShowCreateVaultDialog = true;
    }

    private async Task HandleCreateVaultAsync(VaultCreationRequest request)
    {
        Console.WriteLine($"Creating vault: {request.Name} - {request.Description}");

        var credentials = await authService.GetStoredCredentials();
        if (credentials?.AwsCredentials == null)
        {
            Console.WriteLine("No valid credentials found - cannot create vault");
            return;
        }

        var jsInvoker = jsS3InvokerProvider.GetInvoker();

        var tempVaultManager = vaultManagerFactory.CreateForBlazor(
            jsInvoker,
            credentials.AwsCredentials.AccessKeyId,
            credentials.AwsCredentials.SecretAccessKey,
            credentials.AwsCredentials.SessionToken,
            awsS3Config.Region,
            awsS3Config.BucketName,
            credentials.AwsCredentials.IdentityId);

        var vault = tempVaultManager.Create(request.Name, request.Description);
        Console.WriteLine($"Vault created with ID: {vault.Info.Id}");

        var keyBytes = await keyDerivationService.DeriveKeyFromPassphraseAsync(request.Passphrase, vault.Info.Base64Salt);
        var base64Key = Convert.ToBase64String(keyBytes);

        Console.WriteLine("Saving vault...");
        var saveResults = await tempVaultManager.SaveAsync(vault, base64Key, null, CancellationToken.None);
        Console.WriteLine($"Vault saved successfully. Results: {saveResults}");

        tempVaultManager.Dispose();

        var existingVaults = await vaultStorage.GetVaultsAsync();
        var existingVault = existingVaults.FirstOrDefault(v => v.Id == vault.Info.Id);

        if (existingVault != null)
        {
            existingVault.Name = vault.Info.Name;
            existingVault.Description = vault.Info.Description;
        }
        else
        {
            existingVaults.Add(new VaultMetadata
            {
                Id = vault.Info.Id,
                Name = vault.Info.Name,
                Description = vault.Info.Description
            });
        }

        await vaultStorage.SaveVaultsAsync(existingVaults);
        Console.WriteLine("Vault metadata persisted to localStorage");

        CurrentPage = "vaults";
        UpdateNavigationItems();
        await HandleRefreshAsync();
    }
}
