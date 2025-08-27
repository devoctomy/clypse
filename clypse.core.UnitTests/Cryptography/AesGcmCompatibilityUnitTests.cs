using System.Text;
using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;

namespace clypse.core.UnitTests.Cryptography;

/// <summary>
/// Cross-compatibility unit tests for AES-GCM ICryptoService implementations
/// Tests that all specified implementations can encrypt/decrypt each other's data.
/// </summary>
public class AesGcmCompatibilityUnitTests
{
    private readonly string testKey;
    private readonly List<(string Name, ICryptoService Service)> cryptoServices;

    // Define the AES-GCM implementations to test for compatibility
    private readonly string[] aesGcmServiceTypeNames =
    [
        "clypse.core.Cryptogtaphy.NativeAesGcmCryptoService",
        "clypse.core.Cryptogtaphy.BouncyCastleAesGcmCryptoService",
    ];

    public AesGcmCompatibilityUnitTests()
    {
        byte[] keyBytes = CryptoHelpers.GenerateRandomBytes(32);
        this.testKey = Convert.ToBase64String(keyBytes);

        this.cryptoServices = this.LoadCryptoServices();
    }

    [Fact]
    public void GivenServiceTypeNames_WhenLoadingServices_ThenAllExpectedServicesAreLoaded()
    {
        // Assert that we have the expected number of services loaded
        Assert.True(this.cryptoServices.Count >= 2, $"Expected at least 2 crypto services, but loaded {this.cryptoServices.Count}");

        // Verify specific services are loaded
        var serviceNames = this.cryptoServices.Select(s => s.Name).ToList();
        Assert.Contains("NativeAesGcmCryptoService", serviceNames);
        Assert.Contains("BouncyCastleAesGcmCryptoService", serviceNames);
    }

    [Fact]
    public async Task GivenAllAesGcmServices_WhenCrossTestingEncryptionDecryption_ThenAllCombinationsWork()
    {
        // Arrange
        string originalText = "Cross-compatibility test data";
        byte[] originalData = Encoding.UTF8.GetBytes(originalText);
        var testResults = new List<string>();

        // Act & Assert - Test every service encrypting and every other service decrypting
        for (int encryptorIndex = 0; encryptorIndex < this.cryptoServices.Count; encryptorIndex++)
        {
            var (encryptorName, encryptorService) = this.cryptoServices[encryptorIndex];

            // Encrypt with this service
            using var inputStream = new MemoryStream(originalData);
            using var encryptedStream = new MemoryStream();

            await encryptorService.EncryptAsync(inputStream, encryptedStream, this.testKey);
            byte[] encryptedData = encryptedStream.ToArray();

            // Try to decrypt with every service (including itself)
            for (int decryptorIndex = 0; decryptorIndex < this.cryptoServices.Count; decryptorIndex++)
            {
                var (decryptorName, decryptorService) = this.cryptoServices[decryptorIndex];

                using var encryptedDataStream = new MemoryStream(encryptedData);
                using var decryptedStream = new MemoryStream();

                // Act
                await decryptorService.DecryptAsync(encryptedDataStream, decryptedStream, this.testKey);

                // Assert
                string decryptedText = Encoding.UTF8.GetString(decryptedStream.ToArray());
                Assert.Equal(originalText, decryptedText);

                testResults.Add($"✓ {encryptorName} → {decryptorName}");
            }
        }

        // Output test matrix for debugging
        System.Diagnostics.Debug.WriteLine("Compatibility Test Results:");
        testResults.ForEach(result => System.Diagnostics.Debug.WriteLine(result));
    }

    [Fact]
    public async Task GivenAllAesGcmServices_WhenCrossTestingLargeData_ThenAllCombinationsPreserveData()
    {
        // Arrange
        byte[] largeData = CryptoHelpers.GenerateRandomBytes(1024 * 100); // 100KB test data

        // Act & Assert - Test every service encrypting and every other service decrypting
        for (int encryptorIndex = 0; encryptorIndex < this.cryptoServices.Count; encryptorIndex++)
        {
            var (_, encryptorService) = this.cryptoServices[encryptorIndex];

            // Encrypt with this service
            using var inputStream = new MemoryStream(largeData);
            using var encryptedStream = new MemoryStream();

            await encryptorService.EncryptAsync(inputStream, encryptedStream, this.testKey);
            byte[] encryptedData = encryptedStream.ToArray();

            // Try to decrypt with every service
            for (int decryptorIndex = 0; decryptorIndex < this.cryptoServices.Count; decryptorIndex++)
            {
                var (decryptorName, decryptorService) = this.cryptoServices[decryptorIndex];

                using var encryptedDataStream = new MemoryStream(encryptedData);
                using var decryptedStream = new MemoryStream();

                // Act
                await decryptorService.DecryptAsync(encryptedDataStream, decryptedStream, this.testKey);

                // Assert
                byte[] decryptedData = decryptedStream.ToArray();
                Assert.Equal(largeData, decryptedData);
            }
        }
    }

