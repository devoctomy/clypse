namespace clypse.portal.setup.Services.Build;

public interface IPortalConfigService
{
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
