using System.Text.Json;
using Blazing.Mvvm.ComponentModel;
using clypse.core.Vault;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels.Messages;
using clypse.portal.Models.Vault;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the vaults component, managing vault listing and unlocking.
/// </summary>
public partial class VaultsViewModel : ViewModelBase, IRecipient<RefreshVaultsMessage>
{
    private readonly IVaultStorageService vaultStorage;
    private readonly IVaultManagerBootstrapperFactoryService vaultManagerBootstrapperFactory;
    private readonly ILocalStorageService localStorageService;
    private readonly IVaultStateService vaultStateService;
    private readonly IJsS3InvokerProvider jsS3InvokerProvider;
    private readonly Models.Aws.AwsS3Config awsS3Config;
    private readonly IMessenger messenger;

    private List<VaultMetadata> vaults = [];
    private List<VaultListing> vaultListings = [];
    private bool isLoading = true;
    private bool showPassphrasePanel;
    private bool showVaultDetailsPanel;
    private bool isUnlocking;
    private VaultMetadata? selectedVault;
    private VaultListing? selectedVaultListing;
    private string? errorMessage;
    private IVaultManagerBootstrapperService? bootstrapperService;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultsViewModel"/> class.
    /// </summary>
    /// <param name="vaultStorage">The vault storage service.</param>
    /// <param name="vaultManagerBootstrapperFactory">The vault manager bootstrapper factory.</param>
    /// <param name="localStorageService">The local storage service.</param>
    /// <param name="vaultStateService">The vault state service.</param>
    /// <param name="jsS3InvokerProvider">The JavaScript S3 invoker provider.</param>
    /// <param name="awsS3Config">The AWS S3 configuration.</param>
    /// <param name="messenger">The messenger used for cross-component communication.</param>
    public VaultsViewModel(
        IVaultStorageService vaultStorage,
        IVaultManagerBootstrapperFactoryService vaultManagerBootstrapperFactory,
        ILocalStorageService localStorageService,
        IVaultStateService vaultStateService,
        IJsS3InvokerProvider jsS3InvokerProvider,
        Models.Aws.AwsS3Config awsS3Config,
        IMessenger messenger)
    {
        this.vaultStorage = vaultStorage ?? throw new ArgumentNullException(nameof(vaultStorage));
        this.vaultManagerBootstrapperFactory = vaultManagerBootstrapperFactory ?? throw new ArgumentNullException(nameof(vaultManagerBootstrapperFactory));
        this.localStorageService = localStorageService ?? throw new ArgumentNullException(nameof(localStorageService));
        this.vaultStateService = vaultStateService ?? throw new ArgumentNullException(nameof(vaultStateService));
        this.jsS3InvokerProvider = jsS3InvokerProvider ?? throw new ArgumentNullException(nameof(jsS3InvokerProvider));
        this.awsS3Config = awsS3Config ?? throw new ArgumentNullException(nameof(awsS3Config));
        this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        this.messenger.Register(this);
    }

    /// <summary>Gets the list of vaults to display.</summary>
    public List<VaultMetadata> Vaults { get => vaults; private set => SetProperty(ref vaults, value); }

    /// <summary>Gets the vault listings with manifest information.</summary>
    public List<VaultListing> VaultListings { get => vaultListings; private set => SetProperty(ref vaultListings, value); }

    /// <summary>Gets a value indicating whether vaults are being loaded.</summary>
    public bool IsLoading { get => isLoading; private set => SetProperty(ref isLoading, value); }

    /// <summary>Gets a value indicating whether the passphrase panel is shown.</summary>
    public bool ShowPassphrasePanel { get => showPassphrasePanel; private set => SetProperty(ref showPassphrasePanel, value); }

    /// <summary>Gets a value indicating whether the vault details panel is shown.</summary>
    public bool ShowVaultDetailsPanel { get => showVaultDetailsPanel; private set => SetProperty(ref showVaultDetailsPanel, value); }

    /// <summary>Gets a value indicating whether a vault is being unlocked.</summary>
    public bool IsUnlocking { get => isUnlocking; private set => SetProperty(ref isUnlocking, value); }

    /// <summary>Gets the currently selected vault.</summary>
    public VaultMetadata? SelectedVault { get => selectedVault; private set => SetProperty(ref selectedVault, value); }

    /// <summary>Gets the vault listing details for the selected vault.</summary>
    public VaultListing? SelectedVaultListing { get => selectedVaultListing; private set => SetProperty(ref selectedVaultListing, value); }

    /// <summary>Gets or sets the error message.</summary>
    public string? ErrorMessage { get => errorMessage; set => SetProperty(ref errorMessage, value); }

    /// <inheritdoc/>
    public override async Task OnInitializedAsync()
    {
        await LoadVaultsAsync();
    }

