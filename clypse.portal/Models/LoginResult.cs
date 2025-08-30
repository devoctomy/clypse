namespace clypse.portal.Models;

public class LoginResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public AwsCredentials? AwsCredentials { get; set; }
}
