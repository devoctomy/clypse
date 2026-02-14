namespace clypse.portal.Models.WebAuthn;

public class WebAuthnRegisterResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public string? CredentialID { get; set; }

    public string? UserID { get; set; }

    public string? Username { get; set; }

    public bool PrfEnabled { get; set; }
}