    [Fact]
    public async Task GivenAllAesGcmServices_WhenCrossTestingEmptyData_ThenAllCombinationsWork()
    {
        // Arrange
        byte[] emptyData = [];

        // Act & Assert - Test every service encrypting and every other service decrypting
        for (int encryptorIndex = 0; encryptorIndex < this.cryptoServices.Count; encryptorIndex++)
        {
            var (_, encryptorService) = this.cryptoServices[encryptorIndex];

            // Encrypt with this service
            using var inputStream = new MemoryStream(emptyData);
            using var encryptedStream = new MemoryStream();

            await encryptorService.EncryptAsync(inputStream, encryptedStream, this.testKey);
            byte[] encryptedData = encryptedStream.ToArray();

            // Try to decrypt with every service
            for (int decryptorIndex = 0; decryptorIndex < this.cryptoServices.Count; decryptorIndex++)
            {
                var (decryptorName, decryptorService) = this.cryptoServices[decryptorIndex];

                using var encryptedDataStream = new MemoryStream(encryptedData);
                using var decryptedStream = new MemoryStream();

                // Act
                await decryptorService.DecryptAsync(encryptedDataStream, decryptedStream, this.testKey);

                // Assert
                byte[] decryptedData = decryptedStream.ToArray();
                Assert.Equal(emptyData, decryptedData);
            }
        }
    }

    [Fact]
    public async Task GivenAllAesGcmServices_WhenCrossTestingWithWrongKey_ThenAllThrowCryptographicException()
    {
        // Arrange
        string testText = "Authentication test data";
        byte[] testData = Encoding.UTF8.GetBytes(testText);
        byte[] wrongKeyBytes = CryptoHelpers.GenerateRandomBytes(32);
        string wrongKey = Convert.ToBase64String(wrongKeyBytes);

        // Act & Assert - Test every service encrypting and every other service failing to decrypt with wrong key
        for (int encryptorIndex = 0; encryptorIndex < this.cryptoServices.Count; encryptorIndex++)
        {
            var (encryptorName, encryptorService) = this.cryptoServices[encryptorIndex];

            // Encrypt with correct key
            using var inputStream = new MemoryStream(testData);
            using var encryptedStream = new MemoryStream();

            await encryptorService.EncryptAsync(inputStream, encryptedStream, this.testKey);
            byte[] encryptedData = encryptedStream.ToArray();

            // Try to decrypt with wrong key using every service
            for (int decryptorIndex = 0; decryptorIndex < this.cryptoServices.Count; decryptorIndex++)
            {
                var (decryptorName, decryptorService) = this.cryptoServices[decryptorIndex];
                using var encryptedDataStream = new MemoryStream(encryptedData);
                using var decryptedStream = new MemoryStream();

                // Act & Assert
                await Assert.ThrowsAnyAsync<System.Security.Cryptography.CryptographicException>(
                    async () => await decryptorService.DecryptAsync(encryptedDataStream, decryptedStream, wrongKey));
            }
        }
    }

    private List<(string Name, ICryptoService Service)> LoadCryptoServices()
    {
        var services = new List<(string Name, ICryptoService Service)>();

        foreach (var typeName in this.aesGcmServiceTypeNames)
        {
            try
            {
                var type = typeof(ICryptoService).Assembly.GetType(typeName);
                if (type != null && typeof(ICryptoService).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type) is ICryptoService instance)
                    {
                        var serviceName = type.Name;
                        services.Add((serviceName, instance));
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail - this allows tests to run even if some implementations are missing
                System.Diagnostics.Debug.WriteLine($"Failed to load crypto service {typeName}: {ex.Message}");
            }
        }

        return services;
    }
}
