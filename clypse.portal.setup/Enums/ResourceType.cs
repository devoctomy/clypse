namespace clypse.portal.setup.Enums;

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
}
