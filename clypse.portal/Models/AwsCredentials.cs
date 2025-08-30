namespace clypse.portal.Models;

public class AwsCredentials
{
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string SessionToken { get; set; } = string.Empty;
    public string Expiration { get; set; } = string.Empty;
    public string IdentityId { get; set; } = string.Empty;
}
