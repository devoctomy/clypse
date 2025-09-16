using clypse.core.Secrets;
using clypse.core.Secrets.Import;
using clypse.core.Vault;

namespace clypse.core.IntTests;

public class TestContext
{
    public string? AwsAccessKey { get; set; }

    public string? SecretAccessKey { get; set; }

    public string? BucketName { get; set; }

    public IVault? Vault { get; set; }

    public string? Base64Key { get; set; }

    public VaultSaveResults? SaveResults { get; set; }

    public VaultVerifyResults? VerifyResults { get; set; }

    public Dictionary<string, WebSecret> AddedSecrets { get; set; } = [];

    public string? IdentityId { get; set; }

    public CsvSecretsImporterService? Importer { get; set; }

    public int ImportedSecretCount { get; set; }

    public List<Dictionary<string, string>>? MappedImportedSecrets { get; set; }
}
