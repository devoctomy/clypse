using Clypse.Core.Secrets;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Files;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Sync;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VaultFileHistoryServiceTests
{
    private const string FileId = "2b0f8f3e-4c1d-4f6e-9a57-0d3c2b1a0e9f";
    private const string ContentKey = "profiles/vault-1/files/blobs/" + FileId + ".bin";
    private const string SecretId = "6f1c2d3e-4b5a-4c6d-8e7f-9a0b1c2d3e4f";
    private const string SecretKey = "profiles/vault-1/secrets/" + SecretId + ".json";

    private readonly Mock<ISyncProfileRegistry> mockRegistry = new();
    private readonly Mock<IS3SyncTargetProvider> mockTarget = new();
    private readonly Mock<ISyncUserContext> mockUser = new();
    private readonly Mock<ISyncFileStore> mockStore = new();
    private readonly Mock<IVaultFileSession> mockSession = new();
    private readonly Mock<IVaultSession> mockSecrets = new();

    public VaultFileHistoryServiceTests()
    {
        mockRegistry.Setup(x => x.GetFileStore(It.IsAny<string>())).Returns(mockStore.Object);
        mockSession.SetupGet(x => x.Vault).Returns(new Vault { Id = "vault-1" });
        mockSession.Setup(x => x.GetContentId(ContentKey)).Returns(FileId);
        mockSession.Setup(x => x.SaveContentAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VaultFileEntry { Id = FileId, Kind = VaultFileKind.File });
        mockSecrets.SetupGet(x => x.Vault).Returns(new Vault { Id = "vault-1" });
        mockSecrets.Setup(x => x.GetSecretId(SecretKey)).Returns(SecretId);
        mockSecrets.Setup(x => x.SaveAsync(It.IsAny<Secret>(), It.IsAny<CancellationToken>())).ReturnsAsync((Secret s, CancellationToken _) => s);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var history = CreateHistory();

        // Act & Assert
        Assert.Equal("history", Assert.Throws<ArgumentNullException>(() => new VaultFileHistoryService(null!, mockSecrets.Object, mockSession.Object)).ParamName);
        Assert.Equal("secrets", Assert.Throws<ArgumentNullException>(() => new VaultFileHistoryService(history, null!, mockSession.Object)).ParamName);
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new VaultFileHistoryService(history, mockSecrets.Object, null!)).ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GivenABucketOrNone_WhenAskedIfThereIsHistory_ThenBlazwizsAnswerIsGiven(bool configured)
    {
        // Arrange
        mockTarget.SetupGet(x => x.IsConfigured).Returns(configured);
        var sut = CreateSut();

        // Act
        var available = sut.IsAvailable;

        // Assert
        Assert.Equal(configured, available);
    }

    [Fact]
    public async Task GivenAVersionOfAFilesContentInTheOpenVault_WhenRestored_ThenItIsSavedWithTheFilesDetails()
    {
        // Arrange
        byte[] content = [1, 2, 3];
        var sut = CreateSut();

        // Act
        await sut.RestoreAsync(Version("vault-1", ContentKey, content));

        // Assert
        mockSession.Verify(x => x.SaveContentAsync(FileId, content, It.IsAny<CancellationToken>()), Times.Once);
        mockStore.Verify(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenAVersionOfASecretInTheOpenVault_WhenRestored_ThenItIsSavedAsTheVaultsSecret()
    {
        // Arrange
        byte[] content = [7, 8, 9];
        var restored = new WebSecret { Name = "Mail" };
        mockSecrets.Setup(x => x.ReadSecret(SecretId, content)).Returns(restored);
        var sut = CreateSut();

        // Act
        await sut.RestoreAsync(Version("vault-1", SecretKey, content));

        // Assert
        mockSecrets.Verify(x => x.SaveAsync(restored, It.IsAny<CancellationToken>()), Times.Once);
        mockSession.Verify(x => x.SaveContentAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        mockStore.Verify(x => x.WriteAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("vault-2", ContentKey)]
    [InlineData("vault-2", SecretKey)]
    [InlineData("vault-1", "profiles/vault-1/files/meta/" + FileId + ".json")]
    public async Task GivenAVersionOfAnythingElse_WhenRestored_ThenBlazwizRestoresIt(string profile, string key)
    {
        // Arrange
        byte[] content = [1, 2, 3];
        var sut = CreateSut();

        // Act
        await sut.RestoreAsync(Version(profile, key, content));

        // Assert
        mockStore.Verify(x => x.WriteAsync(key, content, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()), Times.Once);
        mockSession.Verify(x => x.SaveContentAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        mockSecrets.Verify(x => x.SaveAsync(It.IsAny<Secret>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNoVersion_WhenRestored_ThenThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.RestoreAsync(null!));
    }

    private static SyncFileVersionContent Version(string profile, string key, byte[] content)
    {
        return new SyncFileVersionContent
        {
            Version = new SyncFileVersion { Profile = profile, Key = key, VersionId = "v1", ETag = "\"abc\"" },
            Content = content,
        };
    }

    private SyncFileHistoryService CreateHistory() => new(mockRegistry.Object, mockTarget.Object, mockUser.Object);

    private VaultFileHistoryService CreateSut() => new(CreateHistory(), mockSecrets.Object, mockSession.Object);
}
