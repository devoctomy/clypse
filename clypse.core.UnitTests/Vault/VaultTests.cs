using clypse.core.Vault;

namespace clypse.core.UnitTests.Vault;

public class VaultTests
{
    [Fact]
    public void GivenVault_WithNoSecrets_WhenAddSecret_AndSecretAddedToPending_AndVaultIsDirty()
    {
        // Arrange
        var info = new VaultInfo("Foo", "Bar");
        var index = new VaultIndex();
        var vault = new core.Vault.Vault(
            info,
            index);
        vault.MakeClean();

        // Act
        vault.AddSecret(new core.Secrets.Secret());

        // Assert
        Assert.Single(vault.PendingSecrets);
        Assert.True(vault.IsDirty);
    }

    [Fact]
    public void GivenDirtyVault_WhenMakeClean_ThenVaultNotDirty()
    {
        // Arrange
        var info = new VaultInfo("Foo", "Bar");
        var index = new VaultIndex();
        var vault = new core.Vault.Vault(
            info,
            index);


        // Act & Assert
        Assert.True(vault.IsDirty);
        vault.MakeClean();
        Assert.False(vault.IsDirty);
    }

    [Fact]
    public void GivenVault_WithSecrets_WhenAddSecret_AndSecretAlreadyExists_TheSecretNotAdded_AndFalseReturned_AndVaultNotDirty()
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
        vault.MakeClean();

        // Act
        var result = vault.AddSecret(new core.Secrets.Secret
        {
            Id = "1"
        });

        // Assert
        Assert.False(result);
        Assert.Empty(vault.PendingSecrets);
        Assert.False(vault.IsDirty);
    }

    [Fact]
    public void GivenVault_WithSecrets_WhenUpdateSecret_AndSecretAlreadyExists_TheSecretAddedToPending_AndTrueReturned_AndVaultIsDirty()
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
        vault.MakeClean();

        // Act
        var result = vault.UpdateSecret(new core.Secrets.Secret
        {
            Id = "1"
        });

        // Assert
        Assert.True(result);
        Assert.Single(vault.PendingSecrets);
        Assert.True(vault.IsDirty);
    }

    [Fact]
    public void GivenVault_WithSecrets_WhenUpdateSecret_AndSecretNotExists_TheSecretNotAddedToPending_AndFalseReturned_AndVaultBotDirty()
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
        vault.MakeClean();

        // Act
        var result = vault.UpdateSecret(new core.Secrets.Secret
        {
            Id = "2"
        });

        // Assert
        Assert.False(result);
        Assert.Empty(vault.PendingSecrets);
        Assert.False(vault.IsDirty);
    }

    [Fact]
    public void GivenVault_WithSecrets_WhenDeleteSecret_AndSecretExists_TheSecretAddedToList_AndTrueReturned_AndVaultIsDirty()
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
        Assert.True(vault.IsDirty);
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
