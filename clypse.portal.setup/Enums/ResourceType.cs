namespace clypse.portal.setup.Enums;

/// <summary>
/// Types of resources provisioned during setup.
/// </summary>
public enum ResourceType
{
    None = 0,
    S3Bucket = 1,
    CloudFrontDistribution = 2,
    CognitoUserPool = 3,
    CognitoUserPoolClient = 4,
    CognitoIdentityPool = 5,
    IamRole = 6,
    IamPolicy = 7,
    PortalDeployment = 8,
}
