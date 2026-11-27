using Clypse.Core.Secrets;
using Clypse.Portal.Models.Vaults;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

// The list of secrets: its order, the search, the tag filters, and what it says while the secrets are read.
public partial class VaultViewModelTests
{
    [Fact]
    public async Task GivenSecrets_WhenListed_ThenByNameWithEveryTag()
    {
        // Arrange
        secrets = [Web("Zoo", "bob", "fun"), Web("bank", "alice", "money, Fun"), new AwsCredentials { Name = "Deploy", AccessKeyId = "AKIA" }];
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act
        var names = sut.Secrets.Select(s => s.Name).ToList();

        // Assert
        Assert.Equal(["bank", "Deploy", "Zoo"], names);
        Assert.Equal(["fun", "money"], sut.Tags);
        Assert.Equal(3, sut.SecretCount);
        Assert.Equal("Showing 3/3 secrets", sut.CountText);
    }

    [Fact]
    public async Task GivenATag_WhenSelected_ThenTheCountSaysHowManyAreShownOfAll()
    {
        // Arrange
        secrets = [Web("Zoo", "bob", "fun"), Web("bank", "alice", "money"), Web("shop", "carol", "fun")];
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act
        sut.SelectTagCommand.Execute("fun");

        // Assert
        Assert.Equal("Showing 2/3 secrets", sut.CountText);
    }

    [Fact]
    public async Task GivenOneSecret_WhenShown_ThenTheCountIsSingular()
    {
        // Arrange
        secrets = [Web("Zoo", "bob", "fun")];
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act
        var count = sut.CountText;

        // Assert
        Assert.Equal("Showing 1/1 secret", count);
    }

    [Fact]
    public async Task GivenUntaggedSecrets_WhenUntaggedIsSelectedAndSelectedAgain_ThenOnlyThoseAndThenAll()
    {
        // Arrange
        secrets = [Web("Zoo", "bob", "fun"), new AwsCredentials { Name = "Deploy", AccessKeyId = "AKIA" }];
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        Assert.True(sut.HasUntagged);

        // Act & Assert
        sut.SelectUntaggedCommand.Execute(null);
        Assert.True(sut.IsUntaggedSelected);
        Assert.Equal("Deploy", Assert.Single(sut.Secrets).Name);
        sut.SelectUntaggedCommand.Execute(null);
        Assert.False(sut.IsUntaggedSelected);
        Assert.Equal(2, sut.Secrets.Count);
    }

    [Fact]
    public async Task GivenUntaggedSelected_WhenATagOrAllIsSelected_ThenUntaggedIsCleared()
    {
        // Arrange
        secrets = [Web("Zoo", "bob", "fun"), new AwsCredentials { Name = "Deploy", AccessKeyId = "AKIA" }];
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        sut.SelectUntaggedCommand.Execute(null);

        // Act & Assert
        sut.SelectTagCommand.Execute("fun");
        Assert.False(sut.IsUntaggedSelected);
        Assert.Equal("Zoo", Assert.Single(sut.Secrets).Name);
        sut.SelectUntaggedCommand.Execute(null);
        Assert.Null(sut.SelectedTag);
        sut.SelectTagCommand.Execute(null);
        Assert.False(sut.IsUntaggedSelected);
        Assert.Equal(2, sut.Secrets.Count);
    }

    [Fact]
    public async Task GivenUntaggedSelected_WhenSearching_ThenOnlyUntaggedMatchesAreShown()
    {
        // Arrange
        secrets = [Web("Deploy notes", "bob", "fun"), new AwsCredentials { Name = "Deploy", AccessKeyId = "AKIA" }, new AwsCredentials { Name = "Other", AccessKeyId = "AKIB" }];
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        sut.SelectUntaggedCommand.Execute(null);

        // Act
        sut.Search = "deploy";

        // Assert
        Assert.Equal("Deploy", Assert.Single(sut.Secrets).Name);
    }

