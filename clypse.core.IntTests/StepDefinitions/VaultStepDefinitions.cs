using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Interfaces;
using clypse.core.Compression;
using clypse.core.Compression.Interfaces;
using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Secrets;
using clypse.core.Vault;
using System.Collections.Generic;
using System.Security;

namespace clypse.core.IntTests.StepDefinitions
{
    [Binding]
    public sealed class VaultStepDefinitions(TestContext testContext)
    {
        private readonly TestContext _testContext = testContext;
        private IEncryptedCloudStorageProvider? _encryptedCloudStorageProvider;
        private ICryptoService? _cryptoService;
        private ICompressionService? _compressionService;
        private IVaultManager? _vaultManager;

        [Given("aws access key loaded from environment variable")]
        public void AwsAccessKeyLoadedFromEnvironmentVariable()
        {
            _testContext.AwsAccessKey = Environment.GetEnvironmentVariable("CLYPSE_AWS_ACCESSKEY")!;
        }

        [Given("aws secret access key loaded from environment variable")]
        public void AwsSecretAccessKeyLoadedFromEnvironmentVariable()
        {
            _testContext.SecretAccessKey = Environment.GetEnvironmentVariable("CLYPSE_AWS_SECRETACCESSKEY")!;
        }

        [Given("aws bucket name loaded from environment variable")]
        public void AwsBucketNameLoadedFromEnvironmentVariable()
        {
            _testContext.BucketName = Environment.GetEnvironmentVariable("CLYPSE_AWS_BUCKETNAME")!;
        }

        [Given("crypto service is initialised")]
        public void GivenCryptoServiceIsInitialised()
        {
            _cryptoService = new AesGcmCryptoService();
        }


        [Given("aws cloud service provider is initialised")]
        public void AwsCloudServiceProviderIsInitialised()
        {
            _encryptedCloudStorageProvider = new AwsS3E2eCloudStorageProvider(
                _testContext.BucketName!,
                new AmazonS3ClientWrapper(
                    _testContext.AwsAccessKey!,
                    _testContext.SecretAccessKey!,
                    Amazon.RegionEndpoint.EUWest2),
                _cryptoService!);
        }

        [Given("compression service is initialised")]
        public void GivenCompressionServiceIsInitialised()
        {
            _compressionService = new GZipCompressionService();
        }

        [Given("vault manager is initialised")]
        public void GivenVaultManagerIsInitialised()
        {
            _vaultManager = new VaultManager(
                _compressionService!,
                _encryptedCloudStorageProvider!);
        }

        [Given("create a new vault")]
        public void CreateANewVault()
        {
            _testContext.Vault = _vaultManager!.Create(
                "TestVault",
                "This is a test vault, made during clypse integration testing.");
        }

        [Given("key derived from password (.*)")]
        public async Task KeyDerivedFromPassword(string password)
        {
            var secureString = new SecureString();
            foreach(char c in password)
            {
                secureString.AppendChar(c);
            }

            var key = await CryptoHelpers.DeriveKeyFromPassphraseAsync(
                secureString,
                _testContext.Vault!.Info.Base64Salt);
            _testContext.Base64Key = Convert.ToBase64String(key);
        }

        [Given("web secrets are added")]
        public void SecretsAreAdded(DataTable dataTable)
        {
            foreach(var row in dataTable.Rows)
            {
                var name = row["Name"];
                var description = row["Description"];
                var userName = row["UserName"];
                var password = row["Password"];
                var webSecret = new Secrets.WebSecret
                {
                    Name = name,
                    Description = description,
                    UserName = userName,
                    Password = password
                };
                _testContext.Vault!.AddSecret(webSecret);

                _testContext.AddedSecrets.Add(webSecret.Id, webSecret);
            }
        }

        [StepDefinition("vault is saved")]
        public async Task VaultIsSaved()
        {
            _testContext.SaveResults = await _vaultManager!.SaveAsync(
                _testContext.Vault!,
                _testContext.Base64Key!,
                CancellationToken.None);
        }

        [StepDefinition("save results successful")]
        public void SaveResultsSuccessful()
        {
            Assert.True(_testContext.SaveResults.Success);
        }

