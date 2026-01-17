namespace clypse.portal.setup.Services.Build;

/// <summary>
/// Creates portal configuration payloads with AWS resource identifiers filled in.
/// </summary>
public interface IPortalConfigService
{
    /// <summary>
    /// Generates a configured portal settings file based on a template.
    /// </summary>
    /// <param name="templatePath">Path to the JSON template file.</param>
    /// <param name="s3DataBucketName">S3 bucket name that stores user data.</param>
    /// <param name="s3Region">AWS region for the S3 bucket.</param>
    /// <param name="cognitoUserPoolId">Cognito user pool identifier.</param>
    /// <param name="cognitoUserPoolClientId">Cognito user pool client identifier.</param>
    /// <param name="cognitoRegion">AWS region for Cognito resources.</param>
    /// <param name="cognitoIdentityPoolId">Cognito identity pool identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A memory stream containing the configured JSON content.</returns>
    public Task<MemoryStream> ConfigureAsync(
        string templatePath,
        string s3DataBucketName,
        string s3Region,
        string cognitoUserPoolId,
        string cognitoUserPoolClientId,
        string cognitoRegion,
        string cognitoIdentityPoolId,
        CancellationToken cancellationToken = default);
}
