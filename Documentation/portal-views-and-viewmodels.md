# Portal Views, ViewModels & Services

This document describes every view (page, layout, component) in the Clypse portal alongside its ViewModel — including injected services and all public methods — followed by the service interface catalogue.

---

## Pages

### `Home` — `Pages/Home.razor`

- **Routes:** `/`, `/home`
- **Layout:** `HomeLayout`
- **ViewModel:** `HomeViewModel`
- **Description:** Main authenticated page. Switches between the Vaults and Credentials sub-views and hosts the vault management dialogs (verify, delete, create).

**Injected Services:**
- `IAuthenticationService`
- `INavigationService`
- `IVaultManagerFactoryService`
- `IVaultStorageService`
- `INavigationStateService`
- `IVaultStateService`
- `IJsS3InvokerProvider`
- `AwsS3Config`
- `IKeyDerivationService` *(clypse.core)*
- `IMessenger` *(CommunityToolkit)*

**Public Methods:**
- `OnAfterRenderAsync(bool firstRender)` — Checks authentication and redirects to login if unauthenticated on first render.
- `HandleLockVaultAsync()` *(RelayCommand)* — Clears vault state and returns to the vaults list.
- `CloseVerifyDialog()` *(RelayCommand)* — Hides the verify results dialog.
- `CancelDeleteVault()` *(RelayCommand)* — Cancels the vault deletion dialog.
- `HandleDeleteVaultConfirmAsync()` *(RelayCommand)* — Confirms and executes vault deletion.
- `CancelCreateVault()` *(RelayCommand)* — Cancels the create vault dialog.
- `HandleCreateVaultFromDialogAsync(VaultCreationRequest request)` *(RelayCommand)* — Creates a new vault from the dialog input.

---

### `Login` — `Pages/Login.razor`

- **Route:** `/login`
- **ViewModel:** `LoginViewModel`
- **Description:** Authentication page supporting standard username/password login, saved-user selection, WebAuthn passkey login, forced password reset, and forgot-password flow.

**Injected Services:**
- `IAuthenticationService`
- `INavigationService`
- `IUserSettingsService`
- `IBrowserInteropService`
- `ILocalStorageService`
- `IWebAuthnService`
- `ICryptoService` *(clypse.core)*
- `AppSettings`

**Public Methods:**
- `OnAfterRenderAsync(bool firstRender)` — Initialises theme, loads saved users, and redirects if already authenticated.
- `ToggleThemeAsync()` *(RelayCommand)* — Toggles between light and dark theme.
- `SelectUserAsync(SavedUser user)` *(RelayCommand)* — Selects a saved user; auto-attempts WebAuthn login if a credential is present.
- `RemoveUserAsync(SavedUser user)` *(RelayCommand)* — Removes a user from the saved users list.
- `ShowLoginForm()` *(RelayCommand)* — Switches from the saved-users list back to the login form.
- `ShowUsersListCommand()` *(RelayCommand)* — Switches to the saved-users list view.
- `HandleLoginAsync()` *(RelayCommand)* — Submits the login form and handles success, password reset required, and error states.
- `HandlePasswordChangeAsync()` *(RelayCommand)* — Submits a forced password reset form.
- `HandleWebAuthnSetupAsync()` *(RelayCommand)* — Registers a WebAuthn passkey credential after successful login.
- `SkipWebAuthnSetup()` *(RelayCommand)* — Skips WebAuthn setup and navigates to the app.
- `DismissWebAuthnError()` *(RelayCommand)* — Dismisses the WebAuthn error and navigates to the app.
- `ShowForgotPassword()` *(RelayCommand)* — Activates the forgot-password flow.
- `CancelForgotPassword()` *(RelayCommand)* — Cancels the forgot-password flow and resets all fields.
- `HandleForgotPasswordAsync()` *(RelayCommand)* — Sends a password reset verification code.
- `HandleConfirmForgotPasswordAsync()` *(RelayCommand)* — Confirms a password reset with the verification code and new password.

