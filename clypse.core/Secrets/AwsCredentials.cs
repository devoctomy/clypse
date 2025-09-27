using System.Text.Json.Serialization;
using clypse.core.Enums;

namespace clypse.core.Secrets;

/// <summary>
/// AWS Credentials Secret.
/// </summary>
public class AwsCredentials : Secret
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwsCredentials"/> class.
    /// </summary>
    public AwsCredentials()
    {
        this.SecretType = Enums.SecretType.Web;
    }

    /// <summary>
    /// Gets or sets AccessKeyId for this secret.
    /// </summary>
    [SecretField(DisplayOrder = 30, FieldType = SecretFieldType.SingleLineText)]
    [JsonIgnore]
    public string? AccessKeyId
    {
        get { return this.GetData(nameof(this.AccessKeyId)); }
        set { this.SetData(nameof(this.AccessKeyId), value); }
    }

    /// <summary>
    /// Gets or sets SecretAccessKey for this secret.
    /// </summary>
    [SecretField(DisplayOrder = 40, FieldType = SecretFieldType.SingleLineText)]
    [JsonIgnore]
    public string? SecretAccessKey
    {
        get { return this.GetData(nameof(this.SecretAccessKey)); }
        set { this.SetData(nameof(this.SecretAccessKey), value); }
    }

    /// <summary>
    /// Create a AwsCredentials from an instance of Secret.
    /// </summary>
    /// <param name="secret">Secret instance to create AwsCredentials from.</param>
    /// <returns>Resulting AwsCredentials.</returns>
    public static AwsCredentials FromSecret(Secret secret)
    {
        var awsCred = new AwsCredentials();
        awsCred.SetAllData(secret.Data);
        return awsCred;
    }
}
