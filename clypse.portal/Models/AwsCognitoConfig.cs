namespace clypse.portal.Models;

public class AwsCognitoConfig
{
    public string UserPoolId { get; set; } = string.Empty;
    public string UserPoolClientId { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string IdentityPoolId { get; set; } = string.Empty;
}
