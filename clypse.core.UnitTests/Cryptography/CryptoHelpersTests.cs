using System.Security;
using clypse.core.Cryptogtaphy;

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
    public async Task GivenPassphrase_WhenDeriveKeyFromPassphraseUsingArgon2Async_ThenKeyCorrectlyDerived()
    {
        // Arrange
        var passphrase = "The quick brown fox jumps over the lazy dog.";
        var securePassphrase = new SecureString();
        foreach (var curChar in passphrase)
        {
            securePassphrase.AppendChar(curChar);
        }

        var salt = new byte[32];

        // Act
        var key = await CryptoHelpers.DeriveKeyFromPassphraseUsingArgon2idAsync(
            securePassphrase,
            Convert.ToBase64String(salt));
        var base64Key = Convert.ToBase64String(key);

        // Assert
        Assert.Equal("saC/6pNtxQdlqG93JCeBLFs2FilLvaqAjug4oACkvmw=", base64Key);
    }

    [Fact]
    public async Task GivenPassphrase_WhenDeriveKeyFromPassphraseUsingRfc2898Async_ThenKeyCorrectlyDerived()
    {
        // Arrange
        var passphrase = "The quick brown fox jumps over the lazy dog.";
        var securePassphrase = new SecureString();
        foreach (var curChar in passphrase)
        {
            securePassphrase.AppendChar(curChar);
        }

        var salt = new byte[32];

        // Act
        var key = await CryptoHelpers.DeriveKeyFromPassphraseUsingRfc2898Async(
            securePassphrase,
            Convert.ToBase64String(salt));
        var base64Key = Convert.ToBase64String(key);

        // Assert
        Assert.Equal("OWzd57bBuePSJ0W+2iYqxvF95f1Lt04KTvVAmsNd0U0=", base64Key);
    }
}
