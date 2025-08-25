using clypse.core.Cryptogtaphy;
using System.Security;

namespace clypse.core.UnitTests.Cryptography;

public class CryptoHelpersTests
{
    [Theory]
    [InlineData("Hello World!", 16, "dQnlvaDHYtK6x/kNdYtbIg==")]
    [InlineData("Hello World!", 32, "dQnlvaDHYtK6x/kNdYtbImP6Acy8VCq1498WO+CObKk=")]
    public void GivenString_AndLength_WhenSha256HashString_ThenStringHashed(
        string value,
        int length,
        string expectedBase64Hash)
    {
        // Arrange & Act
        var hash = CryptoHelpers.Sha256HashString(value, length);

        // Assert
        var base64Hash = Convert.ToBase64String(hash);
        Assert.Equal(expectedBase64Hash, base64Hash);
    }

    [Fact]
    public async Task GivenPassphrase_WhenDeriveKeyFromPassphrase_ThenKeyCorrectlyDerived()
    {
        // Arrange
        var passphrase = "The quick brown fox jumps over the lazy dog.";
        var securePassphrase = new SecureString();
        foreach(var curChar in passphrase)
        {
            securePassphrase.AppendChar(curChar);
        }
        var salt = new byte[32];

        // Act
        var key = await CryptoHelpers.DeriveKeyFromPassphraseAsync(
            securePassphrase,
            Convert.ToBase64String(salt));
        var base64Key = Convert.ToBase64String(key);

        // Assert
        Assert.Equal("NIc4A4c2wQHU3xVNNc4dDb6umub01XVPiJtEontyBDM=", base64Key);
    }
}
