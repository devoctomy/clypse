using Clypse.Core.Enums;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Testing;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class SecretEditorViewModelTests
{
    private static readonly PasswordStrength Strong = new() { Estimation = PasswordComplexityEstimation.Strong, Entropy = 100 };
    private static readonly PasswordStrength KnownWeak = new() { Estimation = PasswordComplexityEstimation.VeryWeak, Entropy = 50, IsKnownWeak = true };

    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultSession> mockSession = new();
    private readonly Mock<IPasswordStrengthService> mockStrength = new();
    private readonly Mock<ISecretCopyService> mockCopy = new();
    private readonly FakeTimeProvider time = new();
    private readonly List<Secret> secrets = [];
    private Secret? saved;

    public SecretEditorViewModelTests()
    {
        mockSession.SetupGet(x => x.Secrets).Returns(() => secrets);
        mockSession.Setup(x => x.SaveAsync(It.IsAny<Secret>(), It.IsAny<CancellationToken>()))
            .Callback<Secret, CancellationToken>((s, _) => saved = s)
            .ReturnsAsync((Secret s, CancellationToken _) => s);
        mockStrength.Setup(x => x.EstimateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? p, CancellationToken _) => string.IsNullOrEmpty(p) ? PasswordStrength.None : Strong);
        mockStrength.Setup(x => x.EstimateCheckingKnownPasswordsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? p, CancellationToken _) => p == "password123" ? KnownWeak : Strong);
        mockStrength.Setup(x => x.Describe(It.IsAny<PasswordStrength>())).Returns((PasswordStrength s) => s.Estimation.ToString());
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new SecretEditorViewModel(null!, mockStrength.Object, text.Errors, mockCopy.Object, time)).ParamName);
        Assert.Equal("strengthService", Assert.Throws<ArgumentNullException>(() => new SecretEditorViewModel(mockSession.Object, null!, text.Errors, mockCopy.Object, time)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new SecretEditorViewModel(mockSession.Object, mockStrength.Object, null!, mockCopy.Object, time)).ParamName);
        Assert.Equal("copyService", Assert.Throws<ArgumentNullException>(() => new SecretEditorViewModel(mockSession.Object, mockStrength.Object, text.Errors, null!, time)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() => new SecretEditorViewModel(mockSession.Object, mockStrength.Object, text.Errors, mockCopy.Object, null!)).ParamName);
    }

    [Fact]
    public async Task GivenNoSecret_WhenLoaded_ThenANewLoginWithItsFieldsInOrder()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(null);

        // Assert
        Assert.True(sut.IsNew);
        Assert.Equal(SecretType.Web, sut.SecretType);
        Assert.Null(sut.LastUpdatedAt);
        Assert.Equal(["Name", "Description", "UserName", "EmailAddress", "WebsiteUrl", "LoginUrl", "Password", "Tags", "Comments"], sut.Fields.Select(f => f.Name));
        Assert.True(sut.Fields.Single(f => f.Name == "Name").IsRequired);
        Assert.False(sut.Fields.Single(f => f.Name == "UserName").IsRequired);
        Assert.Equal(SecretFieldType.Password, sut.Fields.Single(f => f.Name == "Password").FieldType);
    }

    [Fact]
    public async Task GivenANewSecret_WhenItsKindChanges_ThenItsFieldsChange_KeepingSharedValues()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        await sut.SetValueAsync("Name", "Deploy");
        await sut.SetValueAsync("UserName", "bob");

        // Act
        sut.SelectType(SecretType.Aws);

        // Assert
        Assert.Equal(SecretType.Aws, sut.SecretType);
        Assert.Contains(sut.Fields, f => f.Name == "AccessKeyId");
        Assert.DoesNotContain(sut.Fields, f => f.Name == "UserName");
        Assert.Equal("Deploy", sut.GetValue("Name"));
    }

    [Fact]
    public async Task GivenASecret_WhenLoaded_ThenACopyOfItsValues_AndItsKindCannotChange()
    {
        // Arrange
        var secret = new WebSecret { Name = "Gmail", UserName = "bob", Password = "hunter2" };
        secrets.Add(secret);
        var sut = CreateSut();

        // Act
        await sut.LoadAsync(secret.Id);
        sut.SelectType(SecretType.Aws);

        // Assert
        Assert.False(sut.IsNew);
        Assert.Equal(SecretType.Web, sut.SecretType);
        Assert.Equal("Gmail", sut.GetValue("Name"));
        Assert.Equal("hunter2", sut.GetValue("Password"));
        Assert.Equal(Strong, sut.GetStrength("Password"));
        Assert.Equal("Strong", sut.GetStrengthText("Password"));
        Assert.Equal(DateTimeKind.Utc, sut.LastUpdatedAt!.Value.Kind);
        await sut.SetValueAsync("Name", "Changed");
        Assert.Equal("Gmail", secret.Name);
    }

    [Fact]
    public async Task GivenAnEditedSecret_WhenSaved_ThenTheSameSecretIsSavedWithItsNewValues()
    {
        // Arrange
        var secret = new WebSecret { Name = "Gmail", UserName = "bob", Password = " pass with spaces " };
        secret.UpdateTags(["old"]);
        secrets.Add(secret);
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);
        await sut.SetValueAsync("Name", "  Google mail  ");
        await sut.SetValueAsync("Tags", " email, , Personal,email ");
        await sut.SetValueAsync("UserName", string.Empty);

        // Act
        var result = await sut.SaveAsync();

        // Assert
        Assert.True(result);
        var web = Assert.IsType<WebSecret>(saved);
        Assert.Equal(secret.Id, web.Id);
        Assert.Equal(secret.CreatedAt, web.CreatedAt);
        Assert.Equal("Google mail", web.Name);
        Assert.Equal(" pass with spaces ", web.Password);
        Assert.Equal(["email", "Personal"], web.Tags);
        Assert.Null(web.UserName);
    }

    [Fact]
    public async Task GivenANewAwsSecret_WhenSaved_ThenAnAwsSecretIsSaved()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        sut.SelectType(SecretType.Aws);
        await sut.SetValueAsync("Name", "Deploy");
        await sut.SetValueAsync("AccessKeyId", "AKIA");

        // Act
        await sut.SaveAsync();

        // Assert
        var aws = Assert.IsType<AwsCredentials>(saved);
        Assert.Equal(SecretType.Aws, aws.SecretType);
        Assert.Equal("AKIA", aws.AccessKeyId);
    }

    [Fact]
    public async Task GivenSavingFails_WhenSaved_ThenTheErrorIsShown()
    {
        // Arrange
        mockSession.Setup(x => x.SaveAsync(It.IsAny<Secret>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClypseException(ClypseErrorCodes.SecretInvalid, "Name"));
        var sut = CreateSut();
        await sut.LoadAsync(null);

        // Act
        var result = await sut.SaveAsync();

        // Assert
        Assert.False(result);
        Assert.Equal("The secret is missing: Name.", sut.Error);
        Assert.False(sut.IsWorking);
    }

    [Fact]
    public async Task GivenAPasswordField_WhenSet_ThenItsStrengthIsEstimated_ButNotForOtherFields()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        mockStrength.Invocations.Clear();

        // Act
        await sut.SetValueAsync("Password", "correct horse");
        await sut.SetValueAsync("Name", "Gmail");

        // Assert
        Assert.Equal(Strong, sut.GetStrength("Password"));
        Assert.Same(PasswordStrength.None, sut.GetStrength("Name"));
        mockStrength.Verify(x => x.EstimateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenAPasswordField_WhenRevealedTwice_ThenItIsHiddenAgain()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);

        // Act & Assert
        Assert.False(sut.IsRevealed("Password"));
        sut.ToggleReveal("Password");
        Assert.True(sut.IsRevealed("Password"));
        sut.ToggleReveal("Password");
        Assert.False(sut.IsRevealed("Password"));
    }

    [Fact]
    public async Task GivenTheGenerator_WhenAPasswordIsChosen_ThenItIsPutInTheFieldAndShown()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        sut.OpenGenerator("Password");

        // Act
        await sut.CloseGeneratorAsync("generated!");

        // Assert
        Assert.Null(sut.GeneratingFor);
        Assert.Equal("generated!", sut.GetValue("Password"));
        Assert.True(sut.IsRevealed("Password"));
        Assert.Equal(Strong, sut.GetStrength("Password"));
    }

    [Fact]
    public async Task GivenTheGenerator_WhenClosedWithoutChoosing_ThenTheFieldIsUnchanged()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        await sut.SetValueAsync("Password", "mine");
        sut.OpenGenerator("Password");

        // Act
        await sut.CloseGeneratorAsync(null);

        // Assert
        Assert.Equal("mine", sut.GetValue("Password"));
        Assert.False(sut.IsRevealed("Password"));
    }

    [Fact]
    public async Task GivenASecret_WhenDeleteIsConfirmed_ThenItIsDeleted()
    {
        // Arrange
        var secret = new Secret { Name = "Gone" };
        secrets.Add(secret);
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);
        sut.ConfirmDelete(true);
        Assert.True(sut.IsConfirmingDelete);

        // Act
        var result = await sut.DeleteAsync();

        // Assert
        Assert.True(result);
        Assert.False(sut.IsConfirmingDelete);
        mockSession.Verify(x => x.DeleteAsync(secret.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenANewSecret_WhenDeleted_ThenNothingHappens()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);

        // Act
        sut.ConfirmDelete(true);
        var result = await sut.DeleteAsync();

        // Assert
        Assert.False(sut.IsConfirmingDelete);
        Assert.False(result);
        mockSession.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenDeletingFails_WhenDeleted_ThenTheErrorIsShown()
    {
        // Arrange
        var secret = new Secret { Name = "Gone" };
        secrets.Add(secret);
        mockSession.Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new ClypseException(ClypseErrorCodes.NoVaultOpen));
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);

        // Act
        var result = await sut.DeleteAsync();

        // Assert
        Assert.False(result);
        Assert.Equal("Open a vault first.", sut.Error);
    }

    [Fact]
    public async Task GivenAnEditor_WhenLoadedAgain_ThenItStartsAfresh()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        await sut.SetValueAsync("Name", "Draft");
        sut.ToggleReveal("Password");
        sut.OpenGenerator("Password");

        // Act
        await sut.LoadAsync(null);

        // Assert
        Assert.Equal(string.Empty, sut.GetValue("Name"));
        Assert.False(sut.IsRevealed("Password"));
        Assert.Null(sut.GeneratingFor);
    }

    [Fact]
    public async Task GivenFields_WhenCopied_ThenSecretValuesAreCopiedAsSecretsAndOthersAreNot()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        await sut.SetValueAsync("UserName", "bob");
        await sut.SetValueAsync("Password", "hunter2");

        // Act
        await sut.CopyAsync("UserName");
        await sut.CopyAsync("Password");

        // Assert
        mockCopy.Verify(x => x.CopyAsync("UserName", "bob", false), Times.Once);
        mockCopy.Verify(x => x.CopyAsync("Password", "hunter2", true), Times.Once);
    }

    [Fact]
    public async Task GivenAnAwsSecret_WhenLoaded_ThenItsSecretAccessKeyIsASecretValue()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);

        // Act
        sut.SelectType(SecretType.Aws);

        // Assert
        Assert.True(sut.Fields.Single(f => f.Name == "SecretAccessKey").IsSecretValue);
        Assert.False(sut.Fields.Single(f => f.Name == "AccessKeyId").IsSecretValue);
    }

    [Fact]
    public async Task GivenAKnownWeakPassword_WhenTypingPauses_ThenItIsShownAsKnownWeak()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        var changed = WaitForAllPropertiesChanged(sut);

        // Act
        await sut.SetValueAsync("Password", "password123");
        var before = sut.GetStrength("Password");
        time.Advance(SecretEditorViewModel.KnownPasswordCheckDelay);
        await changed;

        // Assert
        Assert.Equal(Strong, before);
        Assert.Equal(KnownWeak, sut.GetStrength("Password"));
    }

    [Fact]
    public async Task GivenAPasswordChangedBeforeTypingPauses_WhenThePauseComes_ThenOnlyTheLastValueIsChecked()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        mockStrength.Invocations.Clear();
        var changed = WaitForAllPropertiesChanged(sut);

        // Act
        await sut.SetValueAsync("Password", "password123");
        time.Advance(SecretEditorViewModel.KnownPasswordCheckDelay / 2);
        await sut.SetValueAsync("Password", "password1234");
        time.Advance(SecretEditorViewModel.KnownPasswordCheckDelay);
        await changed;

        // Assert
        Assert.Equal(Strong, sut.GetStrength("Password"));
        mockStrength.Verify(x => x.EstimateCheckingKnownPasswordsAsync("password1234", It.IsAny<CancellationToken>()), Times.Once);
        mockStrength.Verify(x => x.EstimateCheckingKnownPasswordsAsync("password123", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenACheckWaiting_WhenAnotherSecretIsLoaded_ThenTheCheckIsCancelled()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        await sut.SetValueAsync("Password", "password123");
        mockStrength.Invocations.Clear();

        // Act
        await sut.LoadAsync(null);
        time.Advance(SecretEditorViewModel.KnownPasswordCheckDelay);
        await Task.Yield();

        // Assert
        mockStrength.Verify(x => x.EstimateCheckingKnownPasswordsAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Same(PasswordStrength.None, sut.GetStrength("Password"));
    }

    [Fact]
    public async Task GivenAField_WhenSetToNull_ThenItIsEmpty()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        await sut.SetValueAsync("Name", "Gmail");

        // Act
        await sut.SetValueAsync("Name", null);

        // Assert
        Assert.Equal(string.Empty, sut.GetValue("Name"));
    }

    [Fact]
    public async Task GivenANameThatIsNoField_WhenCopied_ThenItIsNotCopiedAsASecret()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);

        // Act
        await sut.CopyAsync("NotAField");

        // Assert
        mockCopy.Verify(x => x.CopyAsync("NotAField", It.IsAny<string>(), false), Times.Once);
    }

    [Fact]
    public async Task GivenTheGeneratorIsNotOpen_WhenClosedWithAPassword_ThenNoFieldChanges()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync(null);
        await sut.SetValueAsync("Password", "mine");

        // Act
        await sut.CloseGeneratorAsync("generated");

        // Assert
        Assert.Equal("mine", sut.GetValue("Password"));
        Assert.Null(sut.GeneratingFor);
    }

    [Fact]
    public async Task GivenASaveRunning_WhenSavedOrDeletedAgain_ThenNeitherHappens()
    {
        // Arrange
        var secret = new Secret { Name = "Gmail" };
        secrets.Add(secret);
        var saving = new TaskCompletionSource<Secret>();
        mockSession.Setup(x => x.SaveAsync(It.IsAny<Secret>(), It.IsAny<CancellationToken>())).Returns(saving.Task);
        var sut = CreateSut();
        await sut.LoadAsync(secret.Id);
        var first = sut.SaveAsync();

        // Act
        var second = await sut.SaveAsync();
        var deleted = await sut.DeleteAsync();
        saving.SetResult(secret);

        // Assert
        Assert.False(second);
        Assert.False(deleted);
        Assert.True(await first);
        mockSession.Verify(x => x.SaveAsync(It.IsAny<Secret>(), It.IsAny<CancellationToken>()), Times.Once);
        mockSession.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Task WaitForAllPropertiesChanged(SecretEditorViewModel sut)
    {
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.PropertyChanged += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.PropertyName))
            {
                changed.TrySetResult();
            }
        };

        return changed.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    private SecretEditorViewModel CreateSut() => new(mockSession.Object, mockStrength.Object, text.Errors, mockCopy.Object, time);
}
