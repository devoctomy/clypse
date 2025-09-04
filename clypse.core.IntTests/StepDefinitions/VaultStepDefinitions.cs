using System.Security;
using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Interfaces;
using clypse.core.Compression;
using clypse.core.Compression.Interfaces;
using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Secrets;
using clypse.core.Vault;

namespace clypse.core.IntTests.StepDefinitions;

[Binding]
public sealed class VaultStepDefinitions(TestContext testContext)
{
    private readonly TestContext testContext = testContext;
    private ICompressionService? compressionService;
    private IVaultManager? vaultManager;
    private IEncryptedCloudStorageProvider? encryptedCloudStorageProvider;
    private ICryptoService? cryptoService;

    [Given("aws access key loaded from environment variable")]
    public void AwsAccessKeyLoadedFromEnvironmentVariable()
    {
        this.testContext.AwsAccessKey = Environment.GetEnvironmentVariable("CLYPSE_AWS_ACCESSKEY") !;
    }

    [Given("aws secret access key loaded from environment variable")]
    public void AwsSecretAccessKeyLoadedFromEnvironmentVariable()
    {
        this.testContext.SecretAccessKey = Environment.GetEnvironmentVariable("CLYPSE_AWS_SECRETACCESSKEY") !;
    }

    [Given("aws bucket name loaded from environment variable")]
    public void AwsBucketNameLoadedFromEnvironmentVariable()
    {
        this.testContext.BucketName = Environment.GetEnvironmentVariable("CLYPSE_AWS_BUCKETNAME") !;
    }

    [Given("crypto service is initialised")]
    public void GivenCryptoServiceIsInitialised()
    {
        this.cryptoService = new NativeAesGcmCryptoService();
    }

    [Given("aws cloud service provider is initialised")]
    public void AwsCloudServiceProviderIsInitialised()
    {
        this.encryptedCloudStorageProvider = new AwsS3E2eCloudStorageProvider(
            this.testContext.BucketName!,
            new AmazonS3ClientWrapper(
                this.testContext.AwsAccessKey!,
                this.testContext.SecretAccessKey!,
                Amazon.RegionEndpoint.EUWest2),
            this.cryptoService!);
    }

    [Given("compression service is initialised")]
    public void GivenCompressionServiceIsInitialised()
    {
        this.compressionService = new GZipCompressionService();
    }

    [Given("user IdentityId is set")]
    public void GivenUserIdentityIdIsSet()
    {
        this.testContext.IdentityId = Guid.NewGuid().ToString();
    }

    [Given("vault manager is initialised")]
    public void GivenVaultManagerIsInitialised()
    {
        this.vaultManager = new VaultManager(
            this.testContext.IdentityId!,
            this.compressionService!,
            this.encryptedCloudStorageProvider!);
    }

    [Given("create a new vault")]
    public void CreateANewVault()
    {
        this.testContext.Vault = this.vaultManager!.Create(
            "TestVault",
            "This is a test vault, made during clypse integration testing.");
    }

    [Given("key derived from password (.*)")]
    public async Task KeyDerivedFromPassword(string password)
    {
        var secureString = new SecureString();
        foreach (char c in password)
        {
            secureString.AppendChar(c);
        }

        var key = await CryptoHelpers.DeriveKeyFromPassphraseUsingArgon2idAsync(
            secureString,
            this.testContext.Vault!.Info.Base64Salt);
        this.testContext.Base64Key = Convert.ToBase64String(key);
    }

    [Given("web secrets are added")]
    public void SecretsAreAdded(DataTable dataTable)
    {
        foreach (var row in dataTable.Rows)
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
                Password = password,
            };
            this.testContext.Vault!.AddSecret(webSecret);