    /// <inheritdoc/>
    public override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadVaultsAsync();
        }
    }

    /// <summary>Loads the list of vaults.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task LoadVaultsAsync()
    {
        try
        {
            IsLoading = true;

            bool prepared = await PrepareBootstrapper();
            if (!prepared)
            {
                return;
            }

            var vaultListingsResult = await bootstrapperService!.ListVaultsAsync(CancellationToken.None);
            VaultListings = [.. vaultListingsResult];

            var storedVaults = await vaultStorage.GetVaultsAsync();

            var loadedVaults = VaultListings.Select(listing =>
            {
                var storedVault = storedVaults.FirstOrDefault(v => v.Id == listing.Id);
                return new VaultMetadata
                {
                    Id = listing.Id ?? string.Empty,
                    Name = storedVault?.Name,
                    Description = storedVault?.Description,
                };
            }).ToList();

#if DEBUG
            CreateDummyTestVaults(loadedVaults);
#endif

            Vaults = loadedVaults;
            IsLoading = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading vaults: {ex.Message}");
            Vaults = [];
            IsLoading = false;
        }
    }

    /// <summary>Shows the passphrase entry panel for the specified vault.</summary>
    /// <param name="vault">The vault metadata to unlock.</param>
    [RelayCommand]
    public void ShowPassphrasePanelFor(VaultMetadata vault)
    {
        SelectedVault = vault;
        ErrorMessage = null;
        ShowPassphrasePanel = true;
    }

    /// <summary>Hides the passphrase entry panel.</summary>
    [RelayCommand]
    public void HidePassphrasePanel()
    {
        ShowPassphrasePanel = false;
        SelectedVault = null;
        ErrorMessage = null;
    }

    /// <summary>Shows the vault details panel for the specified vault.</summary>
    /// <param name="vault">The vault metadata to show details for.</param>
    [RelayCommand]
    public void ShowVaultDetailsPanelFor(VaultMetadata vault)
    {
        SelectedVault = vault;
        SelectedVaultListing = vaultListings.FirstOrDefault(vl => vl.Id == vault.Id);
        ShowVaultDetailsPanel = true;
    }

    /// <summary>Hides the vault details panel.</summary>
    [RelayCommand]
    public void HideVaultDetailsPanel()
    {
        ShowVaultDetailsPanel = false;
        SelectedVault = null;
        SelectedVaultListing = null;
    }

    /// <summary>Unlocks the selected vault with the given passphrase.</summary>
    /// <param name="passphrase">The passphrase used to decrypt the vault.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task HandleUnlockVaultAsync(string passphrase)
    {
        if (SelectedVault == null || string.IsNullOrEmpty(passphrase))
        {
            ErrorMessage = "Please enter a passphrase";
            return;
        }

        try
        {
            IsUnlocking = true;
            ErrorMessage = null;

            if (bootstrapperService == null)
            {
                ErrorMessage = "Bootstrapper service not available";
                IsUnlocking = false;
                return;
            }

            var vaultSpecificManager = await bootstrapperService.CreateVaultManagerForVaultAsync(SelectedVault.Id, CancellationToken.None);
            if (vaultSpecificManager == null)
            {
                ErrorMessage = "Failed to create vault-specific manager";
                IsUnlocking = false;
                return;
            }

            var keyBytes = await vaultSpecificManager.DeriveKeyFromPassphraseAsync(SelectedVault.Id, passphrase);
            var base64Key = Convert.ToBase64String(keyBytes);
            var vault = await vaultSpecificManager.LoadAsync(SelectedVault.Id, base64Key, CancellationToken.None);

            SelectedVault.Name = vault.Info.Name;
            SelectedVault.Description = vault.Info.Description;
            await vaultStorage.UpdateVaultAsync(SelectedVault);

            var unlockedVault = new VaultMetadata
            {
                Id = SelectedVault.Id,
                Name = SelectedVault.Name,
                Description = SelectedVault.Description,
                IndexEntries = [.. vault.Index.Entries],
            };

            HidePassphrasePanel();
            await LoadVaultsAsync();

            vaultStateService.SetVaultState(unlockedVault, base64Key, vault, vaultSpecificManager);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error unlocking vault: {ex.Message}");
            ErrorMessage = $"Failed to unlock vault: {ex.Message}";
        }
        finally
        {
            IsUnlocking = false;
        }
    }

    /// <summary>Receives the refresh vaults message and reloads the vault list.</summary>
    /// <param name="message">The refresh vaults message.</param>
    public async void Receive(RefreshVaultsMessage message)
    {
        await LoadVaultsAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            messenger.Unregister<RefreshVaultsMessage>(this);
        }

        base.Dispose(disposing);
    }

    private static void CreateDummyTestVaults(List<VaultMetadata> loadedVaults)
    {
        for (var i = 0; i < 20; i++)
        {
            loadedVaults.Add(new VaultMetadata
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Test Vault {i + 1}",
                Description = "This vault cannot be opened.",
            });
        }
    }

    private async Task<bool> PrepareBootstrapper()
    {
        if (bootstrapperService == null)
        {
            bootstrapperService = await CreateBootstrapperServiceAsync();
            if (bootstrapperService == null)
            {
                Console.WriteLine("Failed to create bootstrapper service");
                Vaults = [];
                IsLoading = false;
                return false;
            }
        }

        return true;
    }

    private async Task<IVaultManagerBootstrapperService?> CreateBootstrapperServiceAsync()
    {
        try
        {
            var credentialsJson = await localStorageService.GetItemAsync("clypse_credentials");

            if (string.IsNullOrEmpty(credentialsJson))
            {
                Console.WriteLine("No stored credentials found");
                return null;
            }

            var credentials = JsonSerializer.Deserialize<Models.Aws.StoredCredentials>(credentialsJson);
            if (credentials?.AwsCredentials == null)
            {
                Console.WriteLine("Invalid stored credentials - AwsCredentials is null");
                return null;
            }

            if (string.IsNullOrEmpty(credentials.AwsCredentials.IdentityId))
            {
                Console.WriteLine("Identity ID is null or empty - cannot create bootstrapper");
                return null;
            }

            var jsInvoker = jsS3InvokerProvider.GetInvoker();

            return vaultManagerBootstrapperFactory.CreateForBlazor(
                jsInvoker,
                credentials.AwsCredentials.AccessKeyId,
                credentials.AwsCredentials.SecretAccessKey,
                credentials.AwsCredentials.SessionToken,
                awsS3Config.Region,
                awsS3Config.BucketName,
                credentials.AwsCredentials.IdentityId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating bootstrapper service: {ex.Message}");
            return null;
        }
    }
}