---

### `Test` — `Pages/Test.razor`

- **Route:** `/test`
- **ViewModel:** `TestViewModel`
- **Description:** Debug/development sandbox for testing authentication, S3 vault operations, key derivation benchmarks, and WebAuthn registration/authentication.

**Injected Services:**
- `IAuthenticationService`
- `IWebAuthnService`
- `IJsS3InvokerProvider`
- `AwsCognitoConfig`
- `AwsS3Config`
- `INavigationService`

**Public Methods:**
- `HandleLoginAsync()` *(RelayCommand)* — Authenticates with username/password and stores tokens.
- `HandleLogoutAsync()` *(RelayCommand)* — Logs out and clears all tokens and test results.
- `HandleTestKeyDerivationAsync()` *(RelayCommand)* — Runs Argon2id key derivation benchmarks across all presets.
- `HandleTestClypseAsync()` *(RelayCommand)* — Runs a full S3 vault create/save/load test using stored AWS credentials.
- `HandleWebAuthnRegisterAsync()` *(RelayCommand)* — Registers a new WebAuthn credential for the test username.
- `HandleWebAuthnAuthenticateAsync()` *(RelayCommand)* — Authenticates with the previously registered WebAuthn credential.
- `HandleWebAuthnClear()` *(RelayCommand)* — Resets WebAuthn log, credential state, and status.

---

## Layouts

### `MainLayout` — `Layout/MainLayout.razor`

- **ViewModel:** `MainLayoutViewModel`
- **Description:** Root layout wrapping all pages. Manages PWA update detection and the changelog/changes dialog.

**Injected Services:**
- `IPwaUpdateService`
- `AppSettings`
- `ILogger<MainLayoutViewModel>`

**Public Methods:**
- `OnAfterRenderAsync(bool firstRender)` — Sets up the PWA update service and starts the background update polling loop.
- `HandleVersionClickAsync()` *(RelayCommand)* — Opens the changelog dialog and checks for updates in the background.
- `HandleCloseChangesDialog()` *(RelayCommand)* — Closes the changelog dialog.
- `HandleInstallUpdateAsync()` *(RelayCommand)* — Installs the available PWA update (falls back to force update).

---

### `HomeLayout` — `Layout/HomeLayout.razor`

- **ViewModel:** `HomeLayoutViewModel`
- **Description:** Inner layout for authenticated pages. Manages the collapsible sidebar navigation, theme switching, and the session expiry countdown timer.

**Injected Services:**
- `IAuthenticationService`
- `IUserSettingsService`
- `ILocalStorageService`
- `IBrowserInteropService`
- `INavigationService`
- `INavigationStateService`
- `AppSettings`

**Public Methods:**
- `OnAfterRenderAsync(bool firstRender)` — Initialises theme and starts the session expiry timer.
- `ToggleSidebar()` *(RelayCommand)* — Toggles sidebar expanded/collapsed state.
- `ToggleThemeAsync()` *(RelayCommand)* — Switches between light and dark theme and persists the choice.
- `HandleNavigationAction(string action)` *(RelayCommand)* — Collapses the sidebar and forwards the action to the navigation state service.
- `HandleLogoutAsync()` *(RelayCommand)* — Logs out the user and navigates to `/login`.

---

## Components

### `Vaults` — `Components/Vaults.razor`

- **ViewModel:** `VaultsViewModel`
- **Description:** Displays the list of available vaults. Supports selecting, unlocking (passphrase entry panel), and viewing vault details.

**Injected Services:**
- `IVaultStorageService`
- `IVaultManagerBootstrapperFactoryService`
- `ILocalStorageService`
- `IVaultStateService`
- `IJsS3InvokerProvider`
- `AwsS3Config`
- `IMessenger` *(CommunityToolkit)*

**Message Recipients:**
- `RefreshVaultsMessage` — Reloads the vault list.

