using clypse.core.Enums;
using clypse.core.Password;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Enums;
using clypse.portal.Models.Settings;
using Moq;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class PasswordGeneratorDialogViewModelTests
{
    private readonly Mock<IPasswordGeneratorService> mockPasswordGenerator;
    private readonly AppSettings appSettings;

    public PasswordGeneratorDialogViewModelTests()
    {
        this.mockPasswordGenerator = new Mock<IPasswordGeneratorService>();
        this.appSettings = new AppSettings
        {
            MemorablePasswordTemplates =
            [
                new MemorablePasswordTemplateItem { Name = "Template1", Template = "{word}-{word}" }
            ]
        };

        this.mockPasswordGenerator
            .Setup(s => s.GenerateMemorablePasswordAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("hello-world");
        this.mockPasswordGenerator
            .Setup(s => s.GenerateRandomPassword(It.IsAny<CharacterGroup>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Returns("Rand0m!");
    }

    private PasswordGeneratorDialogViewModel CreateSut() =>
        new(this.mockPasswordGenerator.Object, this.appSettings);

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var sut = this.CreateSut();
        Assert.Equal(PasswordType.Memorable, sut.SelectedPasswordType);
        Assert.Equal(16, sut.PasswordLength);
        Assert.True(sut.ShuffleTokens);
        Assert.True(sut.AtLeastOneOfEachGroup);
    }

    [Fact]
    public async Task InitializeAsync_LoadsTemplatesAndGeneratesPassword()
    {
        var sut = this.CreateSut();

        await sut.InitializeAsync();

        Assert.Single(sut.MemorablePasswordTemplates);
        Assert.Equal("Template1", sut.SelectedTemplateName);
        Assert.NotEmpty(sut.GeneratedPassword);
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_OnlyInitializesOnce()
    {
        var sut = this.CreateSut();
        await sut.InitializeAsync();

        this.mockPasswordGenerator.Invocations.Clear();
        await sut.InitializeAsync();

        this.mockPasswordGenerator.Verify(
            s => s.GenerateMemorablePasswordAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Reset_AllowsReinitialization()
    {
        var sut = this.CreateSut();
        await sut.InitializeAsync();

        sut.Reset();
        await sut.InitializeAsync();

        this.mockPasswordGenerator.Verify(
            s => s.GenerateMemorablePasswordAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    public async Task AcceptPasswordCommand_InvokesCallback()
    {
        var sut = this.CreateSut();
        await sut.InitializeAsync();
        string? received = null;
        sut.OnPasswordGeneratedCallback = p => { received = p; return Task.CompletedTask; };

        await sut.AcceptPasswordCommand.ExecuteAsync(null);

        Assert.NotNull(received);
    }

    [Fact]
    public async Task CancelCommand_InvokesCallback()
    {
        var sut = this.CreateSut();
        var called = false;
        sut.OnCancelCallback = () => { called = true; return Task.CompletedTask; };

        await sut.CancelCommand.ExecuteAsync(null);

        Assert.True(called);
    }

    [Fact]
    public async Task SelectedPasswordType_WhenChangedToRandom_GeneratesRandomPassword()
    {
        var sut = this.CreateSut();
        await sut.InitializeAsync();

        sut.SelectedPasswordType = PasswordType.Random;

        await Task.Delay(50); // let async fire
        this.mockPasswordGenerator.Verify(
            s => s.GenerateRandomPassword(It.IsAny<CharacterGroup>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.AtLeastOnce);
    }
}
