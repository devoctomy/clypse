using System.Text.Json.Serialization;

namespace clypse.portal.Models;

public class ForgotPasswordResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("codeDeliveryDetails")]
    public object? CodeDeliveryDetails { get; set; }
}