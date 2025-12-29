namespace clypse.portal.setup.Services.Cloudfront;

public interface ICloudfrontService
{
    public Task<string?> CreateDistributionAsync(
        string websiteHost,
        string? alias = null,
        string? certificateArn = null,
        CancellationToken cancellationToken = default);
}
