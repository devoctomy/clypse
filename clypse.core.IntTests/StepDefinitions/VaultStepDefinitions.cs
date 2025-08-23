using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Interfaces;
using clypse.core.Compression;
using clypse.core.Compression.Interfaces;
using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Vault;

namespace clypse.core.IntTests.StepDefinitions
{
    [Binding]
    public sealed class VaultStepDefinitions
    {
        private readonly TestContext _testContext;
        private IEncryptedCloudStorageProvider _encryptedCloudStorageProvider;
        private ICryptoService _cryptoService;
        private ICompressionService _compressionService;
        private IVaultManager _vaultManager;

        public VaultStepDefinitions(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given("aws access key loaded from environment variable")]
        public void AwsAccessKeyLoadedFromEnvironmentVariable()
        {
            _testContext.AwsAccessKey = Environment.GetEnvironmentVariable("CLYPSE_AWS_ACCESSKEY", EnvironmentVariableTarget.User)!;
        }

        [Given("aws secret access key loaded from environment variable")]
        public void AwsSecretAccessKeyLoadedFromEnvironmentVariable()
        {
            _testContext.SecretAccessKey = Environment.GetEnvironmentVariable("CLYPSE_AWS_SECRETACCESSKEY", EnvironmentVariableTarget.User)!;
        }

        [Given("aws bucket name loaded from environment variable")]
        public void AwsBucketNameLoadedFromEnvironmentVariable()
        {
            _testContext.BucketName = Environment.GetEnvironmentVariable("CLYPSE_AWS_BUCKETNAME", EnvironmentVariableTarget.User)!;
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
                _testContext.BucketName,
                new AmazonS3ClientWrapper(
                    _testContext.AwsAccessKey,
                    _testContext.SecretAccessKey,
                    Amazon.RegionEndpoint.EUWest2),
                _cryptoService);
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
                _compressionService,
                _encryptedCloudStorageProvider);
        }

        [Given("create a new vault")]
        public void CreateANewVault()
        {
            _testContext.Vault = _vaultManager.Create(
                "TestVault",
                "This is a test vault, made during clypse integration testing.");
        }
    }
}