            this.testContext.AddedSecrets.Add(webSecret.Id, webSecret);
        }
    }

    [StepDefinition("vault is saved")]
    public async Task VaultIsSaved()
    {
        this.testContext.SaveResults = await this.vaultManager!.SaveAsync(
            this.testContext.Vault!,
            this.testContext.Base64Key!,
            null,
            CancellationToken.None);
    }

    [Then("vault is verified")]
    public async Task ThenVaultIsVerified()
    {
        this.testContext.VerifyResults = await this.vaultManager!.VerifyAsync(
            this.testContext.Vault!,
            this.testContext.Base64Key!,
            CancellationToken.None);
    }

    [StepDefinition("save results successful")]
    public void SaveResultsSuccessful()
    {
        Assert.True(this.testContext.SaveResults!.Success);
    }

    [StepDefinition("save results report (.*) secrets created")]
    public void SaveResultsReportSecretsCreated(int created)
    {
        Assert.Equal(created, this.testContext.SaveResults!.SecretsCreated);
    }

    [StepDefinition("save results report (.*) secrets updated")]
    public void SaveResultsReportSecretsUpdated(int updated)
    {
        Assert.Equal(updated, this.testContext.SaveResults!.SecretsUpdated);
    }

    [StepDefinition("save results report (.*) secrets deleted")]
    public void SaveResultsReportSecretsDeleted(int deleted)
    {
        Assert.Equal(deleted, this.testContext.SaveResults!.SecretsDeleted);
    }

    [StepDefinition("verify results successful")]
    public void VerifyResultsSuccessful()
    {
        Assert.True(this.testContext.VerifyResults!.Success);
    }

    [StepDefinition("verify results valid")]
    public void VerifyResultsValid()
    {
        Assert.Equal(0, this.testContext.VerifyResults!.MissingSecrets);
        Assert.Equal(0, this.testContext.VerifyResults!.MismatchedSecrets);
        Assert.Empty(this.testContext.VerifyResults!.UnindexedSecrets);
    }

    [Then("vault listed")]
    public async Task ThenVaultListed()
    {
        var allVaultIds = await this.vaultManager!.ListVaultIdsAsync(CancellationToken.None);
        Assert.Single(allVaultIds);
        Assert.Equal(this.testContext!.Vault!.Info.Id, allVaultIds[0]);
    }

    [Then("vault deleted")]
    public async Task VaultDeleted()
    {
        await this.vaultManager!.DeleteAsync(
            this.testContext.Vault!,
            this.testContext.Base64Key!,
            CancellationToken.None);
    }

    [StepDefinition("vault is loaded")]
    public async Task VaultIsLoaded()
    {
        this.testContext.Vault = await this.vaultManager!.LoadAsync(
            this.testContext.Vault!.Info.Id,
            this.testContext.Base64Key!,
            CancellationToken.None);
    }

    [StepDefinition("secret (.*) is loaded and matches added")]
    public async Task SecretSecretIsLoadedAndMatchesAdded(string secretName)
    {
        var indexEntry = this.testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
        Assert.NotNull(indexEntry);
        var secret = await this.vaultManager!.GetSecretAsync(
            this.testContext.Vault,
            indexEntry.Id,
            this.testContext.Base64Key!,
            CancellationToken.None);
        Assert.NotNull(secret);
        var webSecret = WebSecret.FromSecret(secret);
        var added = this.testContext.AddedSecrets.SingleOrDefault(x => x.Key == secret.Id);
        Assert.NotNull(added.Value);
        Assert.Equal(added.Value.UserName, webSecret.UserName);
        Assert.Equal(added.Value.Password, webSecret.Password);
    }

    [StepDefinition("secret (.*) is loaded and matches added but with password (.*)")]
    public async Task SecretSecretIsLoadedAndMatchesAddedButWithPassword(
        string secretName,
        string password)
    {
        var indexEntry = this.testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
        Assert.NotNull(indexEntry);
        var secret = await this.vaultManager!.GetSecretAsync(
            this.testContext.Vault,
            indexEntry.Id,
            this.testContext.Base64Key!,
            CancellationToken.None);
        Assert.NotNull(secret);
        var webSecret = WebSecret.FromSecret(secret);
        var added = this.testContext.AddedSecrets.SingleOrDefault(x => x.Key == secret.Id);
        Assert.NotNull(added.Value);
        Assert.Equal(added.Value.UserName, webSecret.UserName);
        Assert.Equal(added.Value.Password, password);
    }

    [StepDefinition("secret (.*) does not exist")]
    public void SecretSecretDoesNotExist(string secretName)
    {
        var indexEntry = this.testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
        Assert.Null(indexEntry);
    }

    [Then("secret (.*) is marked for deletion")]
    public void SecretSecretIsMarkedForDeletion(string secretName)
    {
        var indexEntry = this.testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
        Assert.NotNull(indexEntry);
        var deleted = this.testContext.Vault.DeleteSecret(indexEntry.Id);
        Assert.True(deleted);
    }

    [StepDefinition("web secret (.*) password is updated to (.*)")]
    public async Task WebSecretSecretPasswordIsUpdatedToPassword(string secretName, string newPassword)
    {
        var indexEntry = this.testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
        Assert.NotNull(indexEntry);
        var existing = await this.vaultManager!.GetSecretAsync(
            this.testContext.Vault,
            indexEntry.Id,
            this.testContext.Base64Key!,
            CancellationToken.None);
        Assert.NotNull(existing);
        var webSecret = WebSecret.FromSecret(existing);
        webSecret.Password = newPassword;
        this.testContext.AddedSecrets.Remove(existing.Id);
        this.testContext.AddedSecrets.Add(webSecret.Id, webSecret);
        this.testContext.Vault.UpdateSecret(webSecret);
    }
}