**Public Methods:**
- `OnInitializedAsync()` — Loads vaults on component initialisation.
- `OnAfterRenderAsync(bool firstRender)` — Loads vaults on first render.
- `LoadVaultsAsync()` — Fetches the vault listing from S3 and merges with locally stored metadata.
- `ShowPassphrasePanelFor(VaultMetadata vault)` *(RelayCommand)* — Opens the passphrase entry panel for the given vault.
- `HidePassphrasePanel()` *(RelayCommand)* — Dismisses the passphrase panel.
- `ShowVaultDetailsPanelFor(VaultMetadata vault)` *(RelayCommand)* — Opens the vault details panel for the given vault.
- `HideVaultDetailsPanel()` *(RelayCommand)* — Dismisses the vault details panel.
- `HandleUnlockVaultAsync(string passphrase)` *(RelayCommand)* — Derives the vault key and loads the vault, then updates global vault state.
- `Receive(RefreshVaultsMessage message)` — Handles the cross-component refresh message by reloading vaults.

---

### `Credentials` — `Components/Credentials.razor`

- **ViewModel:** `CredentialsViewModel`
- **Description:** Displays and manages secrets within the currently unlocked vault. Supports search, create, view, edit, delete, and import.

**Injected Services:**
- `IVaultStateService`
- `IMessenger` *(CommunityToolkit)*

**Message Recipients:**
- `ShowCreateCredentialMessage` — Opens the create-secret dialog.
- `ShowImportMessage` — Opens the import-secrets dialog.

**Public Methods:**
- `OnInitializedAsync()` — Populates the filtered entries list on initialisation.
- `HandleSearch()` — Filters vault index entries by name, description, or tags.
- `ClearSearch()` *(RelayCommand)* — Resets the search term and shows all entries.
- `ViewSecretAsync(string secretId)` *(RelayCommand)* — Loads and shows a secret in view mode.
- `EditSecretAsync(string secretId)` *(RelayCommand)* — Loads and shows a secret in edit mode.
- `ShowCreateDialog()` — Opens the secret dialog in create mode.
- `ShowImportDialogInternal()` — Opens the import secrets dialog.
- `CloseSecretDialog()` *(RelayCommand)* — Closes the secret dialog.
- `CloseImportDialog()` *(RelayCommand)* — Closes the import dialog.
- `HandleSecretDialogSaveAsync(Secret secret)` *(RelayCommand)* — Dispatches to create or update based on the current dialog mode.
- `HandleImportSecretsAsync(ImportResult result)` *(RelayCommand)* — Bulk-imports secrets from an import result into the vault.
- `ShowDeleteConfirmationFor(string secretId, string secretName)` — Shows the delete confirmation for a specific secret.
- `CancelDeleteConfirmation()` *(RelayCommand)* — Cancels the delete confirmation.
- `HandleDeleteSecretAsync()` *(RelayCommand)* — Deletes the confirmed secret from the vault.
- `Receive(ShowCreateCredentialMessage message)` — Handles create credential message.
- `Receive(ShowImportMessage message)` — Handles show-import message.

---

### `ChangesDialog` — `Components/ChangesDialog.razor`

- **ViewModel:** `ChangesDialogViewModel`
- **Parameters:** `Show`, `ShowUpdateButton`, `AvailableVersion`, `OnClose`, `OnUpdate`
- **Description:** Modal dialog showing the application changelog. Loads changelog content on first display. Optionally shows an install-update button.

**Injected Services:**
- `HttpClient`
- `ILogger<ChangesDialogViewModel>`

**Public Methods:**
- `EnsureChangelogLoadedAsync()` — Loads the changelog if it has not already been loaded.
- `HandleCloseAsync()` *(RelayCommand)* — Invokes the `OnCloseCallback`.
- `HandleUpdateAsync()` *(RelayCommand)* — Invokes the `OnUpdateCallback`.

---

### `ConfirmDialog` — `Components/ConfirmDialog.razor`

