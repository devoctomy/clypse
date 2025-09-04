using System.Security;
using clypse.core.Cryptogtaphy;
using clypse.core.Enums;

namespace clypse.core.UnitTests.Cryptography
{
    public class KeyDerivationServiceTests
    {
        [Theory]
        [InlineData(KeyDerivationAlgorithm.Rfc2898, "password123", "pHCLgS6uUD+4G7ikp7J1m4DtHEueBLt1y5CH8ZK5NF8=")]
        [InlineData(KeyDerivationAlgorithm.Argon2id, "password123", "vWvrnwF/Un5j7Id/gSxs/KKLT4SXgbcCu2+g06T1Rrw=")]
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
                GetDefaults(algorithm),
                securePassphrase,
                base64Salt);
            var base64Key = Convert.ToBase64String(key);

            // Assert
            Assert.Equal(expectedBase64Key, base64Key);
        }

        [Fact]
        public async Task GivenCount_WhenBenchmarkAllAsync_ThenAllAlgorithmsBenchmarked_AndResultsReturned()
        {
            // Arrange
            var sut = new KeyDerivationService();

            // Act
            var results = await sut.BenchmarkAllAsync(3);

            // Assert
            Assert.Equal(2, results.Results.Count);
            Assert.Equal(3, results.Results[0].Timings.Count);
            Assert.Equal(3, results.Results[1].Timings.Count);
        }

        private static KeyDerivationServiceOptions GetDefaults(KeyDerivationAlgorithm algorithm)
        {
            return algorithm switch
            {
                KeyDerivationAlgorithm.Rfc2898 => KeyDerivationServiceDefaultOptions.Blazor_Rfc2898(),
                KeyDerivationAlgorithm.Argon2id => KeyDerivationServiceDefaultOptions.Blazor_Argon2id(),
                _ => throw new NotImplementedException($"KeyDerivationAlgorithm '{algorithm}' not supported by KeyDerivationService."),
            };
        }
    }
}
