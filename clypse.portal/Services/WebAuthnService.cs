using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.WebAuthn;
using Microsoft.JSInterop;

namespace clypse.portal.Services;

/// <inheritdoc/>
public class WebAuthnService(IJSRuntime jsRuntime) : IWebAuthnService
{
    private readonly IJSRuntime jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    /// <inheritdoc/>
    public async Task<WebAuthnRegisterResult> RegisterAsync(string username, string? userId)
    {
        return await jsRuntime.InvokeAsync<WebAuthnRegisterResult>("webAuthnWrapper.register", username, userId);
    }

    /// <inheritdoc/>
    public async Task<WebAuthnAuthenticateResult> AuthenticateAsync(string credentialId)
    {
        return await jsRuntime.InvokeAsync<WebAuthnAuthenticateResult>("webAuthnWrapper.authenticate", credentialId);
    }
}
