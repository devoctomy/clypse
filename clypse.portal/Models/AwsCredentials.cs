using System.Text.Json.Serialization;

namespace clypse.portal.Models;

public class AwsCredentials
{
    [JsonPropertyName("accessKeyId")]
    public string AccessKeyId { get; set; } = string.Empty;

    [JsonPropertyName("secretAccessKey")]
    public string SecretAccessKey { get; set; } = string.Empty;

    [JsonPropertyName("sessionToken")]
    public string SessionToken { get; set; } = string.Empty;

    [JsonPropertyName("expiration")]
    public string Expiration { get; set; } = string.Empty;

    [JsonPropertyName("identityId")]
    public string IdentityId { get; set; } = string.Empty;
}