- **ViewModel:** `ConfirmDialogViewModel`
- **Parameters:** `Show`, `Message`, `IsProcessing`, `OnConfirm`, `OnCancel`
- **Description:** Generic confirmation modal dialog with a customisable message. Cancels on backdrop click when not processing.

**Injected Services:** *(none)*

**Public Methods:**
- `HandleBackdropClickAsync()` *(RelayCommand)* — Cancels the dialog when the backdrop is clicked (only if not processing).

---

### `ImportSecretsDialog` — `Components/ImportSecretsDialog.razor`

- **ViewModel:** `ImportSecretsDialogViewModel`
- **Parameters:** `Show`, `OnCancel`, `OnImport`
- **Description:** Modal dialog for importing secrets from a CSV file. Supports KeePass and Cachy CSV formats and shows a data preview before importing.

**Injected Services:**
- `ISecretsImporterService` *(clypse.core)*

**Public Methods:**
- `GetFormatDisplayName(CsvImportDataFormat format)` *(static)* — Returns the human-readable name for an import format.
- `Reset()` — Resets all dialog state (called when dialog is hidden).
- `HandleFileSelectedAsync(InputFileChangeEventArgs e)` — Handles file selection; reads CSV content, validates extension and size, and generates a preview.
- `ImportAsync()` *(RelayCommand)* — Executes the import operation and invokes `OnImportCallback` with the result.
- `CancelAsync()` *(RelayCommand)* — Invokes the `OnCancelCallback`.

---

### `LoadingDialog` — `Components/LoadingDialog.razor`

- **ViewModel:** `LoadingDialogViewModel`
- **Parameters:** `Show`, `Message`
- **Description:** Simple loading overlay with a configurable message string.

**Injected Services:** *(none)*

**Public Methods:** *(none — property access only)*

---

### `PasswordGeneratorDialog` — `Components/PasswordGeneratorDialog.razor`

- **ViewModel:** `PasswordGeneratorDialogViewModel`
- **Parameters:** `Show`, `OnPasswordGenerated`, `OnCancel`
- **Description:** Modal dialog for generating passwords. Supports memorable (template-based) and random (custom character groups, length) password types.

**Injected Services:**
- `IPasswordGeneratorService` *(clypse.core)*
- `AppSettings`

**Public Methods:**
- `InitializeAsync()` — Loads templates and generates an initial password (idempotent; only runs once per dialog show).
- `Reset()` — Resets `isInitialized` so the dialog re-initialises on next show.
- `RegenerateAsync()` *(RelayCommand)* — Generates a fresh password with the current settings.
- `AcceptPasswordAsync()` *(RelayCommand)* — Accepts the generated password and invokes `OnPasswordGeneratedCallback`.
- `CancelAsync()` *(RelayCommand)* — Invokes the `OnCancelCallback`.

---

### `SecretDialog` — `Components/SecretDialog.razor`

- **ViewModel:** `SecretDialogViewModel`
- **Parameters:** `Show`, `Secret`, `Mode` (`CrudDialogMode`), `OnSave`, `OnCancel`
- **Description:** Modal dialog for creating, viewing, or editing a secret. Dynamically renders fields based on the secret type via reflection.

**Injected Services:** *(none)*

**Public Methods:**
- `GetModeIcon()` *(static)* — Returns the Bootstrap icon name for the dialog header.
- `InitializeForSecret(Secret secret, CrudDialogMode dialogMode)` — Sets up a working copy of the secret and discovers its fields.
- `Clear()` — Resets dialog state (called when dialog is hidden).
- `OnSecretTypeChanged(SecretType newSecretType)` — Casts the editable secret to the correct subtype and refreshes field metadata.
- `GetModeTitle()` — Returns the mode-specific dialog title string.
- `HandleSaveAsync()` *(RelayCommand)* — Invokes `OnSaveCallback` with the editable secret.
- `HandleCancelAsync()` *(RelayCommand)* — Invokes the `OnCancelCallback`.

---

