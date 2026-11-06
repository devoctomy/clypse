using System.Text;
using System.Text.Json;
using Clypse.Core.Enums;
using Clypse.Core.Secrets;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Sync.InMemory;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class SecretServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 30, 15, TimeSpan.Zero);

    private readonly Mock<ISyncUserContext> mockUser = new();
    private readonly FakeTimeProvider clock = new(Now);
    private readonly InMemorySyncFileStore files;
    private readonly Vault vault;

    public SecretServiceTests()
    {
        mockUser.Setup(x => x.GetUserIdAsync()).ReturnsAsync("bob");
        files = new InMemorySyncFileStore(mockUser.Object, clock);
        var id = Guid.NewGuid().ToString("D");
        vault = new Vault { Id = id, Root = $"profiles/{id}/", Name = "Personal", IsUnlocked = true };
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("vault", Assert.Throws<ArgumentNullException>(() => new SecretService(null!, files, clock)).ParamName);
        Assert.Equal("fileStore", Assert.Throws<ArgumentNullException>(() => new SecretService(vault, null!, clock)).ParamName);
        Assert.Equal("timeProvider", Assert.Throws<ArgumentNullException>(() => new SecretService(vault, files, null!)).ParamName);
    }

    [Fact]
    public async Task GivenAWebSecret_WhenSavedAndRead_ThenItComesBackAsAWebSecretWithItsFields()
    {
        // Arrange
        var sut = CreateSut();
        var secret = new WebSecret { Name = "Gmail", UserName = "bob", Password = "hunter2", WebsiteUrl = "https://mail.google.com" };
        secret.AddTag("email");

        // Act
        await sut.SaveAsync(secret);
        var read = await sut.GetAsync(secret.Id);

        // Assert
        var web = Assert.IsType<WebSecret>(read);
        Assert.Equal("Gmail", web.Name);
        Assert.Equal("bob", web.UserName);
        Assert.Equal("hunter2", web.Password);
        Assert.Equal("https://mail.google.com", web.WebsiteUrl);
        Assert.Contains("email", web.Tags);
        Assert.Equal(SecretType.Web, web.SecretType);
    }

    [Fact]
    public async Task GivenASecret_WhenSaved_ThenItIsOneFileNamedByItsId_HoldingOnlyItsData()
    {
        // Arrange
        var secret = new WebSecret { Name = "Gmail", Password = "hunter2" };

        // Act
        await CreateSut().SaveAsync(secret);

        // Assert
        var file = await files.ReadAsync($"{vault.Root}secrets/{secret.Id}.json");
        Assert.NotNull(file);
        Assert.Equal(Now, file.ModifiedAt);
        using var json = JsonDocument.Parse(file.Content);
        var property = Assert.Single(json.RootElement.EnumerateObject());
        Assert.Equal("Data", property.Name);
        Assert.Equal("Gmail", property.Value.GetProperty("Name").GetString());
        Assert.Equal("Web", property.Value.GetProperty("SecretType").GetString());
        Assert.DoesNotContain("Gmail", file.Key, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GivenASecret_WhenSaved_ThenItsLastUpdatedTimeIsNow()
    {
        // Arrange
        var secret = new Secret { Name = "Note", LastUpdatedAt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

        // Act
        var saved = await CreateSut().SaveAsync(secret);

        // Assert
        Assert.Equal(Now.UtcDateTime, saved.LastUpdatedAt);
    }

    [Fact]
    public async Task GivenAFileInClypsesFormat_WhenRead_ThenItIsTheSecretItHolds()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("D");
        var content = $$"""
            {
              "Data": {
                "Id": "{{id}}",
                "CreatedAt": "01-02-2026T03:04:05",
                "LastUpdatedAt": "06-07-2026T08:09:10",
                "SecretType": "Aws",
                "Name": "Deploy",
                "AccessKeyId": "AKIAEXAMPLE",
                "SecretAccessKey": "wJalrXUtnFEMI",
                "Tags": "aws,work"
              }
            }
            """;
        await files.WriteAsync($"{vault.Root}secrets/{id}.json", Encoding.UTF8.GetBytes(content));

        // Act
        var secret = await CreateSut().GetAsync(id);

        // Assert
        var aws = Assert.IsType<AwsCredentials>(secret);
        Assert.Equal("Deploy", aws.Name);
        Assert.Equal("AKIAEXAMPLE", aws.AccessKeyId);
        Assert.Equal("wJalrXUtnFEMI", aws.SecretAccessKey);
        Assert.Equal(new DateTime(2026, 2, 1, 3, 4, 5), aws.CreatedAt);
        Assert.Equal(new DateTime(2026, 7, 6, 8, 9, 10), aws.LastUpdatedAt);
    }

    [Fact]
    public async Task GivenNoSuchSecret_WhenRead_ThenNull()
    {
        // Act & Assert
        Assert.Null(await CreateSut().GetAsync(Guid.NewGuid().ToString("D")));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"Data":{"Name":"No type","SecretType":"Martian"}}""")]
    [InlineData("null")]
    public async Task GivenAFileThatIsNotASecret_WhenRead_ThenThrowsSecretUnreadable(string content)
    {
        // Arrange
        var id = Guid.NewGuid().ToString("D");
        await files.WriteAsync($"{vault.Root}secrets/{id}.json", Encoding.UTF8.GetBytes(content));

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().GetAsync(id));

        // Assert
        Assert.Equal(ClypseErrorCodes.SecretUnreadable, ex.Code);
    }

    [Fact]
    public async Task GivenAFileHoldingAnotherSecretsId_WhenRead_ThenThrowsSecretUnreadable()
    {
        // Arrange
        var other = new Secret { Name = "Other" };
        var id = Guid.NewGuid().ToString("D");
        await files.WriteAsync($"{vault.Root}secrets/{id}.json", JsonSerializer.SerializeToUtf8Bytes(other));

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().GetAsync(id));

        // Assert
        Assert.Equal(ClypseErrorCodes.SecretUnreadable, ex.Code);
    }

    [Theory]
    [InlineData("gmail")]
    [InlineData("../../other")]
    [InlineData("")]
    public async Task GivenAnIdThatIsNotAGuid_WhenUsed_ThenThrowsSecretIdInvalid(string id)
    {
        // Arrange
        var sut = CreateSut();
        var secret = new Secret { Name = "Bad", Id = id };

        // Act & Assert
        Assert.Equal(ClypseErrorCodes.SecretIdInvalid, (await Assert.ThrowsAsync<ClypseException>(() => sut.GetAsync(id))).Code);
        Assert.Equal(ClypseErrorCodes.SecretIdInvalid, (await Assert.ThrowsAsync<ClypseException>(() => sut.SaveAsync(secret))).Code);
        Assert.Equal(ClypseErrorCodes.SecretIdInvalid, (await Assert.ThrowsAsync<ClypseException>(() => sut.DeleteAsync(id))).Code);
        Assert.Empty(await files.ListAsync());
    }

    [Fact]
    public async Task GivenASecretWithoutAName_WhenSaved_ThenThrowsSecretInvalid_AndNothingIsWritten()
    {
        // Arrange
        var secret = new WebSecret { Password = "hunter2" };

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => CreateSut().SaveAsync(secret));

        // Assert
        Assert.Equal(ClypseErrorCodes.SecretInvalid, ex.Code);
        Assert.Equal("Name", Assert.Single(ex.Arguments));
        Assert.Empty(await files.ListAsync());
    }

    [Fact]
    public async Task GivenOtherFiles_WhenListingIds_ThenOnlyThisVaultsSecrets()
    {
        // Arrange
        var sut = CreateSut();
        var first = await sut.SaveAsync(new Secret { Name = "First" });
        var second = await sut.SaveAsync(new Secret { Name = "Second" });
        await files.WriteAsync($"{vault.Root}.vault", [1]);
        await files.WriteAsync($"{vault.Root}secrets/notes.txt", [1]);
        await files.WriteAsync($"{vault.Root}secrets/gmail.json", [1]);
        await files.WriteAsync($"profiles/{Guid.NewGuid():D}/secrets/{Guid.NewGuid():D}.json", [1]);
        await files.CreateFolderAsync($"{vault.Root}secrets/");

        // Act
        var ids = await sut.ListIdsAsync();

        // Assert
        Assert.Equal(new[] { first.Id, second.Id }.Order(), ids.Order());
    }

    [Fact]
    public async Task GivenASecret_WhenDeleted_ThenItIsGone()
    {
        // Arrange
        var sut = CreateSut();
        var secret = await sut.SaveAsync(new Secret { Name = "Gone" });

        // Act
        await sut.DeleteAsync(secret.Id);

        // Assert
        Assert.Null(await sut.GetAsync(secret.Id));
        Assert.Empty(await sut.ListIdsAsync());
    }

    [Fact]
    public async Task GivenValidSecrets_WhenImported_ThenAllAreWrittenWithProgress()
    {
        // Arrange
        var sut = CreateSut();
        var secrets = new List<Secret> { new WebSecret { Name = "One" }, new AwsCredentials { Name = "Two" }, new Secret { Name = "Three" } };
        var reported = new List<int>();

        // Act
        var imported = await sut.ImportAsync(secrets, new SynchronousProgress(reported.Add));

        // Assert
        Assert.Equal(3, imported);
        Assert.Equal([1, 2, 3], reported);
        Assert.Equal(3, (await sut.ListIdsAsync()).Count);
        Assert.IsType<AwsCredentials>(await sut.GetAsync(secrets[1].Id));
    }

    [Fact]
    public async Task GivenNoProgress_WhenImported_ThenTheCountIsStillReturned()
    {
        // Act
        var imported = await CreateSut().ImportAsync([new Secret { Name = "One" }, new Secret { Name = "Two" }]);

        // Assert
        Assert.Equal(2, imported);
    }

    [Fact]
    public async Task GivenOneInvalidSecret_WhenImported_ThenNoneIsWritten()
    {
        // Arrange
        var sut = CreateSut();
        var secrets = new List<Secret> { new Secret { Name = "One" }, new Secret(), new Secret { Name = "Three" } };

        // Act
        var ex = await Assert.ThrowsAsync<ClypseException>(() => sut.ImportAsync(secrets));

        // Assert
        Assert.Equal(ClypseErrorCodes.SecretInvalid, ex.Code);
        Assert.Empty(await files.ListAsync());
    }

    [Fact]
    public async Task GivenCancelled_WhenImported_ThenStopsBeforeWriting()
    {
        // Arrange
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateSut().ImportAsync([new Secret { Name = "One" }], ct: cancelled.Token));
        Assert.Empty(await files.ListAsync());
    }

    [Fact]
    public void GivenKeys_WhenGettingSecretIds_ThenOnlyThisVaultsSecretFilesHaveOne()
    {
        // Arrange
        var sut = CreateSut();
        var id = Guid.NewGuid().ToString("D");

        // Act & Assert
        Assert.Equal(id, sut.GetSecretId($"{vault.Root}secrets/{id}.json"));
        Assert.Null(sut.GetSecretId($"{vault.Root}.vault"));
        Assert.Null(sut.GetSecretId($"{vault.Root}secrets/{id}.txt"));
        Assert.Null(sut.GetSecretId($"{vault.Root}secrets/gmail.json"));
        Assert.Null(sut.GetSecretId($"{vault.Root}secrets/sub/{id}.json"));
        Assert.Null(sut.GetSecretId($"profiles/{Guid.NewGuid():D}/secrets/{id}.json"));
        Assert.Null(sut.GetSecretId(null!));
    }

    private SecretService CreateSut() => new(vault, files, clock);

    private sealed class SynchronousProgress(Action<int> report) : IProgress<int>
    {
        public void Report(int value) => report(value);
    }
}
