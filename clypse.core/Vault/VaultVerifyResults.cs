namespace clypse.core.Vault;

public class VaultVerifyResults
{
    public bool Success { get; set; }

    public int MissingSecrets { get; set; }

    public int MismatchedSecrets { get; set; }

    public List<string> UnindexedSecrets { get; set; } = [];
}
