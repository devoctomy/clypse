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
    public string AccessId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS secret key for authentication.
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS region where resources will be created.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prefix to be added to all AWS resource names.
    /// </summary>
    public string ResourcePrefix { get; set; } = string.Empty;

    public bool InteractiveMode { get; set; } = true;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(AccessId)
            && !string.IsNullOrWhiteSpace(SecretAccessKey)
            && !string.IsNullOrWhiteSpace(Region)
            && !string.IsNullOrWhiteSpace(ResourcePrefix);
    }
}
