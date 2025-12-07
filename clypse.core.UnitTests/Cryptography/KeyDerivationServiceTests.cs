using clypse.core.Cryptography;
using clypse.core.Enums;

namespace clypse.core.UnitTests.Cryptography;

public class KeyDerivationServiceTests
{
    [Theory]
    [InlineData(KeyDerivationAlgorithm.Rfc2898, "password123", "pHCLgS6uUD+4G7ikp7J1m4DtHEueBLt1y5CH8ZK5NF8=")]
#if DEBUG
    [InlineData(KeyDerivationAlgorithm.Argon2id, "password123", "gvPfoBqAnZTEdUPRnWpt2XDwFN2eH5lxdOt+4bFaIus=")]
#else
    [InlineData(KeyDerivationAlgorithm.Argon2id, "password123", "hfd4rZy/D4VVYEFj8tBkgSvC3VieV9FFzUoVa5f53bU=")]
#endif
    public async Task GivenAlgorithm_AndPassphrase_AndBase64Salt_WhenDeriveKeyFromPassphraseAsync_ThenKeyDerivedCorrectly(
        KeyDerivationAlgorithm algorithm,
        string passphrase,
        string expectedBase64Key)
    {
        // Arrange
        var salt = new byte[16];
        var base64Salt = Convert.ToBase64String(salt);
        var defaultOptions = GetDefaults(algorithm);
        var randomGeneratorService = new RandomGeneratorService();
        using var sut = new KeyDerivationService(randomGeneratorService, defaultOptions);

        // Act
        var key = await sut.DeriveKeyFromPassphraseAsync(
            passphrase,
            base64Salt);
        var base64Key = Convert.ToBase64String(key);

        // Assert
        Assert.Equal(expectedBase64Key, base64Key);
    }

    [Fact]
    public async Task GivenCount_WhenBenchmarkAllAsync_ThenAllAlgorithmsBenchmarked_AndResultsReturned()
    {
        // Arrange
        var randomGeneratorService = new RandomGeneratorService();
        using var sut = new KeyDerivationService(randomGeneratorService, new KeyDerivationServiceOptions());

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
