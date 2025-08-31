using System.Text.Json.Serialization;

namespace clypse.portal.Models;

public class LoginResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }
    
    [JsonPropertyName("idToken")]
    public string? IdToken { get; set; }
    
    [JsonPropertyName("awsCredentials")]
    public AwsCredentials? AwsCredentials { get; set; }
}
