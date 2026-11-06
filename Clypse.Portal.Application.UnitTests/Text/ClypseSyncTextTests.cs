using Clypse.Portal.Application.Text;
using Devoctomy.Blazwiz.Testing;
using Devoctomy.Blazwiz.UI.Sync.Text;

namespace Clypse.Portal.Application.UnitTests.Text;

/// <summary>
/// Clypse's wording of the sync module's text (<see cref="ClypseSyncText"/>) overrides it, so it must keep matching it:
/// after a framework upgrade, these fail if one of its keys was renamed or removed, or its placeholders changed.
/// </summary>
public class ClypseSyncTextTests
{
    private readonly FrameworkText framework = new(typeof(SyncUiText));

    [Fact]
    public void GivenClypsesWording_WhenComparedWithTheFramework_ThenEveryKeyIsFrameworkText()
    {
        // Act
        var unknown = framework.UnknownOverrideKeys(typeof(ClypseSyncText));

        // Assert
        Assert.Empty(unknown);
    }

    [Fact]
    public void GivenClypsesWording_WhenComparedWithTheFramework_ThenTheirPlaceholdersMatch()
    {
        // Act
        var mismatched = framework.MismatchedOverridePlaceholders(typeof(ClypseSyncText));

        // Assert
        Assert.Empty(mismatched);
    }
}
