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
        string? alias = null,
        string? certificateArn = null,
        CancellationToken cancellationToken = default);
}