    [Fact]
    public async Task GivenEverySecretTagged_WhenShown_ThenUntaggedIsNotOffered()
    {
        // Arrange
        secrets = [Web("Zoo", "bob", "fun"), Web("bank", "alice", "money")];
        var sut = CreateSut();

        // Act
        await sut.LoadAsync("v1");

        // Assert
        Assert.False(sut.HasUntagged);
    }

    [Fact]
    public async Task GivenUntaggedSelected_WhenTheLastUntaggedSecretGoes_ThenTheFilterIsCleared()
    {
        // Arrange
        secrets = [Web("Zoo", "bob", "fun"), new AwsCredentials { Name = "Deploy", AccessKeyId = "AKIA" }];
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        sut.SelectUntaggedCommand.Execute(null);

        // Act
        secrets = [secrets[0]];
        mockSession.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.False(sut.IsUntaggedSelected);
        Assert.Single(sut.Secrets);
    }

    [Theory]
    [InlineData("ZOO", "Zoo")]
    [InlineData("alice", "bank")]
    [InlineData("example.com", "Zoo")]
    [InlineData("money", "bank")]
    [InlineData("akia", "Deploy")]
    public async Task GivenASearch_WhenListed_ThenOnlyMatchingSecrets(string search, string expected)
    {
        // Arrange
        var aws = new AwsCredentials { Name = "Deploy", AccessKeyId = "AKIA", Description = "akia keys" };
        secrets = [Web("Zoo", "bob", "fun", "https://example.com"), Web("bank", "alice", "money"), aws];
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act
        sut.Search = search;

        // Assert
        Assert.Equal(expected, Assert.Single(sut.Secrets).Name);
    }

    [Fact]
    public async Task GivenATag_WhenSelectedAndSelectedAgain_ThenFilteredAndThenAll()
    {
        // Arrange
        secrets = [Web("Zoo", "bob", "fun"), Web("bank", "alice", "money")];
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act & Assert
        sut.SelectTagCommand.Execute("MONEY");
        Assert.Equal("bank", Assert.Single(sut.Secrets).Name);
        sut.SelectTagCommand.Execute("money");
        Assert.Null(sut.SelectedTag);
        Assert.Equal(2, sut.Secrets.Count);
        sut.SelectTagCommand.Execute("fun");
        sut.SelectTagCommand.Execute(null);
        Assert.Null(sut.SelectedTag);
    }

    [Fact]
    public async Task GivenATagFilter_WhenItsLastSecretGoes_ThenTheFilterIsCleared()
    {
        // Arrange
        secrets = [Web("Zoo", "bob", "fun"), Web("bank", "alice", "money")];
        var sut = CreateSut();
        await sut.LoadAsync("v1");
        sut.SelectTagCommand.Execute("money");

        // Act
        secrets = [secrets[0]];
        mockSession.Raise(x => x.Changed += null, EventArgs.Empty);

        // Assert
        Assert.Null(sut.SelectedTag);
        Assert.Single(sut.Secrets);
    }

    [Fact]
    public async Task GivenLoading_WhenShown_ThenProgressAndUnreadableAreWorded()
    {
        // Arrange
        mockSession.SetupGet(x => x.IsLoading).Returns(true);
        mockSession.SetupGet(x => x.TotalCount).Returns(40);
        mockSession.SetupGet(x => x.LoadedCount).Returns(10);
        mockSession.SetupGet(x => x.UnreadableCount).Returns(1);
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act & Assert
        Assert.True(sut.IsLoading);
        Assert.Equal(25, sut.LoadingPercent);
        Assert.Equal("Decrypting 10 of 40 secrets...", sut.LoadingText);
        Assert.Equal("1 secret in this vault could not be read.", sut.UnreadableText);
    }

    [Fact]
    public async Task GivenNothingToRead_WhenShown_ThenNoProgressOrWarning()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync("v1");

        // Act & Assert
        Assert.Equal(0, sut.LoadingPercent);
        Assert.Null(sut.UnreadableText);
    }
}
