using clypse.core.Cryptogtaphy;
using System.Security;

namespace clypse.core.UnitTests.Cryptography
{
    public class CryptoHelpersTests
    {
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
            Assert.Equal("B4VTTKslTp06FO7CQEduDhsNLe4CUjr5ImafG7xeNnU=", base64Key);
        }
    }
}
