using System.Text.Json.Serialization;

namespace clypse.portal.Models.Aws;

/// <summary>
/// Represents credentials stored in local storage for authentication.
/// </summary>
public class StoredCredentials
{
    /// <summary>
    /// Gets or sets the access token from AWS Cognito.
    /// </summary>
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the ID token from AWS Cognito.
    /// </summary>
    [JsonPropertyName("idToken")]
    public string? IdToken { get; set; }

    /// <summary>
    /// Gets or sets the AWS temporary credentials.
    /// </summary>
    [JsonPropertyName("awsCredentials")]
    public AwsCredentials? AwsCredentials { get; set; }

    /// <summary>
    /// Gets or sets the expiration time of the credentials.
    /// </summary>
    [JsonPropertyName("expirationTime")]
    public string ExpirationTime { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the credentials were stored.
    /// </summary>
    [JsonPropertyName("storedAt")]
    public DateTime StoredAt { get; set; }
}