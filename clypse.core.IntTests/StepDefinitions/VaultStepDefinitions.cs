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
        testContext.AwsAccessKey = Environment.GetEnvironmentVariable("CLYPSE_AWS_ACCESSKEY")!;
    }

    [Given("aws secret access key loaded from environment variable")]
    public void AwsSecretAccessKeyLoadedFromEnvironmentVariable()
    {
        testContext.SecretAccessKey = Environment.GetEnvironmentVariable("CLYPSE_AWS_SECRETACCESSKEY")!;
    }

    [Given("aws bucket name loaded from environment variable")]
    public void AwsBucketNameLoadedFromEnvironmentVariable()
    {
        testContext.BucketName = Environment.GetEnvironmentVariable("CLYPSE_AWS_BUCKETNAME")!;
    }

    [Given("crypto service is initialised")]
    public void GivenCryptoServiceIsInitialised()
    {
        cryptoService = new NativeAesGcmCryptoService();
    }


    [Given("aws cloud service provider is initialised")]
    public void AwsCloudServiceProviderIsInitialised()
    {
        encryptedCloudStorageProvider = new AwsS3E2eCloudStorageProvider(
            testContext.BucketName!,
            new AmazonS3ClientWrapper(
                testContext.AwsAccessKey!,
                testContext.SecretAccessKey!,
                Amazon.RegionEndpoint.EUWest2),
            cryptoService!);
    }

    [Given("compression service is initialised")]
    public void GivenCompressionServiceIsInitialised()
    {
        compressionService = new GZipCompressionService();
    }

    [Given("vault manager is initialised")]
    public void GivenVaultManagerIsInitialised()
    {
        vaultManager = new VaultManager(
            compressionService!,
            encryptedCloudStorageProvider!);
    }

    [Given("create a new vault")]
    public void CreateANewVault()
    {
        testContext.Vault = vaultManager!.Create(
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

        var key = await CryptoHelpers.DeriveKeyFromPassphraseAsync(
            secureString,
            testContext.Vault!.Info.Base64Salt);
        testContext.Base64Key = Convert.ToBase64String(key);
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
            testContext.Vault!.AddSecret(webSecret);

            testContext.AddedSecrets.Add(webSecret.Id, webSecret);
        }
    }

    [StepDefinition("vault is saved")]
    public async Task VaultIsSaved()
    {
        testContext.SaveResults = await vaultManager!.SaveAsync(
            testContext.Vault!,
            testContext.Base64Key!,
            CancellationToken.None);
    }

    [Then("vault is verified")]
    public async Task ThenVaultIsVerified()
    {
        testContext.VerifyResults = await vaultManager!.VerifyAsync(
            testContext.Vault!,
            testContext.Base64Key!,
            CancellationToken.None);
    }

    [StepDefinition("save results successful")]
    public void SaveResultsSuccessful()
    {
        Assert.True(testContext.SaveResults!.Success);
    }

    [StepDefinition("save results report (.*) secrets created")]
    public void SaveResultsReportSecretsCreated(int created)
    {
        Assert.Equal(created, testContext.SaveResults!.SecretsCreated);
    }

    [StepDefinition("save results report (.*) secrets updated")]
    public void SaveResultsReportSecretsUpdated(int updated)
    {
        Assert.Equal(updated, testContext.SaveResults!.SecretsUpdated);
    }

    [StepDefinition("save results report (.*) secrets deleted")]
    public void SaveResultsReportSecretsDeleted(int deleted)
    {
        Assert.Equal(deleted, testContext.SaveResults!.SecretsDeleted);
    }

    [StepDefinition("verify results successful")]
    public void VerifyResultsSuccessful()
    {
        Assert.True(testContext.VerifyResults!.Success);
    }

    [StepDefinition("verify results valid")]
    public void VerifyResultsValid()
    {
        Assert.Equal(0, testContext.VerifyResults!.MissingSecrets);
        Assert.Equal(0, testContext.VerifyResults!.MismatchedSecrets);
        Assert.Empty(testContext.VerifyResults!.UnindexedSecrets);
    }

    [Then("vault deleted")]
    public async Task VaultDeleted()
    {
        await vaultManager!.DeleteAsync(
            testContext.Vault!,
            testContext.Base64Key!,
            CancellationToken.None);
    }

    [StepDefinition("vault is loaded")]
    public async Task VaultIsLoaded()
    {
        testContext.Vault = await vaultManager!.LoadAsync(
            testContext.Vault!.Info.Id,
            testContext.Base64Key!,
            CancellationToken.None);
    }

    [StepDefinition("secret (.*) is loaded and matches added")]
    public async Task SecretSecretIsLoadedAndMatchesAdded(string secretName)
    {
        var indexEntry = testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
        Assert.NotNull(indexEntry);
        var secret = await vaultManager!.GetSecretAsync(
            testContext.Vault,
            indexEntry.Id,
            testContext.Base64Key!,
            CancellationToken.None);
        Assert.NotNull(secret);
        var webSecret = WebSecret.FromSecret(secret);
        var added = testContext.AddedSecrets.SingleOrDefault(x => x.Key == secret.Id);
        Assert.NotNull(added.Value);
        Assert.Equal(added.Value.UserName, webSecret.UserName);
        Assert.Equal(added.Value.Password, webSecret.Password);
    }

    [StepDefinition("secret (.*) is loaded and matches added but with password (.*)")]
    public async Task SecretSecretIsLoadedAndMatchesAddedButWithPassword(
        string secretName,
        string password)
    {
        var indexEntry = testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
        Assert.NotNull(indexEntry);
        var secret = await vaultManager!.GetSecretAsync(
            testContext.Vault,
            indexEntry.Id,
            testContext.Base64Key!,
            CancellationToken.None);
        Assert.NotNull(secret);
        var webSecret = WebSecret.FromSecret(secret);
        var added = testContext.AddedSecrets.SingleOrDefault(x => x.Key == secret.Id);
        Assert.NotNull(added.Value);
        Assert.Equal(added.Value.UserName, webSecret.UserName);
        Assert.Equal(added.Value.Password, password);
    }

    [StepDefinition("secret (.*) does not exist")]
    public void SecretSecretDoesNotExist(string secretName)
    {
        var indexEntry = testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
        Assert.Null(indexEntry);
    }

    [Then("secret (.*) is marked for deletion")]
    public void SecretSecretIsMarkedForDeletion(string secretName)
    {
        var indexEntry = testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
        Assert.NotNull(indexEntry);
        var deleted = testContext.Vault.DeleteSecret(indexEntry.Id);
        Assert.True(deleted);
    }

    [StepDefinition("web secret (.*) password is updated to (.*)")]
    public async Task WebSecretSecretPasswordIsUpdatedToPassword(string secretName, string newPassword)
    {
        var indexEntry = testContext.Vault!.Index.Entries.SingleOrDefault(x => x.Name == secretName);
        Assert.NotNull(indexEntry);
        var existing = await vaultManager!.GetSecretAsync(
            testContext.Vault,
            indexEntry.Id,
            testContext.Base64Key!,
            CancellationToken.None);
        Assert.NotNull(existing);
        var webSecret = WebSecret.FromSecret(existing);
        webSecret.Password = newPassword;
        testContext.AddedSecrets.Remove(existing.Id);
        testContext.AddedSecrets.Add(webSecret.Id, webSecret);
        testContext.Vault.UpdateSecret(webSecret);
    }
}
