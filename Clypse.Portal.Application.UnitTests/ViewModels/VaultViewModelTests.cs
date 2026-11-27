using Clypse.Core.Enums;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Devoctomy.Blazwiz.Sync.Encryption.Interfaces;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public partial class VaultViewModelTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultService> mockVaults = new();
    private readonly Mock<IVaultSession> mockSession = new();
    private readonly Mock<ISecretCopyService> mockCopy = new();
    private readonly Mock<INavigationService> mockNavigation = new();
    private readonly Mock<IVaultReportService> mockReport = new();
    private readonly Mock<IVaultFileSession> mockFiles = new();
    private readonly UnsavedChangesGuard guard = new();
    private readonly Mock<ISyncProtectedProfileService> mockProfiles = new();
    private readonly Vault vault = new() { Id = "v1", Root = "profiles/v1/", Name = "Personal", Description = "Logins", IsUnlocked = true };
    private List<Secret> secrets = [];
    private Vault? open;
    private bool vaultsRead = true;

    public VaultViewModelTests()
    {
        mockProfiles.SetupGet(x => x.IsLoaded).Returns(() => vaultsRead);
        mockVaults.Setup(x => x.FindAsync("v1", It.IsAny<CancellationToken>())).ReturnsAsync(vault);
        mockSession.SetupGet(x => x.Vault).Returns(() => open);
        mockSession.SetupGet(x => x.Secrets).Returns(() => secrets);
        mockSession.Setup(x => x.OpenAsync(It.IsAny<Vault>(), It.IsAny<CancellationToken>()))
            .Callback<Vault, CancellationToken>((v, _) => open = v)
            .Returns(Task.CompletedTask);
        mockSession.Setup(x => x.Close()).Callback(() => open = null);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("vaultService", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(null!, mockSession.Object, text.Errors, text.Plurals, t, mockCopy.Object, mockNavigation.Object, mockReport.Object, mockFiles.Object, guard, mockProfiles.Object)).ParamName);
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(mockVaults.Object, null!, text.Errors, text.Plurals, t, mockCopy.Object, mockNavigation.Object, mockReport.Object, mockFiles.Object, guard, mockProfiles.Object)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(mockVaults.Object, mockSession.Object, null!, text.Plurals, t, mockCopy.Object, mockNavigation.Object, mockReport.Object, mockFiles.Object, guard, mockProfiles.Object)).ParamName);
        Assert.Equal("plurals", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(mockVaults.Object, mockSession.Object, text.Errors, null!, t, mockCopy.Object, mockNavigation.Object, mockReport.Object, mockFiles.Object, guard, mockProfiles.Object)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(mockVaults.Object, mockSession.Object, text.Errors, text.Plurals, null!, mockCopy.Object, mockNavigation.Object, mockReport.Object, mockFiles.Object, guard, mockProfiles.Object)).ParamName);
        Assert.Equal("copyService", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(mockVaults.Object, mockSession.Object, text.Errors, text.Plurals, t, null!, mockNavigation.Object, mockReport.Object, mockFiles.Object, guard, mockProfiles.Object)).ParamName);
        Assert.Equal("navigationService", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(mockVaults.Object, mockSession.Object, text.Errors, text.Plurals, t, mockCopy.Object, null!, mockReport.Object, mockFiles.Object, guard, mockProfiles.Object)).ParamName);
        Assert.Equal("reportService", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(mockVaults.Object, mockSession.Object, text.Errors, text.Plurals, t, mockCopy.Object, mockNavigation.Object, null!, mockFiles.Object, guard, mockProfiles.Object)).ParamName);
        Assert.Equal("fileSession", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(mockVaults.Object, mockSession.Object, text.Errors, text.Plurals, t, mockCopy.Object, mockNavigation.Object, mockReport.Object, null!, guard, mockProfiles.Object)).ParamName);
        Assert.Equal("guard", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(mockVaults.Object, mockSession.Object, text.Errors, text.Plurals, t, mockCopy.Object, mockNavigation.Object, mockReport.Object, mockFiles.Object, null!, mockProfiles.Object)).ParamName);
        Assert.Equal("profiles", Assert.Throws<ArgumentNullException>(() => new VaultViewModel(mockVaults.Object, mockSession.Object, text.Errors, text.Plurals, t, mockCopy.Object, mockNavigation.Object, mockReport.Object, mockFiles.Object, guard, null!)).ParamName);
    }

    [Fact]
    public async Task GivenAVault_WhenLoaded_ThenItIsOpenedInTheSession_WithItsNameAndDescription()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("v1");

        // Assert
        Assert.Same(vault, sut.Vault);
        Assert.False(sut.IsMissing);
        Assert.True(sut.IsUnlocked);
        Assert.Equal("Personal", sut.Title);
        Assert.Equal("Logins", sut.Subtitle);
        mockSession.Verify(x => x.Close(), Times.Once);
        mockSession.Verify(x => x.OpenAsync(vault, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenTheSameVault_WhenLoadedAgain_ThenItIsNotReopened()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act
        await sut.LoadAsync("v1");

        // Assert
        mockSession.Verify(x => x.OpenAsync(It.IsAny<Vault>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAVaultWithoutADescription_WhenLoaded_ThenTheSubtitleSaysWhatThePageIs()
    {
        // Arrange
        vault.Description = null;
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("v1");

        // Assert
        Assert.Equal("Your secrets in this vault, encrypted with its own keys.", sut.Subtitle);
    }

    [Fact]
    public async Task GivenAVaultBeingDeleted_WhenLoaded_ThenItIsNotOpened_AndTheVaultsAreShown()
    {
        // Arrange
        vault.IsDeleting = true;
        vault.IsUnlocked = false;
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("v1");

        // Assert
        mockSession.Verify(x => x.OpenAsync(It.IsAny<Vault>(), It.IsAny<CancellationToken>()), Times.Never);
        mockNavigation.Verify(x => x.NavigateTo("vaults"), Times.Once);
        Assert.False(sut.IsMissing);
    }

    [Fact]
    public async Task GivenTheUsersVaultsStillBeingRead_WhenLoaded_ThenTheVaultIsNotCalledMissing_AndIsOpenedOnceTheyAreRead()
    {
        // Arrange: straight after sign in, or when the app is opened again on the vault's page.
        vaultsRead = false;
        mockVaults.Setup(x => x.FindAsync("v1", It.IsAny<CancellationToken>())).ReturnsAsync(() => vaultsRead ? vault : null);
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("v1");
        var whileReading = (sut.IsFinding, sut.IsMissing, sut.Vault);
        await sut.LoadAsync("v1");
        vaultsRead = true;
        mockProfiles.Raise(x => x.ProfilesChanged += null, mockProfiles.Object, EventArgs.Empty);

        // Assert
        Assert.Equal((true, false, null), whileReading);
        Assert.False(sut.IsFinding);
        Assert.False(sut.IsMissing);
        Assert.Same(vault, sut.Vault);
        mockSession.Verify(x => x.Close(), Times.Once);
        mockSession.Verify(x => x.OpenAsync(vault, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenTheUsersVaultsStillBeingRead_WhenTheyAreReadWithoutTheVault_ThenItIsMissing()
    {
        // Arrange
        vaultsRead = false;
        var sut = CreateSut();
        await sut.LoadAsync("gone");
        var whileReading = (sut.IsFinding, sut.IsMissing);

        // Act: a change that is not the vaults being read changes nothing; then they are read.
        mockProfiles.Raise(x => x.ProfilesChanged += null, mockProfiles.Object, EventArgs.Empty);
        var stillReading = (sut.IsFinding, sut.IsMissing);
        vaultsRead = true;
        mockProfiles.Raise(x => x.ProfilesChanged += null, mockProfiles.Object, EventArgs.Empty);

        // Assert
        Assert.Equal((true, false), whileReading);
        Assert.Equal((true, false), stillReading);
        Assert.False(sut.IsFinding);
        Assert.True(sut.IsMissing);
        mockSession.Verify(x => x.OpenAsync(It.IsAny<Vault>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenAVaultThatIsOpen_WhenTheUsersVaultsChange_ThenItIsNotLookedForAgain()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act
        mockProfiles.Raise(x => x.ProfilesChanged += null, mockProfiles.Object, EventArgs.Empty);
        sut.Dispose();
        mockProfiles.Raise(x => x.ProfilesChanged += null, mockProfiles.Object, EventArgs.Empty);

        // Assert
        mockVaults.Verify(x => x.FindAsync("v1", It.IsAny<CancellationToken>()), Times.Once);
        mockSession.Verify(x => x.OpenAsync(vault, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenTheUsersVaultsStillBeingRead_WhenLookingAgainFails_ThenTheErrorIsShown()
    {
        // Arrange
        vaultsRead = false;
        var sut = CreateSut();
        await sut.LoadAsync("gone");
        mockVaults.Setup(x => x.FindAsync("gone", It.IsAny<CancellationToken>())).ThrowsAsync(new ClypseException(ClypseErrorCodes.VaultNotFound));

        // Act
        mockProfiles.Raise(x => x.ProfilesChanged += null, mockProfiles.Object, EventArgs.Empty);

        // Assert
        Assert.NotNull(sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenNoSuchVault_WhenLoaded_ThenItIsMissing()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("gone");

        // Assert
        Assert.True(sut.IsMissing);
        Assert.Null(sut.Vault);
        Assert.Equal("Vault", sut.Title);
        mockSession.Verify(x => x.OpenAsync(It.IsAny<Vault>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenOpeningFails_WhenLoaded_ThenTheErrorIsShown()
    {
        // Arrange
        mockSession.Setup(x => x.OpenAsync(It.IsAny<Vault>(), It.IsAny<CancellationToken>())).ThrowsAsync(new ClypseException(ClypseErrorCodes.SecretUnreadable));
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("v1");
        await WaitForAsync(() => sut.ErrorMessage != null);

        // Assert
        Assert.Equal("A secret in this vault could not be read.", sut.ErrorMessage);
        sut.DismissErrorCommand.Execute(null);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenDialogsAreOpenedAndClosed_ThenTheActiveDialogFollows()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act & Assert
        sut.NewSecretCommand.Execute(null);
        Assert.Equal(VaultViewModel.VaultDialog.Editor, sut.ActiveDialog);
        Assert.Null(sut.EditingSecretId);
        sut.EditSecretCommand.Execute("s1");
        Assert.Equal("s1", sut.EditingSecretId);
        sut.ShowImportCommand.Execute(null);
        Assert.Equal(VaultViewModel.VaultDialog.Import, sut.ActiveDialog);
        sut.CloseDialogCommand.Execute(null);
        Assert.Equal(VaultViewModel.VaultDialog.None, sut.ActiveDialog);
        Assert.Null(sut.EditingSecretId);
    }

    [Fact]
    public async Task GivenTheReport_WhenShownAndASecretInItIsOpened_ThenTheEditorReplacesIt()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act
        sut.ShowReportCommand.Execute(null);
        var shown = sut.ActiveDialog;
        sut.EditSecretCommand.Execute("s1");

        // Assert
        Assert.Equal(VaultViewModel.VaultDialog.Report, shown);
        Assert.Equal(VaultViewModel.VaultDialog.Editor, sut.ActiveDialog);
        Assert.Equal("s1", sut.EditingSecretId);
    }

    [Fact]
    public async Task GivenTheVaultsReport_WhenItFindsSomething_ThenThePageSaysSo_AndIsToldWhenItChanges()
    {
        // Arrange
        var report = new VaultReport { VaultId = "v1" };
        mockReport.SetupGet(x => x.Report).Returns(() => report);
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        var clean = sut.HasReportIssues;
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        report = new VaultReport { VaultId = "v1", ReusedPasswords = [new VaultReportReuse { SecretIds = ["a", "b"] }] };

        // Act
        mockReport.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.False(clean);
        Assert.True(sut.HasReportIssues);
        Assert.Contains(nameof(VaultViewModel.HasReportIssues), changed);
    }

    [Fact]
    public async Task GivenAnotherVaultsReport_WhenItFindsSomething_ThenThisPageDoesNotSaySo()
    {
        // Arrange
        mockReport.SetupGet(x => x.Report).Returns(new VaultReport { VaultId = "v2", ReusedPasswords = [new VaultReportReuse { SecretIds = ["a", "b"] }] });
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("v1");

        // Assert
        Assert.False(sut.HasReportIssues);
    }

    [Fact]
    public async Task GivenADialogOpen_WhenTheVaultIsLockedElsewhere_ThenTheDialogCloses()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        sut.EditSecretCommand.Execute("s1");

        // Act
        vault.IsUnlocked = false;
        mockSession.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.False(sut.IsUnlocked);
        Assert.Equal(VaultViewModel.VaultDialog.None, sut.ActiveDialog);
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenLocked_ThenTheVaultServiceLocksIt()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        sut.NewSecretCommand.Execute(null);

        // Act
        sut.LockCommand.Execute(null);

        // Assert
        mockVaults.Verify(x => x.Lock("v1"), Times.Once);
        mockNavigation.Verify(x => x.NavigateTo(VaultsViewModel.Url), Times.Once);
        Assert.Equal(VaultViewModel.VaultDialog.None, sut.ActiveDialog);
    }

    [Fact]
    public void GivenNoVault_WhenLocked_ThenNothingHappens()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.LockCommand.Execute(null);

        // Assert
        mockVaults.Verify(x => x.Lock(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GivenThePageIsLeft_WhenDisposed_ThenTheSessionIsClosed()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        mockSession.Invocations.Clear();

        // Act
        sut.Dispose();
        mockSession.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        mockSession.Verify(x => x.Close(), Times.Once);
    }

    [Theory]
    [InlineData(SecretType.Web, "bi-globe2")]
    [InlineData(SecretType.Aws, "bi-cloud-fill")]
    [InlineData(SecretType.None, "bi-key-fill")]
    public void GivenAKindOfSecret_WhenGettingItsIcon_ThenItsOwn(SecretType type, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, VaultViewModel.GetIcon(type));
    }

    [Fact]
    public void GivenSecrets_WhenSummarised_ThenWhoOrWhereTheyAreFor()
    {
        // Act & Assert
        Assert.Equal("bob", VaultViewModel.GetSummary(new WebSecret { Name = "a", UserName = "bob", EmailAddress = "b@x" }));
        Assert.Equal("b@x", VaultViewModel.GetSummary(new WebSecret { Name = "a", EmailAddress = "b@x" }));
        Assert.Equal("mail.google.com", VaultViewModel.GetSummary(new WebSecret { Name = "a", WebsiteUrl = "https://mail.google.com/inbox" }));
        Assert.Equal("example.com", VaultViewModel.GetSummary(new WebSecret { Name = "a", WebsiteUrl = "example.com" }));
        Assert.Equal("AKIA", VaultViewModel.GetSummary(new AwsCredentials { Name = "a", AccessKeyId = "AKIA" }));
        Assert.Equal("notes", VaultViewModel.GetSummary(new Secret { Name = "a", Description = "notes" }));
        Assert.Null(VaultViewModel.GetSummary(new WebSecret { Name = "a" }));
    }

    [Fact]
    public void GivenAField_WhenGettingItsCopyLabel_ThenItNamesTheField()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var first = sut.GetCopyLabel("Password");
        var again = sut.GetCopyLabel("Password");

        // Assert
        Assert.Equal("Copy Password", first);
        Assert.Same(first, again);
        Assert.Equal("Copy User name", sut.GetCopyLabel("UserName"));
    }

    private static WebSecret Web(string name, string user, string tags, string? website = null)
    {
        var secret = new WebSecret { Name = name, UserName = user, WebsiteUrl = website };
        secret.UpdateTags([.. tags.Split(',', StringSplitOptions.TrimEntries)]);
        return secret;
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(5);
        }
    }

    [Fact]
    public void GivenSecretsOfEachKind_WhenGettingTheirQuickCopyFields_ThenTheirIdentityAndSecretWhenSet()
    {
        // Act & Assert
        Assert.Equal(["UserName", "Password"], VaultViewModel.GetQuickCopyFields(new WebSecret { Name = "a", UserName = "bob", Password = "pw" }));
        Assert.Equal(["Password"], VaultViewModel.GetQuickCopyFields(new WebSecret { Name = "a", Password = "pw" }));
        Assert.Equal(["AccessKeyId", "SecretAccessKey"], VaultViewModel.GetQuickCopyFields(new AwsCredentials { Name = "a", AccessKeyId = "AKIA", SecretAccessKey = "s" }));
        Assert.Empty(VaultViewModel.GetQuickCopyFields(new Secret { Name = "a", Description = "notes" }));
        Assert.Equal("bi-person", VaultViewModel.GetCopyIcon("UserName"));
        Assert.Equal("bi-key", VaultViewModel.GetCopyIcon("SecretAccessKey"));
        Assert.Equal("bi-copy", VaultViewModel.GetCopyIcon("Comments"));
    }

    [Fact]
    public async Task GivenASecret_WhenAFieldIsCopiedFromTheList_ThenSecretValuesAreCopiedAsSecrets()
    {
        // Arrange
        var web = new WebSecret { Name = "Gmail", UserName = "bob", Password = "hunter2" };
        var aws = new AwsCredentials { Name = "Deploy", SecretAccessKey = "wJalr" };
        secrets = [web, aws];
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act
        await sut.CopyAsync(web.Id, "UserName");
        await sut.CopyAsync(web.Id, "Password");
        await sut.CopyAsync(aws.Id, "SecretAccessKey");
        await sut.CopyAsync("gone", "Password");

        // Assert
        mockCopy.Verify(x => x.CopyAsync("UserName", "bob", false), Times.Once);
        mockCopy.Verify(x => x.CopyAsync("Password", "hunter2", true), Times.Once);
        mockCopy.Verify(x => x.CopyAsync("SecretAccessKey", "wJalr", true), Times.Once);
        mockCopy.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GivenAnOpenVault_WhenItsSettingsAreShownAndClosed_ThenTheDialogFollows()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        // Act & Assert
        sut.ShowSettingsCommand.Execute(null);
        Assert.Equal(VaultViewModel.VaultDialog.Settings, sut.ActiveDialog);
        sut.CloseSettingsCommand.Execute(null);
        Assert.Equal(VaultViewModel.VaultDialog.None, sut.ActiveDialog);
        Assert.Contains(nameof(VaultViewModel.Title), changed);
    }

    [Fact]
    public async Task GivenTheVaultsSettings_WhenChangePassphraseIsChosenAndClosed_ThenThatDialogReplacesThemThenCloses()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        sut.ShowSettingsCommand.Execute(null);

        // Act
        sut.ShowChangePassphraseCommand.Execute(null);
        var shown = sut.ActiveDialog;
        sut.CloseDialogCommand.Execute(null);

        // Assert
        Assert.Equal(VaultViewModel.VaultDialog.ChangePassphrase, shown);
        Assert.Equal(VaultViewModel.VaultDialog.None, sut.ActiveDialog);
    }

    [Theory]
    [InlineData("UserName", "bi-person")]
    [InlineData("AccessKeyId", "bi-person-badge")]
    [InlineData("Password", "bi-key")]
    [InlineData("SecretAccessKey", "bi-key")]
    [InlineData("Comments", "bi-copy")]
    public void GivenAField_WhenGettingItsCopyIcon_ThenItsOwn(string field, string expected)
    {
        // Act & Assert
        Assert.Equal(expected, VaultViewModel.GetCopyIcon(field));
    }

    [Fact]
    public void GivenAWebsiteThatIsNoAddress_WhenSummarised_ThenItIsShownAsItIs()
    {
        // Act & Assert
        Assert.Equal("not a site", VaultViewModel.GetSummary(new WebSecret { Name = "a", WebsiteUrl = "not a site" }));
    }

    [Fact]
    public void GivenNoVaultLoaded_WhenShown_ThenTheTitlesSayWhatThePageIs_AndItIsNotUnlocked()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act
        var sut = CreateSut();

        // Assert
        Assert.Equal(t["Vault.Title"], sut.Title);
        Assert.Equal(t["Vault.Subtitle"], sut.Subtitle);
        Assert.False(sut.IsUnlocked);
    }

    [Fact]
    public async Task GivenASearch_WhenSetToNull_ThenItIsEmpty()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        sut.Search = "gmail";

        // Act
        sut.Search = null!;

        // Assert
        Assert.Empty(sut.Search);
    }

    [Fact]
    public async Task GivenASecret_WhenAFieldItDoesNotHaveIsCopied_ThenItIsNotCopiedAsASecret()
    {
        // Arrange
        var web = new WebSecret { Name = "Gmail" };
        secrets = [web];
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act
        await sut.CopyAsync(web.Id, "NotAField");

        // Assert
        mockCopy.Verify(x => x.CopyAsync("NotAField", null, false), Times.Once);
    }

    [Fact]
    public void GivenAReport_WhenRead_ThenItIsTheReportServicesReport()
    {
        // Arrange
        var report = new VaultReport { VaultId = "v1" };
        mockReport.SetupGet(x => x.Report).Returns(report);
        var sut = CreateSut();

        // Act
        var read = sut.Report;

        // Assert
        Assert.Same(report, read);
    }

    private VaultViewModel CreateSut() => new(mockVaults.Object, mockSession.Object, text.Errors, text.Plurals, text.For<ClypseText>(), mockCopy.Object, mockNavigation.Object, mockReport.Object, mockFiles.Object, guard, mockProfiles.Object);
}
