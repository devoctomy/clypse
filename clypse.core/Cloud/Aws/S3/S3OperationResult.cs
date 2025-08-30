namespace clypse.core.Cloud.Aws.S3;

/// <summary>
/// Result structure for JavaScript S3 operations.
/// Contains the success status, error message, and optional data payload from S3 operations.
/// </summary>
public class S3OperationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the S3 operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional data payload returned from the S3 operation.
    /// </summary>
    public Dictionary<string, object?>? Data { get; set; }
}