### `UnlockVaultDialog` — `Components/UnlockVaultDialog.razor`

- **ViewModel:** `UnlockVaultDialogViewModel`
- **Parameters:** `IsVisible`, `Vault`, `IsUnlocking`, `ErrorMessage`, `OnCancel`, `OnUnlock`
- **Description:** Modal dialog for entering a vault passphrase. Auto-focuses the input and supports Enter key to submit.

**Injected Services:** *(none)*

**Public Methods:**
- `ResetPassphrase()` — Clears the passphrase field (called when the dialog becomes visible).
- `UnlockAsync()` *(RelayCommand)* — Invokes `OnUnlockCallback` with the current passphrase.
- `CancelAsync()` *(RelayCommand)* — Clears the passphrase and invokes `OnCancelCallback`.

---

### `VaultCreateDialog` — `Components/VaultCreateDialog.razor`

- **ViewModel:** `VaultCreateDialogViewModel`
- **Parameters:** `Show`, `IsCreating`, `ErrorMessage`, `OnCancel`, `OnCreateVault`
- **Description:** Modal form for creating a new vault with name, description, and passphrase (with confirmation). Validates that both passphrases match and meet minimum length.

**Injected Services:** *(none)*

**Public Methods:**
- `ClearForm()` — Resets all form fields to empty strings.
- `CreateVaultAsync()` *(RelayCommand)* — Validates the form and invokes `OnCreateVaultCallback` with a `VaultCreationRequest`.
- `CancelAsync()` *(RelayCommand)* — Clears the form and invokes `OnCancelCallback`.

---

### `VaultDeleteConfirmDialog` — `Components/VaultDeleteConfirmDialog.razor`

- **ViewModel:** `VaultDeleteConfirmDialogViewModel`
- **Parameters:** `Show`, `VaultToDelete`, `IsDeleting`, `ErrorMessage`, `OnConfirm`, `OnCancel`
- **Description:** Deletion-confirmation dialog requiring the user to type the vault name before the confirm button activates.

**Injected Services:** *(none)*

**Public Methods:**
- `Reset()` — Clears the confirmation text field (called when dialog is hidden).

---

### `VerifyDialog` — `Components/VerifyDialog.razor`

- **ViewModel:** `VerifyDialogViewModel`
- **Parameters:** `Show`, `Results`, `OnClose`
- **Description:** Read-only display dialog showing vault verification results.

**Injected Services:** *(none)*

**Public Methods:** *(none — property and callback access only)*

---

## Field Components (`Components/Fields/`)

### `SingleLineTextField` — `Components/Fields/SingleLineTextField.razor`

- **ViewModel:** `SingleLineTextFieldViewModel`
- **Parameters:** `Label`, `Value`, `ValueChanged`, `Placeholder`, `IsReadOnly`
- **Description:** Single-line text input with two-way binding via ViewModel.

**Injected Services:** *(none)*

**Public Methods:**
- `OnValueChangedAsync(string? newValue)` — Updates the value and invokes `ValueChangedCallback`.

---

### `MultiLineTextField` — `Components/Fields/MultiLineTextField.razor`

- **ViewModel:** `MultiLineTextFieldViewModel`
- **Parameters:** `Label`, `Value`, `ValueChanged`, `Placeholder`, `IsReadOnly`, `Rows`
- **Description:** Multi-line textarea with two-way binding via ViewModel.

**Injected Services:** *(none)*

**Public Methods:**
- `OnValueChangedAsync(string? newValue)` — Updates the value and invokes `ValueChangedCallback`.

---

### `PasswordField` — `Components/Fields/PasswordField.razor`

- **ViewModel:** `PasswordFieldViewModel`
- **Parameters:** `Label`, `Placeholder`, `Value`, `ValueChanged`, `IsReadOnly`, `ShowRevealButton`, `ShowGeneratorButton`, `ShowStrengthIndicator`
- **Description:** Password input with show/hide toggle, inline strength indicator, and optional embedded password generator.

