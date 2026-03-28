using System.Text.Json.Serialization;

namespace clypse.portal.Models.Aws;

/// <summary>
/// Represents AWS temporary security credentials.
/// </summary>
public class AwsCredentials
{
    /// <summary>
    /// Gets or sets the AWS access key identifier.
    /// </summary>
    [JsonPropertyName("accessKeyId")]
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS secret access key.
    /// </summary>
    [JsonPropertyName("secretAccessKey")]
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS session token.
    /// </summary>
    [JsonPropertyName("sessionToken")]
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration time of the credentials.
    /// </summary>
    [JsonPropertyName("expiration")]
    public string Expiration { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS Cognito identity identifier.
    /// </summary>
    [JsonPropertyName("identityId")]
    public string IdentityId { get; set; } = string.Empty;
}