namespace clypse.portal.setup.Services.Cloudfront;

/// <summary>
/// Manages CloudFront distributions for the portal.
/// </summary>
public interface ICloudfrontService
{
    /// <summary>
    /// Creates a CloudFront distribution for the specified website host.
    /// </summary>
    /// <param name="websiteHost">Origin host name to serve from.</param>
    /// <param name="alias">Optional alternate domain name (CNAME).</param>
    /// <param name="certificateArn">Optional ACM certificate ARN for the alias.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The distribution domain name when successful; otherwise, <see langword="null"/>.</returns>
    public Task<string?> CreateDistributionAsync(
        string websiteHost,
        string? alias,
        string? certificateArn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a CloudFront distribution for the specified website host.
    /// </summary>
    /// <param name="websiteHost">Origin host name to serve from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The distribution domain name when successful; otherwise, <see langword="null"/>.</returns>
    public Task<string?> CreateDistributionAsync(
        string websiteHost,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a CloudFront distribution for the specified website host.
    /// </summary>
    /// <param name="websiteHost">Origin host name to serve from.</param>
    /// <param name="alias">Optional alternate domain name (CNAME).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The distribution domain name when successful; otherwise, <see langword="null"/>.</returns>
    public Task<string?> CreateDistributionAsync(
        string websiteHost,
        string? alias,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the CloudFront distribution cache for the specified paths.
    /// </summary>
    /// <param name="distributionId">The CloudFront distribution ID.</param>
    /// <param name="paths">Paths to invalidate. Defaults to all paths ("/*").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> when invalidation is created; otherwise, <see langword="false"/>.</returns>
    public Task<bool> InvalidateDistributionAsync(
        string distributionId,
        string[]? paths = null,
        CancellationToken cancellationToken = default);
}
