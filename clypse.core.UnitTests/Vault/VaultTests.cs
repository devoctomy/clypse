using clypse.core.Vault;

namespace clypse.core.UnitTests.Vault;

public class VaultTests
{
    [Fact]
    public void GivenVault_WithSecrets_WhenDeleteSecret_AndSecretExists_TheSecretAddedToList_AndTrueReturned()
    {
        // Arrange
        var info = new VaultInfo("Foo", "Bar");
        var index = new VaultIndex
        {
            Entries = new List<VaultIndexEntry>
            {
                new VaultIndexEntry(
                    "1",
                    "Secret1",
                    "Hello World",
                    null)
            }
        };
        var vault = new core.Vault.Vault(
            info,
            index);

        // Act
        var result = vault.DeleteSecret("1");

        // Assert
        Assert.True(result);
        Assert.Single(vault.SecretsToDelete);
        Assert.Equal("1", vault.SecretsToDelete[0]);
    }

    [Fact]
    public void GivenVault_WithSecrets_WhenDeleteSecret_AndSecretNotExists_TheSecretNotAddedToList_AndFalseReturned()
    {
        // Arrange
        var info = new VaultInfo("Foo", "Bar");
        var index = new VaultIndex
        {
            Entries = new List<VaultIndexEntry>
            {
                new VaultIndexEntry(
                    "1",
                    "Secret1",
                    "Hello World",
                    null)
            }
        };
        var vault = new core.Vault.Vault(
            info,
            index);

        // Act
        var result = vault.DeleteSecret("2");

        // Assert
        Assert.False(result);
        Assert.Empty(vault.SecretsToDelete);
    }
}
