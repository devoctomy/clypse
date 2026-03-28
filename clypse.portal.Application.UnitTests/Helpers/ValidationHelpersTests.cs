using clypse.portal.Application.Helpers;

namespace clypse.portal.Application.UnitTests.Helpers;

public class ValidationHelpersTests
{
    [Fact]
    public void GivenNullValue_WhenVerifiedAssignment_ThenThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act
        var exception = Assert.ThrowsAny<ArgumentNullException>(() => VerifiedAssignmentTest(value));

        // Assert
        Assert.Contains("testParameter1", exception.Message);
    }

    [Fact]
    public void GivenNonNull_WhenVerifiedAssignment_ThenExceptionNotThrown()
    {
        // Arrange
        string? value = "Hello World!";

        // Act
        var assigned = VerifiedAssignmentTest(value);

        // Assert
        assigned.Contains(value);
    }

    private List<object?> VerifiedAssignmentTest(object? testParameter1)
    {
        var parameters = new List<object?>();
        testParameter1 = ValidationHelpers.VerifiedAssignent(testParameter1);
        parameters.Add(testParameter1);
        return parameters;
    }
}
