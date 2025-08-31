using System.Text.Json.Serialization;

namespace clypse.portal.Models;

public class StoredCredentials
{
    [JsonPropertyName("AccessToken")]
    public string? AccessToken { get; set; }
    
    [JsonPropertyName("IdToken")]
    public string? IdToken { get; set; }
    
    [JsonPropertyName("AwsCredentials")]
    public AwsCredentials? AwsCredentials { get; set; }
    
    [JsonPropertyName("ExpirationTime")]
    public string ExpirationTime { get; set; } = string.Empty;
    
    [JsonPropertyName("StoredAt")]
    public DateTime StoredAt { get; set; }
}
