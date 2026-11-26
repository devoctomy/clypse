using Clypse.Core.Enums;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Settings;
using Clypse.Portal.Models.Vaults;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VaultReportServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IVaultSession> mockSession = new();
    private readonly Mock<IPasswordStrengthService> mockStrength = new();
    private readonly Mock<IVaultReportPreferencesService> mockPreferences = new();
    private readonly FakeTimeProvider clock = new(Now);
    private readonly List<Secret> secrets = [];
    private VaultReportPreferences preferences = new() { OldPasswordMonths = 3 };
    private Vault? vault = new() { Id = "vault-1", IsUnlocked = true };
    private bool isLoading;

    public VaultReportServiceTests()
    {
        mockSession.SetupGet(x => x.Vault).Returns(() => vault);
        mockSession.SetupGet(x => x.IsLoading).Returns(() => isLoading);
        mockSession.SetupGet(x => x.Secrets).Returns(() => secrets.ToList());
        mockPreferences.Setup(x => x.GetAsync()).ReturnsAsync(() => preferences);
        mockStrength.Setup(x => x.EstimateCheckingKnownPasswordsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? p, CancellationToken _) => p switch
            {
                "password" => new PasswordStrength { Estimation = PasswordComplexityEstimation.VeryWeak, IsKnownWeak = true },
                _ when p!.StartsWith("weak", StringComparison.Ordinal) => new PasswordStrength { Estimation = PasswordComplexityEstimation.Weak, Entropy = 30 },
                _ when p!.StartsWith("medium", StringComparison.Ordinal) => new PasswordStrength { Estimation = PasswordComplexityEstimation.Medium, Entropy = 50 },
                _ => new PasswordStrength { Estimation = PasswordComplexityEstimation.VeryStrong, Entropy = 120 },
            });
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new VaultReportService(null!, mockStrength.Object, mockPreferences.Object, clock)).ParamName);
        Assert.Equal("strengthService", Assert.Throws<ArgumentNullException>(() => new VaultReportService(mockSession.Object, null!, mockPreferences.Object, clock)).ParamName);
        Assert.Equal("preferencesService", Assert.Throws<ArgumentNullException>(() => new VaultReportService(mockSession.Object, mockStrength.Object, null!, clock)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() => new VaultReportService(mockSession.Object, mockStrength.Object, mockPreferences.Object, null!)).ParamName);
    }

    [Fact]
    public void GivenNoVaultOpen_WhenCreated_ThenTheReportIsEmpty()
    {
        // Arrange
        vault = null;

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.Same(VaultReport.Empty, sut.Report);
        Assert.False(sut.IsUpdating);
    }

    [Fact]
    public void GivenTheSecretsAreLoading_WhenCreated_ThenItWaits_WithoutEstimatingAnything()
    {
        // Arrange
        isLoading = true;
        secrets.Add(Web("One", password: "weak1"));

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.True(sut.IsUpdating);
        Assert.Equal("vault-1", sut.Report.VaultId);
        Assert.Null(sut.Report.GeneratedAt);
        mockStrength.VerifyNoOtherCalls();
    }

    [Fact]
    public void GivenTheSecretsLoaded_WhenTheSessionChanges_ThenTheReportIsMade()
    {
        // Arrange
        isLoading = true;
        using var sut = CreateSut();
        secrets.Add(Web("One", password: "weak1"));
        isLoading = false;

        // Act
        mockSession.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.False(sut.IsUpdating);
        Assert.Equal(Now, sut.Report.GeneratedAt);
        Assert.Equal(1, sut.Report.SecretCount);
        Assert.Single(sut.Report.WeakPasswords);
    }

    [Fact]
    public void GivenWeakPasswords_WhenReported_ThenWeakAndKnownWeakAreListed_InNameOrder()
    {
        // Arrange
        var strong = Web("Alpha", password: "Str0ng!Passw0rd#2026");
        var medium = Web("Bravo", password: "medium1");
        var known = Web("Charlie", password: "password");
        var weak = Web("Delta", password: "weak1");
        secrets.AddRange([weak, strong, known, medium]);

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.Equal([known.Id, weak.Id], sut.Report.WeakPasswords.Select(w => w.SecretId));
        Assert.True(sut.Report.WeakPasswords[0].Strength.IsKnownWeak);
        Assert.Equal(PasswordComplexityEstimation.Weak, sut.Report.WeakPasswords[1].Strength.Estimation);
    }

    [Fact]
    public void GivenReusedPasswords_WhenReported_ThenEachIsListedWithItsSecrets_MostUsedFirst()
    {
        // Arrange
        var a = Web("A", password: "Shared-One-2026!");
        var b = Web("B", password: "Shared-Two-2026!");
        var c = Web("C", password: "Shared-Two-2026!");
        var d = Web("D", password: "Shared-One-2026!");
        var e = Web("E", password: "Shared-One-2026!");
        var f = Web("F", password: "shared-one-2026!");
        secrets.AddRange([f, e, d, c, b, a]);

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.Equal(2, sut.Report.ReusedPasswords.Count);
        Assert.Equal([a.Id, d.Id, e.Id], sut.Report.ReusedPasswords[0].SecretIds);
        Assert.Equal([b.Id, c.Id], sut.Report.ReusedPasswords[1].SecretIds);
    }

    [Fact]
    public void GivenReusedUserNames_WhenReported_ThenThoseNotEmailAddressesAreListed_IgnoringCaseAndSpaces()
    {
        // Arrange
        var a = Web("A", userName: "bob");
        var b = Web("B", userName: " Bob ");
        var c = Web("C", userName: "bob@example.com");
        var d = Web("D", userName: "bob@example.com");
        var e = Web("E", userName: "alice");
        secrets.AddRange([a, b, c, d, e]);

        // Act
        using var sut = CreateSut();

        // Assert
        var reused = Assert.Single(sut.Report.ReusedUserNames);
        Assert.Equal([a.Id, b.Id], reused.SecretIds);
    }

    [Fact]
    public void GivenPasswordsOfDifferentAges_WhenReported_ThenThoseOlderThanTheChosenMonthsAreListed_OldestFirst()
    {
        // Arrange
        var recent = Web("Recent", password: "Recent-Password-1!", passwordUpdated: Now.AddMonths(-2));
        var old = Web("Old", password: "Old-Password-1!", passwordUpdated: Now.AddMonths(-4));
        var older = Web("Older", password: "Older-Password-1!", passwordUpdated: Now.AddYears(-1));
        var noPassword = Web("No password", userName: "carol", updated: Now.AddYears(-2));
        secrets.AddRange([recent, old, older, noPassword]);

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.Equal(3, sut.Report.OldPasswordMonths);
        Assert.Equal([older.Id, old.Id], sut.Report.OldPasswords.Select(o => o.SecretId));
        Assert.Equal(Now.AddYears(-1).UtcDateTime, sut.Report.OldPasswords[0].PasswordUpdatedAt);
    }

    [Fact]
    public void GivenAnOldPasswordInARecentlyEditedSecret_WhenReported_ThenItIsListed_ByWhenThePasswordChanged()
    {
        // Arrange
        var edited = Web("Edited", password: "Old-Password-1!", updated: Now.AddDays(-1), passwordUpdated: Now.AddMonths(-5));
        var changed = Web("Changed", password: "New-Password-1!", updated: Now.AddDays(-1), passwordUpdated: Now.AddDays(-1));
        secrets.AddRange([edited, changed]);

        // Act
        using var sut = CreateSut();

        // Assert
        var listed = Assert.Single(sut.Report.OldPasswords);
        Assert.Equal(edited.Id, listed.SecretId);
        Assert.Equal(Now.AddMonths(-5).UtcDateTime, listed.PasswordUpdatedAt);
    }

    [Fact]
    public void GivenASecretSavedBeforePasswordTimesWereKept_WhenReported_ThenItsLastUpdateIsUsed()
    {
        // Arrange
        var saved = Web("Saved before", password: "Old-Password-1!", updated: Now.AddMonths(-4));
        secrets.Add(saved);

        // Act
        using var sut = CreateSut();

        // Assert
        var listed = Assert.Single(sut.Report.OldPasswords);
        Assert.Equal(saved.Id, listed.SecretId);
        Assert.Equal(Now.AddMonths(-4).UtcDateTime, listed.PasswordUpdatedAt);
    }

    [Fact]
    public void GivenTheMonthsChanged_WhenThePreferencesAreSaved_ThenTheReportIsMadeAgain()
    {
        // Arrange
        var old = Web("Old", password: "Old-Password-1!", passwordUpdated: Now.AddMonths(-4));
        secrets.Add(old);
        using var sut = CreateSut();
        preferences = new VaultReportPreferences { OldPasswordMonths = 6 };

        // Act
        mockPreferences.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.Equal(6, sut.Report.OldPasswordMonths);
        Assert.Empty(sut.Report.OldPasswords);
    }

    [Fact]
    public void GivenSecretsWithoutPasswordsOrUserNames_WhenReported_ThenNothingIsFound()
    {
        // Arrange
        secrets.Add(new AwsCredentials { Name = "AWS", AccessKeyId = "AKIA", SecretAccessKey = "secret" });
        secrets.Add(new AwsCredentials { Name = "AWS 2", AccessKeyId = "AKIA", SecretAccessKey = "secret" });
        secrets.Add(Web("Web"));

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.Equal(3, sut.Report.SecretCount);
        Assert.False(sut.Report.HasIssues);
        mockStrength.VerifyNoOtherCalls();
    }

    [Fact]
    public void GivenAReport_WhenASecretIsAdded_ThenOnlyTheNewPasswordIsEstimated()
    {
        // Arrange
        secrets.Add(Web("One", password: "weak1"));
        using var sut = CreateSut();
        secrets.Add(Web("Two", password: "weak1"));
        secrets.Add(Web("Three", password: "weak2"));

        // Act
        mockSession.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.Equal(3, sut.Report.WeakPasswords.Count);
        Assert.Single(sut.Report.ReusedPasswords);
        mockStrength.Verify(x => x.EstimateCheckingKnownPasswordsAsync("weak1", It.IsAny<CancellationToken>()), Times.Once);
        mockStrength.Verify(x => x.EstimateCheckingKnownPasswordsAsync("weak2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GivenAReport_WhenAnotherVaultIsOpened_ThenItsPasswordsAreEstimatedAfresh()
    {
        // Arrange
        secrets.Add(Web("One", password: "weak1"));
        using var sut = CreateSut();
        vault = new Vault { Id = "vault-2", IsUnlocked = true };

        // Act
        mockSession.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.Equal("vault-2", sut.Report.VaultId);
        mockStrength.Verify(x => x.EstimateCheckingKnownPasswordsAsync("weak1", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void GivenAReport_WhenTheVaultIsClosed_ThenTheReportIsEmpty_AndChangedIsRaised()
    {
        // Arrange
        secrets.Add(Web("One", password: "weak1"));
        using var sut = CreateSut();
        var changes = 0;
        sut.Changed += (_, _) => changes++;
        vault = null;
        secrets.Clear();

        // Act
        mockSession.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.Same(VaultReport.Empty, sut.Report);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task GivenAReport_WhenAPasswordCannotBeEstimated_ThenTheLastReportStands_AndTheErrorIsThrown()
    {
        // Arrange
        secrets.Add(Web("One", password: "weak1"));
        using var sut = CreateSut();
        var report = sut.Report;
        secrets.Add(Web("Two", password: "fails"));
        mockStrength.Setup(x => x.EstimateCheckingKnownPasswordsAsync("fails", It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RefreshAsync());
        Assert.Same(report, sut.Report);
        Assert.False(sut.IsUpdating);
    }

    [Fact]
    public void GivenADisposedService_WhenTheSessionChanges_ThenTheReportIsNotMadeAgain()
    {
        // Arrange
        var sut = CreateSut();
        var report = sut.Report;
        secrets.Add(Web("One", password: "weak1"));

        // Act
        sut.Dispose();
        mockSession.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.Same(report, sut.Report);
        mockStrength.VerifyNoOtherCalls();
    }

    private static WebSecret Web(string name, string? userName = null, string? password = null, DateTimeOffset? updated = null, DateTimeOffset? passwordUpdated = null)
    {
        var secret = new WebSecret { Name = name, UserName = userName, Password = password };
        secret.LastUpdatedAt = (updated ?? Now).UtcDateTime;
        secret.PasswordUpdatedAt = passwordUpdated?.UtcDateTime;
        return secret;
    }

    private VaultReportService CreateSut() => new(mockSession.Object, mockStrength.Object, mockPreferences.Object, clock);
}
