using clypse.portal.Models.Aws;
using System.Text.Json.Serialization;

namespace clypse.portal.Models.Aws;

public class StoredCredentials
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("idToken")]
    public string? IdToken { get; set; }

    [JsonPropertyName("awsCredentials")]
    public AwsCredentials? AwsCredentials { get; set; }

    [JsonPropertyName("expirationTime")]
    public string ExpirationTime { get; set; } = string.Empty;

    [JsonPropertyName("storedAt")]
    public DateTime StoredAt { get; set; }
}