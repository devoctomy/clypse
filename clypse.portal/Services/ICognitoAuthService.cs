using Amazon.CognitoIdentity.Model;

namespace clypse.portal.Services;

public interface ICognitoAuthService
{
    Task<LoginResult> LoginAsync(string username, string password);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    string? AccessToken { get; }
    string? IdToken { get; }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public string? RefreshToken { get; set; }
    public Credentials? AwsCredentials { get; set; }
}
