namespace clypse.portal.Models.Aws;

/// <summary>
/// Represents the configuration for AWS S3 storage.
/// </summary>
public class AwsS3Config
{
    /// <summary>
    /// Gets or sets the name of the S3 bucket.
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS region where the S3 bucket is located.
    /// </summary>
    public string Region { get; set; } = string.Empty;
}
