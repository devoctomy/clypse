namespace clypse.portal.Models;

public class StoredCredentials
{
    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public AwsCredentials? AwsCredentials { get; set; }
    public string ExpirationTime { get; set; } = string.Empty;
    public DateTime StoredAt { get; set; }
}
