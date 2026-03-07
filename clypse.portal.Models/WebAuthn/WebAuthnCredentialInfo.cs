namespace clypse.portal.Models.WebAuthn;

/// <summary>
/// Holds runtime information about a registered WebAuthn credential during a test session.
/// </summary>
public class WebAuthnCredentialInfo
{
    /// <summary>Gets or sets the Base64-encoded credential ID.</summary>
    public string CredentialID { get; set; } = string.Empty;

    /// <summary>Gets or sets the user identifier.</summary>
    public string UserID { get; set; } = string.Empty;

    /// <summary>Gets or sets the username associated with the credential.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the PRF extension is enabled.</summary>
    public bool PrfEnabled { get; set; }
}
