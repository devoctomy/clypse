using Clypse.Core.Enums;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class VaultReportViewModelTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultReportService> mockReport = new();
    private readonly Mock<IVaultSession> mockSession = new();
    private readonly Mock<IPasswordStrengthService> mockStrength = new();
    private readonly List<Secret> secrets = [];
    private VaultReport report = new() { VaultId = "v1", GeneratedAt = DateTimeOffset.UtcNow, OldPasswordMonths = 3 };

    public VaultReportViewModelTests()
    {
        mockReport.SetupGet(x => x.Report).Returns(() => report);
        mockSession.SetupGet(x => x.Secrets).Returns(() => secrets);
        mockStrength.Setup(x => x.Describe(It.IsAny<PasswordStrength>())).Returns("Weak, about 40 bits");
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("reportService", Assert.Throws<ArgumentNullException>(() => new VaultReportViewModel(null!, mockSession.Object, mockStrength.Object, text.Plurals, t)).ParamName);
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new VaultReportViewModel(mockReport.Object, null!, mockStrength.Object, text.Plurals, t)).ParamName);
        Assert.Equal("strengthService", Assert.Throws<ArgumentNullException>(() => new VaultReportViewModel(mockReport.Object, mockSession.Object, null!, text.Plurals, t)).ParamName);
        Assert.Equal("plurals", Assert.Throws<ArgumentNullException>(() => new VaultReportViewModel(mockReport.Object, mockSession.Object, mockStrength.Object, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new VaultReportViewModel(mockReport.Object, mockSession.Object, mockStrength.Object, text.Plurals, null!)).ParamName);
    }

    [Fact]
    public void GivenNoReportYet_WhenShown_ThenItIsNotReady()
    {
        // Arrange
        report = new VaultReport { VaultId = "v1" };

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.False(sut.IsReady);
    }

    [Fact]
    public void GivenACleanReport_WhenShown_ThenEveryCheckPasses_InOrder_WithItsGoodNews()
    {
        // Arrange
        report = new VaultReport { VaultId = "v1", GeneratedAt = DateTimeOffset.UtcNow, SecretCount = 12, OldPasswordMonths = 3 };

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.True(sut.IsReady);
        Assert.True(sut.AllPassed);
        Assert.Equal("5 of 5 checks passed", sut.PassedText);
        Assert.Equal("12 secrets checked.", sut.CheckedText);
        Assert.Equal(
            [VaultReportCheck.KnownWeakPasswords, VaultReportCheck.WeakPasswords, VaultReportCheck.ReusedPasswords, VaultReportCheck.ReusedUserNames, VaultReportCheck.OldPasswords],
            sut.Sections.Select(s => s.Check));
        Assert.All(sut.Sections, s => Assert.True(s.Passed));
        Assert.Equal("No known weak passwords found.", sut.Sections[0].Summary);
        Assert.Equal("Passwords not changed in 3 months", sut.Sections[4].Title);
    }

    [Fact]
    public void GivenWeakPasswords_WhenShown_ThenKnownWeakAndWeakAreSeparate_AndTheWeakSayHowWeak()
    {
        // Arrange
        var known = Add("Forum", "quietowl");
        var weak = Add("Recipes", "cookbook");
        report = Report(weakPasswords:
        [
            new VaultReportWeakPassword { SecretId = known.Id, Strength = new PasswordStrength { Estimation = PasswordComplexityEstimation.VeryWeak, IsKnownWeak = true } },
            new VaultReportWeakPassword { SecretId = weak.Id, Strength = new PasswordStrength { Estimation = PasswordComplexityEstimation.Weak, Entropy = 40 } },
        ]);

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.False(sut.AllPassed);
        Assert.Equal("3 of 5 checks passed", sut.PassedText);
        var knownSection = sut.Sections[0];
        Assert.False(knownSection.Passed);
        Assert.Equal("1 secret uses a known weak password.", knownSection.Summary);
        Assert.Equal("Forum", Assert.Single(Assert.Single(knownSection.Items).Secrets).Name);
        var weakItem = Assert.Single(sut.Sections[1].Items);
        Assert.Equal("Weak, about 40 bits", weakItem.Detail);
        Assert.Equal(weak.Id, Assert.Single(weakItem.Secrets).Id);
    }

    [Fact]
    public void GivenSeveralKnownWeakPasswords_WhenShown_ThenTheyAreOneItem_CountedBySecret()
    {
        // Arrange
        var forum = Add("Forum", "quietowl");
        var laptop = Add("Laptop", "laptopuser");
        var knownWeak = new PasswordStrength { Estimation = PasswordComplexityEstimation.VeryWeak, IsKnownWeak = true };
        report = Report(weakPasswords:
        [
            new VaultReportWeakPassword { SecretId = forum.Id, Strength = knownWeak },
            new VaultReportWeakPassword { SecretId = laptop.Id, Strength = knownWeak },
        ]);

        // Act
        using var sut = CreateSut();

        // Assert
        var section = sut.Sections[0];
        Assert.Equal("2 secrets use known weak passwords.", section.Summary);
        Assert.Equal(["Forum", "Laptop"], Assert.Single(section.Items).Secrets.Select(s => s.Name));
    }

    [Fact]
    public void GivenReusedPasswordsAndUserNames_WhenShown_ThenEachGroupNamesItsSecrets_AndTheUserName()
    {
        // Arrange
        var a = Add("Music", "jsmith88");
        var b = Add("Video", "JSmith88");
        var c = Add("Travel", "wanderer");
        report = Report(
            reusedPasswords: [new VaultReportReuse { SecretIds = [a.Id, b.Id, c.Id] }],
            reusedUserNames: [new VaultReportReuse { SecretIds = [a.Id, b.Id] }]);

        // Act
        using var sut = CreateSut();

        // Assert
        var passwords = Assert.Single(sut.Sections[2].Items);
        Assert.Equal("Used by 3 secrets", passwords.Heading);
        Assert.Equal(["Music", "Video", "Travel"], passwords.Secrets.Select(s => s.Name));
        Assert.Equal("1 password is used by more than one secret.", sut.Sections[2].Summary);
        var userNames = Assert.Single(sut.Sections[3].Items);
        Assert.Equal("jsmith88", userNames.Heading);
        Assert.Equal("Used by 2 secrets", userNames.Detail);
    }

    [Fact]
    public void GivenAReportBeingMadeAgain_WhenShown_ThenItSaysSo()
    {
        // Arrange
        mockReport.SetupGet(x => x.IsUpdating).Returns(true);

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.True(sut.IsUpdating);
    }

    [Fact]
    public void GivenAReusedUserNameWhoseFirstSecretHasGone_WhenShown_ThenItHasNoHeading_ButNamesTheSecretsLeft()
    {
        // Arrange
        var b = Add("Video", "jsmith88");
        var c = Add("Books", "jsmith88");
        report = Report(reusedUserNames: [new VaultReportReuse { SecretIds = [Guid.NewGuid().ToString(), b.Id, c.Id] }]);

        // Act
        using var sut = CreateSut();

        // Assert
        var item = Assert.Single(sut.Sections[3].Items);
        Assert.Null(item.Heading);
        Assert.Equal(["Video", "Books"], item.Secrets.Select(s => s.Name));
    }

    [Fact]
    public void GivenAReusedUserNameWhoseFirstSecretNoLongerHasOne_WhenShown_ThenItHasNoHeading()
    {
        // Arrange
        var a = new WebSecret { Name = "Music", Password = "x" };
        secrets.Add(a);
        var b = Add("Video", "jsmith88");
        report = Report(reusedUserNames: [new VaultReportReuse { SecretIds = [a.Id, b.Id] }]);

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.Null(Assert.Single(sut.Sections[3].Items).Heading);
    }

    [Fact]
    public void GivenOldPasswords_WhenShown_ThenEachSaysWhenItWasLastChanged()
    {
        // Arrange
        var old = Add("Laptop", "laptopuser");
        var changed = new DateTime(2024, 11, 26, 10, 0, 0, DateTimeKind.Utc);
        report = Report(oldPasswords: [new VaultReportOldPassword { SecretId = old.Id, PasswordUpdatedAt = changed }]);

        // Act
        using var sut = CreateSut();

        // Assert
        var item = Assert.Single(sut.Sections[4].Items);
        Assert.Equal("Last changed " + changed.ToLocalTime().ToString("d", System.Globalization.CultureInfo.CurrentCulture), item.Detail);
        Assert.Equal("1 secret has an old password.", sut.Sections[4].Summary);
    }

    [Fact]
    public void GivenASecretDeletedSinceTheReport_WhenShown_ThenItIsLeftOut()
    {
        // Arrange
        report = Report(weakPasswords: [new VaultReportWeakPassword { SecretId = Guid.NewGuid().ToString(), Strength = new PasswordStrength { Estimation = PasswordComplexityEstimation.Weak } }]);

        // Act
        using var sut = CreateSut();

        // Assert
        Assert.True(sut.Sections[1].Passed);
    }

    [Fact]
    public void GivenAReport_WhenShown_ThenEverySectionIsClosed_AndToggleOpensAndClosesOne()
    {
        // Arrange
        using var sut = CreateSut();
        var closedAtFirst = sut.Sections.All(s => !sut.IsExpanded(s.Check));

        // Act
        sut.ToggleCommand.Execute(VaultReportCheck.ReusedPasswords);
        var opened = sut.IsExpanded(VaultReportCheck.ReusedPasswords);
        sut.ToggleCommand.Execute(VaultReportCheck.ReusedPasswords);

        // Assert
        Assert.True(closedAtFirst);
        Assert.True(opened);
        Assert.False(sut.IsExpanded(VaultReportCheck.ReusedPasswords));
        Assert.False(sut.IsExpanded(VaultReportCheck.WeakPasswords));
    }

    [Fact]
    public void GivenAnOpenDialog_WhenTheReportIsMadeAgain_ThenItShowsTheNewReport_AndKeepsOpenSectionsOpen()
    {
        // Arrange
        var weak = Add("Recipes", "cookbook");
        using var sut = CreateSut();
        sut.ToggleCommand.Execute(VaultReportCheck.WeakPasswords);
        report = Report(weakPasswords: [new VaultReportWeakPassword { SecretId = weak.Id, Strength = new PasswordStrength { Estimation = PasswordComplexityEstimation.Weak } }]);

        // Act
        mockReport.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.False(sut.Sections[1].Passed);
        Assert.True(sut.IsExpanded(VaultReportCheck.WeakPasswords));
    }

    [Fact]
    public void GivenADisposedDialog_WhenTheReportChanges_ThenItIsNotShownAgain()
    {
        // Arrange
        var weak = Add("Recipes", "cookbook");
        var sut = CreateSut();
        var sections = sut.Sections;
        report = Report(weakPasswords: [new VaultReportWeakPassword { SecretId = weak.Id, Strength = new PasswordStrength { Estimation = PasswordComplexityEstimation.Weak } }]);

        // Act
        sut.Dispose();
        mockReport.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.Same(sections, sut.Sections);
    }

    private static VaultReport Report(
        IReadOnlyList<VaultReportWeakPassword>? weakPasswords = null,
        IReadOnlyList<VaultReportReuse>? reusedPasswords = null,
        IReadOnlyList<VaultReportReuse>? reusedUserNames = null,
        IReadOnlyList<VaultReportOldPassword>? oldPasswords = null) => new()
        {
            VaultId = "v1",
            GeneratedAt = DateTimeOffset.UtcNow,
            OldPasswordMonths = 3,
            WeakPasswords = weakPasswords ?? [],
            ReusedPasswords = reusedPasswords ?? [],
            ReusedUserNames = reusedUserNames ?? [],
            OldPasswords = oldPasswords ?? [],
        };

    private WebSecret Add(string name, string userName)
    {
        var secret = new WebSecret { Name = name, UserName = userName, Password = "x" };
        secrets.Add(secret);
        return secret;
    }

    private VaultReportViewModel CreateSut() => new(mockReport.Object, mockSession.Object, mockStrength.Object, text.Plurals, text.For<ClypseText>());
}
