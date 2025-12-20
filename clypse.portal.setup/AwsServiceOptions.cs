namespace clypse.portal.setup;

public class AwsServiceOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string ResourcePrefix { get; set; } = "test";
}