        [StepDefinition("save results report (.*) secrets created")]
        public void SaveResultsReportSecretsCreated(int created)
        {
            Assert.Equal(created, _testContext.SaveResults.SecretsCreated);
        }

        [StepDefinition("save results report (.*) secrets updated")]
        public void SaveResultsReportSecretsUpdated(int updated)
        {
            Assert.Equal(updated, _testContext.SaveResults.SecretsUpdated);
        }

        [StepDefinition("save results report (.*) secrets deleted")]
        public void SaveResultsReportSecretsDeleted(int deleted)
        {
            Assert.Equal(deleted, _testContext.SaveResults.SecretsDeleted);
        }

        [Then("vault deleted")]
        public async Task VaultDeleted()
        {
            await _vaultManager!.DeleteAsync(
                _testContext.Vault!,
                _testContext.Base64Key!,
                CancellationToken.None);
        }

        [StepDefinition("vault is loaded")]
        public async Task VaultIsLoaded()
        {
            _testContext.Vault = await _vaultManager!.LoadAsync(
                _testContext.Vault!.Info.Id,
                _testContext.Base64Key!,
                CancellationToken.None);
        }

        [StepDefinition("secret (.*) is loaded and matches added")]
        public async Task SecretSecretIsLoadedAndMatchesAdded(string secretName)
        {
            var indexEntry = _testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
            Assert.NotNull(indexEntry);
            var secret = await _vaultManager!.GetSecretAsync(
                _testContext.Vault,
                indexEntry.Id,
                _testContext.Base64Key!,
                CancellationToken.None);
            Assert.NotNull(secret);
            var webSecret = WebSecret.FromSecret(secret);
            var added = _testContext.AddedSecrets.SingleOrDefault(x => x.Key == secret.Id);
            Assert.NotNull(added.Value);
            Assert.Equal(added.Value.UserName, webSecret.UserName);
            Assert.Equal(added.Value.Password, webSecret.Password);
        }

        [StepDefinition("secret (.*) is loaded and matches added but with password (.*)")]
        public async Task SecretSecretIsLoadedAndMatchesAddedButWithPassword(
            string secretName,
            string password)
        {
            var indexEntry = _testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
            Assert.NotNull(indexEntry);
            var secret = await _vaultManager!.GetSecretAsync(
                _testContext.Vault,
                indexEntry.Id,
                _testContext.Base64Key!,
                CancellationToken.None);
            Assert.NotNull(secret);
            var webSecret = WebSecret.FromSecret(secret);
            var added = _testContext.AddedSecrets.SingleOrDefault(x => x.Key == secret.Id);
            Assert.NotNull(added.Value);
            Assert.Equal(added.Value.UserName, webSecret.UserName);
            Assert.Equal(added.Value.Password, password);
        }

        [StepDefinition("secret (.*) does not exist")]
        public void SecretSecretDoesNotExist(string secretName)
        {
            var indexEntry = _testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
            Assert.Null(indexEntry);
        }

        [Then("secret (.*) is marked for deletion")]
        public void SecretSecretIsMarkedForDeletion(string secretName)
        {
            var indexEntry = _testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
            Assert.NotNull(indexEntry);
            var deleted = _testContext.Vault.DeleteSecret(indexEntry.Id);
            Assert.True(deleted);
        }

        [StepDefinition("web secret (.*) password is updated to (.*)")]
        public async Task WebSecretSecretPasswordIsUpdatedToPassword(string secretName, string newPassword)
        {
            var indexEntry = _testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
            Assert.NotNull(indexEntry);
            var existing = await _vaultManager!.GetSecretAsync(
                _testContext.Vault,
                indexEntry.Id,
                _testContext.Base64Key!,
                CancellationToken.None);
            var webSecret = WebSecret.FromSecret(existing);
            webSecret.Password = newPassword;
            _testContext.AddedSecrets.Remove(existing.Id);
            _testContext.AddedSecrets.Add(webSecret.Id, webSecret);
            _testContext.Vault.UpdateSecret(webSecret);
        }
    }
}