**Injected Services:**
- `IPasswordComplexityEstimatorService` *(clypse.core, optional)*

**Public Methods:**
- `TogglePasswordVisibility()` *(RelayCommand)* — Toggles `ShowPassword` between `true` and `false`.
- `ShowGenerator()` *(RelayCommand)* — Sets `ShowPasswordGenerator` to `true`.
- `HandlePasswordGeneratedAsync(string password)` — Accepts a generated password, propagates it via callbacks, updates complexity, and hides the generator.
- `HideGenerator()` *(RelayCommand)* — Hides the embedded password generator without accepting a password.
- `OnPasswordChangedAsync()` — Fires `ValueChangedCallback` and triggers a complexity update.
- `OnPasswordInput(string? newValue)` — Handles raw input events and schedules a debounced complexity update (500 ms).
- `Dispose()` — Cleans up the debounce timer.

---

### `TagListField` — `Components/Fields/TagListField.razor`

- **ViewModel:** `TagListFieldViewModel`
- **Parameters:** `Label`, `Placeholder`, `Tags`, `TagsChanged`, `IsReadOnly`
- **Description:** Tag chip list with add (Enter key or button) and remove functionality.

**Injected Services:** *(none)*

**Public Methods:**
- `AddTagAsync()` *(RelayCommand)* — Adds `NewTag` to the list (case-insensitive duplicate check) and invokes `TagsChangedCallback`.
- `RemoveTagAsync(string tag)` *(RelayCommand)* — Removes the specified tag and invokes `TagsChangedCallback`.

---

## Service Interfaces

### `IAuthenticationService`

**Description:** Provides all authentication operations including login, logout, password reset, and forgot-password flows.

**Methods:**
- `Initialize()` — Initialises the authentication service.
- `CheckAuthentication()` → `Task<bool>` — Returns `true` if the user is currently authenticated.
- `Login(string username, string password)` → `Task<LoginResult>` — Authenticates with username/password credentials.
- `Logout()` — Logs out and clears stored credentials.
- `GetStoredCredentials()` → `Task<StoredCredentials?>` — Returns the current session's stored credentials.
- `CompletePasswordReset(string username, string newPassword)` → `Task<LoginResult>` — Completes a forced password reset.
- `ForgotPassword(string username)` → `Task<ForgotPasswordResult>` — Initiates the forgot-password flow (sends verification code).
- `ConfirmForgotPassword(string username, string verificationCode, string newPassword)` → `Task<ForgotPasswordResult>` — Confirms the forgot-password flow with the verification code and new password.

---

### `INavigationService`

**Description:** Provides programmatic page navigation.

**Methods:**
- `NavigateTo(string uri)` — Navigates to the specified URI.

---

### `INavigationStateService`

**Description:** Maintains shared navigation state (sidebar items and action requests) between the home page and its layout.

**Events:**
- `NavigationItemsChanged` — Raised when the sidebar items are updated.
- `NavigationActionRequested` — Raised when a sidebar action is triggered.

**Methods:**
- `UpdateNavigationItems(IEnumerable<NavigationItem> items)` — Replaces the sidebar navigation items.
- `RequestNavigationAction(string action)` — Fires the `NavigationActionRequested` event with the given action identifier.

---

### `ILocalStorageService`

**Description:** Abstracts browser `localStorage` for key/value persistence.

**Methods:**
- `GetItemAsync(string key)` → `Task<string?>` — Retrieves a stored value, or `null` if not found.
- `SetItemAsync(string key, string value)` — Stores a value under the specified key.
- `RemoveItemAsync(string key)` — Removes an item from storage.
- `ClearAllExceptPersistentSettingsAsync()` — Clears all local storage entries except persistent user settings.

---

### `IVaultStateService`

**Description:** Shared singleton holding the currently active vault, its encryption key, loaded instance, and vault manager.

**Events:**
- `VaultStateChanged` — Raised when the vault state is set or cleared.

