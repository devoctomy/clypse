using clypse.portal.Application.Services;
using clypse.portal.Models.Navigation;

namespace clypse.portal.Application.UnitTests.Services.Navigation;

public class NavigationStateServiceTests
{
    private readonly NavigationStateService sut;

    public NavigationStateServiceTests()
    {
        this.sut = new NavigationStateService();
    }

    [Fact]
    public void GivenNoItems_WhenCreated_ThenNavigationItemsIsEmpty()
    {
        // Assert
        Assert.NotNull(this.sut.NavigationItems);
        Assert.Empty(this.sut.NavigationItems);
    }

    [Fact]
    public void GivenItems_WhenUpdateNavigationItems_ThenItemsAreUpdated()
    {
        // Arrange
        var items = new List<NavigationItem>
        {
            new() { Text = "Item 1", Action = "action1" },
            new() { Text = "Item 2", Action = "action2" }
        };

        // Act
        this.sut.UpdateNavigationItems(items);

        // Assert
        Assert.Equal(2, this.sut.NavigationItems.Count);
        Assert.Equal("Item 1", this.sut.NavigationItems[0].Text);
        Assert.Equal("Item 2", this.sut.NavigationItems[1].Text);
    }

    [Fact]
    public void GivenSubscriber_WhenUpdateNavigationItems_ThenEventIsRaised()
    {
        // Arrange
        var eventRaised = false;
        this.sut.NavigationItemsChanged += (_, _) => eventRaised = true;
        var items = new List<NavigationItem> { new() { Text = "Item", Action = "action" } };

        // Act
        this.sut.UpdateNavigationItems(items);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void GivenSubscriber_WhenRequestNavigationAction_ThenEventIsRaisedWithAction()
    {
        // Arrange
        string? receivedAction = null;
        this.sut.NavigationActionRequested += (_, action) => receivedAction = action;

        // Act
        this.sut.RequestNavigationAction("test-action");

        // Assert
        Assert.Equal("test-action", receivedAction);
    }

    [Fact]
    public void GivenNoSubscribers_WhenRequestNavigationAction_ThenDoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => this.sut.RequestNavigationAction("test-action"));
        Assert.Null(exception);
    }

    [Fact]
    public void GivenNewItems_WhenUpdateNavigationItems_ThenOldItemsAreReplaced()
    {
        // Arrange
        this.sut.UpdateNavigationItems([new() { Text = "Old Item", Action = "old" }]);
        var newItems = new List<NavigationItem> { new() { Text = "New Item", Action = "new" } };

        // Act
        this.sut.UpdateNavigationItems(newItems);

        // Assert
        Assert.Single(this.sut.NavigationItems);
        Assert.Equal("New Item", this.sut.NavigationItems[0].Text);
    }
}
