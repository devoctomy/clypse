using clypse.portal.Models.Aws;
using System.Text.Json.Serialization;

namespace clypse.portal.Models.Login;

/// <summary>
/// Represents the result of a login operation.
/// </summary>
public class LoginResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the login was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the login failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

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
    /// Gets or sets a value indicating whether a password reset is required.
    /// </summary>
    [JsonPropertyName("passwordResetRequired")]
    public bool PasswordResetRequired { get; set; }
}