using System.Text.Json.Serialization;

namespace clypse.core.Secrets;

/// <summary>
/// Web Secret for website logins.
/// </summary>
public class WebSecret : Secret
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebSecret"/> class.
    /// </summary>
    public WebSecret()
    {
        this.SecretType = Enums.SecretType.Web;
    }

    /// <summary>
    /// Gets or sets UserName for this secret.
    /// </summary>
    [JsonIgnore]
    public string? UserName
    {
        get { return this.GetData(nameof(this.UserName)); }
        set { this.SetData(nameof(this.UserName), value); }
    }

    /// <summary>
    /// Gets or sets EmailAddress for this secret.
    /// </summary>
    [JsonIgnore]
    public string? EmailAddress
    {
        get { return this.GetData(nameof(this.EmailAddress)); }
        set { this.SetData(nameof(this.EmailAddress), value); }
    }

    /// <summary>
    /// Gets or sets WebsiteUrl for this secret.
    /// </summary>
    [JsonIgnore]
    public string? WebsiteUrl
    {
        get { return this.GetData(nameof(this.WebsiteUrl)); }
        set { this.SetData(nameof(this.WebsiteUrl), value); }
    }

    /// <summary>
    /// Gets or sets LoginUrl for this secret.
    /// </summary>
    [JsonIgnore]
    public string? LoginUrl
    {
        get { return this.GetData(nameof(this.LoginUrl)); }
        set { this.SetData(nameof(this.LoginUrl), value); }
    }

    /// <summary>
    /// Gets or sets password for this secret.
    /// </summary>
    [JsonIgnore]
    public string? Password
    {
        get { return this.GetData(nameof(this.Password)); }
        set { this.SetData(nameof(this.Password), value); }
    }

    /// <summary>
    /// Create a WebSecret from an instance of Secret.
    /// </summary>
    /// <param name="secret">Secret instance to create WebSecret from.</param>
    /// <returns>Resulting WebSecret.</returns>
    public static WebSecret FromSecret(Secret secret)
    {
        var webSecret = new WebSecret();
        webSecret.SetAllData(secret.Data);
        return webSecret;
    }
}