**Methods:**
- `SetVaultState(VaultMetadata vault, string key, IVault loadedVault, IVaultManager manager)` — Activates a vault after unlocking.
- `ClearVaultState()` — Clears the active vault state (locking).
- `UpdateLoadedVault(IVault loadedVault)` — Replaces the loaded vault instance (e.g., after a save operation).

---

### `IVaultStorageService`

**Description:** Persists vault metadata (name, description, ID) in local storage.

**Methods:**
- `GetVaultsAsync()` → `Task<List<VaultMetadata>>` — Retrieves all stored vault metadata.
- `SaveVaultsAsync(List<VaultMetadata> vaults)` — Overwrites all stored vault metadata.
- `UpdateVaultAsync(VaultMetadata vault)` — Upserts a single vault metadata record.
- `RemoveVaultAsync(string vaultId)` — Removes vault metadata by ID.
- `ClearVaultsAsync()` — Removes all vault metadata from storage.

---

### `IUserSettingsService`

**Description:** Provides access to user preferences (theme, etc.) with persistent storage.

**Methods:**
- `GetSettingsAsync()` → `Task<UserSettings>` — Retrieves all user settings.
- `SaveSettingsAsync(UserSettings settings)` — Persists user settings.
- `GetThemeAsync()` → `Task<string>` — Returns the current theme name.
- `SetThemeAsync(string theme)` — Saves and applies the named theme.

---

### `IBrowserInteropService`

**Description:** Provides DOM-level browser interop via JavaScript.

**Methods:**
- `SetThemeAsync(string theme)` — Applies a theme to the `<html>` root element (adds/removes a CSS attribute or class).

---

### `IWebAuthnService`

**Description:** Provides WebAuthn passkey registration and authentication via JavaScript interop.

**Methods:**
- `RegisterAsync(string username, string? userId)` → `Task<WebAuthnRegisterResult>` — Registers a new WebAuthn credential (PRF extension requested).
- `AuthenticateAsync(string credentialId)` → `Task<WebAuthnAuthenticateResult>` — Authenticates using an existing credential.

---

### `IPwaUpdateService`

**Description:** Manages PWA service-worker update detection and installation.

**Methods:**
- `IsUpdateAvailableAsync()` → `Task<bool>` — Returns `true` if a service-worker update is waiting.
- `CheckForUpdateAsync()` → `Task<bool>` — Manually triggers an update check.
- `InstallUpdateAsync()` → `Task<bool>` — Activates the waiting service-worker update.
- `ForceUpdateAsync()` → `Task<bool>` — Forces an update check and immediate install.
- `SetupUpdateCallbacksAsync(Func<Task>? onUpdateAvailable, Func<Task>? onUpdateInstalled, Func<string, Task>? onUpdateError)` — Registers event callbacks for update lifecycle events.

---

### `IJsS3InvokerProvider`

**Description:** Factory that creates JavaScript S3 invoker instances for browser-side AWS S3 operations.

**Methods:**
- `GetInvoker()` → `IJavaScriptS3Invoker` — Creates and returns a new JavaScript S3 invoker.

---

### `IVaultManagerFactoryService`

**Description:** Factory for creating `IVaultManager` instances configured for Blazor/JavaScript S3 interop.

**Methods:**
- `CreateForBlazor(IJavaScriptS3Invoker jsInvoker, string accessKey, string secretAccessKey, string sessionToken, string region, string bucketName, string identityId)` → `IVaultManager` — Creates a Blazor-compatible vault manager.

---

### `IVaultManagerBootstrapperFactoryService`

**Description:** Factory for creating `IVaultManagerBootstrapperService` instances used for vault listing and per-vault manager creation.

**Methods:**
- `CreateForBlazor(IJavaScriptS3Invoker jsInvoker, string accessKey, string secretAccessKey, string sessionToken, string region, string bucketName, string identityId)` → `IVaultManagerBootstrapperService` — Creates a Blazor-compatible vault manager bootstrapper.
