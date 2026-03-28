namespace clypse.portal.Models.Aws;

/// <summary>
/// Represents the configuration for AWS Cognito authentication.
/// </summary>
public class AwsCognitoConfig
{
    /// <summary>
    /// Gets or sets the AWS Cognito user pool identifier.
    /// </summary>
    public string UserPoolId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS Cognito user pool client identifier.
    /// </summary>
    public string UserPoolClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS region where the Cognito user pool is located.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS Cognito identity pool identifier.
    /// </summary>
    public string IdentityPoolId { get; set; } = string.Empty;
}
