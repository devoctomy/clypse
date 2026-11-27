using Clypse.Portal.Application.Services;

namespace Clypse.Portal.Application.UnitTests.Services;

public class UnsavedChangesGuardTests
{
    private readonly UnsavedChangesGuard sut = new();

    [Fact]
    public void GivenNothingBeingEdited_WhenAnActionIsRun_ThenItRunsAtOnce()
    {
        // Arrange
        var ran = 0;

        // Act
        sut.Run(() => ran++);

        // Assert
        Assert.Equal(1, ran);
    }

    [Fact]
    public void GivenNoUnsavedChanges_WhenAnActionIsRun_ThenItRunsAtOnce()
    {
        // Arrange
        var ran = 0;
        sut.SetHolder(_ => false);

        // Act
        sut.Run(() => ran++);

        // Assert
        Assert.Equal(1, ran);
    }

    [Fact]
    public void GivenUnsavedChanges_WhenAnActionIsRun_ThenItIsHeld_UntilTheHolderRunsIt()
    {
        // Arrange
        var ran = 0;
        Action? held = null;
        sut.SetHolder(action =>
        {
            held = action;
            return true;
        });

        // Act
        sut.Run(() => ran++);
        var whileHeld = ran;
        held?.Invoke();

        // Assert
        Assert.Equal(0, whileHeld);
        Assert.Equal(1, ran);
    }

    [Fact]
    public void GivenAHolder_WhenItIsCleared_ThenActionsRunAtOnceAgain()
    {
        // Arrange
        var ran = 0;
        Func<Action, bool> holder = _ => true;
        sut.SetHolder(holder);

        // Act
        sut.ClearHolder(holder);
        sut.Run(() => ran++);

        // Assert
        Assert.Equal(1, ran);
    }

    [Fact]
    public void GivenANewerHolder_WhenAnOlderOneIsCleared_ThenTheNewerOneStillHolds()
    {
        // Arrange
        var ran = 0;
        Func<Action, bool> older = _ => false;
        Func<Action, bool> newer = _ => true;
        sut.SetHolder(older);
        sut.SetHolder(newer);

        // Act
        sut.ClearHolder(older);
        sut.Run(() => ran++);

        // Assert
        Assert.Equal(0, ran);
    }

    [Fact]
    public void GivenAHolder_WhenSetToNull_ThenActionsRunAtOnce()
    {
        // Arrange
        var ran = 0;
        sut.SetHolder(_ => true);

        // Act
        sut.SetHolder(null);
        sut.Run(() => ran++);

        // Assert
        Assert.Equal(1, ran);
    }

    [Fact]
    public void GivenNoAction_WhenRun_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.Run(null!));
    }
}
