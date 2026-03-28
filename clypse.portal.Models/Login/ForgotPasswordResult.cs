using System.Text.Json.Serialization;

namespace clypse.portal.Models.Login;

/// <summary>
/// Represents the result of a forgot password operation.
/// </summary>
public class ForgotPasswordResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the message returned by the operation.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the code delivery details (e.g., how the verification code was sent).
    /// </summary>
    [JsonPropertyName("codeDeliveryDetails")]
    public object? CodeDeliveryDetails { get; set; }
}