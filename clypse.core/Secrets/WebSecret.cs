using System.Text.Json.Serialization;

namespace clypse.core.Secrets;

public class WebSecret : Secret
{
    [JsonIgnore]
    public string? UserName
    {
        get { return GetData(nameof(UserName)); }
        set { SetData(nameof(UserName), value); }
    }

    [JsonIgnore]
    public string? EmailAddress
    {
        get { return GetData(nameof(EmailAddress)); }
        set { SetData(nameof(EmailAddress), value); }
    }

    [JsonIgnore]
    public string? WebsiteUrl
    {
        get { return GetData(nameof(WebsiteUrl)); }
        set { SetData(nameof(WebsiteUrl), value); }
    }

    [JsonIgnore]
    public string? LoginUrl
    {
        get { return GetData(nameof(LoginUrl)); }
        set { SetData(nameof(LoginUrl), value); }
    }

    [JsonIgnore]
    public string? Password
    {
        get { return GetData(nameof(Password)); }
        set { SetData(nameof(Password), value); }
    }

    public WebSecret()
    {
        SecretType = Enums.SecretType.Web;
    }
}
