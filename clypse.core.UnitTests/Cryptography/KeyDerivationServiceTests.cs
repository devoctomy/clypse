using System.Security;
using clypse.core.Cryptogtaphy;
using clypse.core.Enums;

namespace clypse.core.UnitTests.Cryptography
{
    public class KeyDerivationServiceTests
    {
        [Theory]
        [InlineData(KeyDerivationAlgorithm.Rfc2898, "password123", "pHCLgS6uUD+4G7ikp7J1m4DtHEueBLt1y5CH8ZK5NF8=")]
        [InlineData(KeyDerivationAlgorithm.Argon2, "password123", "fmrfIUeZXhOOGqHY2fBxDZA6gSrisuSiaJNojxsf6pY=")]
        public async Task GivenAlgorithm_AndPassphrase_AndBase64Salt_WhenDeriveKeyFromPassphraseAsync_ThenKeyDerivedCorrectly(
            KeyDerivationAlgorithm algorithm,
            string passphrase,
            string expectedBase64Key)
        {
            // Arrange
            var securePassphrase = new SecureString();
            foreach (var curChar in passphrase)
            {
                securePassphrase.AppendChar(curChar);
            }

            var salt = new byte[16];
            var base64Salt = Convert.ToBase64String(salt);

            var sut = new KeyDerivationService();

            // Act
            var key = await sut.DeriveKeyFromPassphraseAsync(
                algorithm,
                securePassphrase,
                base64Salt);
            var base64Key = Convert.ToBase64String(key);

            // Assert
            Assert.Equal(expectedBase64Key, base64Key);
        }
    }
}
