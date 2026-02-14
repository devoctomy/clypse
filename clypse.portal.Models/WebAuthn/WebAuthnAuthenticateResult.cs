namespace clypse.portal.Models.WebAuthn;

public class WebAuthnAuthenticateResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public bool UserPresent { get; set; }

    public bool UserVerified { get; set; }

    public string? PrfOutput { get; set; }
}
