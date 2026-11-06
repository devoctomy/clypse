using System.Reflection;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Text;
using Devoctomy.Blazwiz.Testing;

namespace Clypse.Portal.Application.UnitTests.Text;

/// <summary>
/// Every clypse error has its text in <see cref="ClypseText"/>, found through the framework's error text as the app finds
/// it.
/// </summary>
public class ClypseTextTests
{
    private readonly FrameworkText framework = new(typeof(ClypseText));

    public static TheoryData<string> ErrorCodes()
    {
        var codes = new TheoryData<string>();
        foreach (var field in typeof(ClypseErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral))
        {
            codes.Add((string)field.GetRawConstantValue()!);
        }

        return codes;
    }

    [Theory]
    [MemberData(nameof(ErrorCodes))]
    public void GivenAClypseError_WhenDescribed_ThenItHasItsOwnText(string code)
    {
        // Arrange
        var unknown = framework.Errors.Describe("Clypse.NoSuchError", []);

        // Act
        var text = framework.Errors.Describe(new ClypseException(code, "Name"));

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.NotEqual(unknown, text);
    }

    [Fact]
    public void GivenAMissingField_WhenSecretInvalidIsDescribed_ThenItNamesTheField()
    {
        // Act
        var text = framework.Errors.Describe(new ClypseException(ClypseErrorCodes.SecretInvalid, "Name"));

        // Assert
        Assert.Contains("Name", text, StringComparison.Ordinal);
    }
}
