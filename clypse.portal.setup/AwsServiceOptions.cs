namespace clypse.portal.setup;

/// <summary>
/// Configuration options for AWS services used in the portal setup.
/// </summary>
public class AwsServiceOptions
{
    /// <summary>
    /// Gets or sets the base URL for AWS services.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS access key for authentication.
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS secret key for authentication.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS region where resources will be created.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prefix to be added to all AWS resource names.
    /// </summary>
    public string ResourcePrefix { get; set; } = "test";
}
