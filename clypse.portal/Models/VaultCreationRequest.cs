namespace clypse.portal.Models;

public class VaultCreationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Passphrase { get; set; } = string.Empty;
}
