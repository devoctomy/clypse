using clypse.portal.Application.ViewModels;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class TagListFieldViewModelTests
{
    private static TagListFieldViewModel CreateSut() => new();

    [Fact]
    public void Constructor_SetsEmptyTags()
    {
        var sut = CreateSut();
        Assert.Empty(sut.Tags);
        Assert.Equal(string.Empty, sut.NewTag);
    }

    [Fact]
    public async Task AddTagCommand_WithValidTag_AddsTags()
    {
        var sut = CreateSut();
        sut.NewTag = "apple";
        List<string>? callbackResult = null;
        sut.TagsChangedCallback = tags => { callbackResult = tags; return Task.CompletedTask; };

        await sut.AddTagCommand.ExecuteAsync(null);

        Assert.Single(sut.Tags);
        Assert.Equal("apple", sut.Tags[0]);
        Assert.Equal(string.Empty, sut.NewTag);
        Assert.NotNull(callbackResult);
    }

    [Fact]
    public async Task AddTagCommand_WithEmptyInput_DoesNothing()
    {
        var sut = CreateSut();
        sut.NewTag = "   ";

        await sut.AddTagCommand.ExecuteAsync(null);

        Assert.Empty(sut.Tags);
    }

    [Fact]
    public async Task AddTagCommand_WithDuplicateTag_DoesNotAdd()
    {
        var sut = CreateSut();
        sut.Tags = ["Apple"];
        sut.NewTag = "apple"; // case-insensitive duplicate

        await sut.AddTagCommand.ExecuteAsync(null);

        Assert.Single(sut.Tags);
    }

    [Fact]
    public async Task RemoveTagCommand_RemovesExistingTag()
    {
        var sut = CreateSut();
        sut.Tags = ["apple", "banana"];
        List<string>? callbackResult = null;
        sut.TagsChangedCallback = tags => { callbackResult = tags; return Task.CompletedTask; };

        await sut.RemoveTagCommand.ExecuteAsync("apple");

        Assert.Single(sut.Tags);
        Assert.Equal("banana", sut.Tags[0]);
        Assert.NotNull(callbackResult);
    }

    [Fact]
    public async Task AddTagCommand_WhenReadOnly_DoesNothing()
    {
        var sut = CreateSut();
        sut.IsReadOnly = true;
        sut.NewTag = "apple";

        await sut.AddTagCommand.ExecuteAsync(null);

        Assert.Empty(sut.Tags);
    }

    [Fact]
    public async Task RemoveTagCommand_WhenReadOnly_DoesNothing()
    {
        var sut = CreateSut();
        sut.IsReadOnly = true;
        sut.Tags = ["apple"];

        await sut.RemoveTagCommand.ExecuteAsync("apple");

        Assert.Single(sut.Tags);
    }
}
