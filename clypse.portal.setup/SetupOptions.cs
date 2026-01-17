namespace clypse.portal.setup;

/// <summary>
/// Configuration options for portal setup.
/// </summary>
public class SetupOptions
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

    /// <summary>
    /// Gets or sets an optional CloudFront alias (CNAME).
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional ACM certificate ARN to use with the CloudFront alias.
    /// </summary>
    public string CertificateArn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path where the full WASM Release build of the portal has been published.
    /// </summary>
    public string PortalBuildOutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the initial user's email to be created during setup
    /// </summary>
    public string InitialUserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to prompt the user interactively during setup.
    /// </summary>
    public bool InteractiveMode { get; set; } = true;

    /// <summary>
    /// Determines whether the options contain the required values.
    /// </summary>
    /// <returns><see langword="true"/> when required values are present; otherwise, <see langword="false"/>.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(AccessId)
            && !string.IsNullOrWhiteSpace(SecretAccessKey)
            && !string.IsNullOrWhiteSpace(Region)
            && !string.IsNullOrWhiteSpace(ResourcePrefix)
            && !string.IsNullOrWhiteSpace(InitialUserEmail);
    }
}
