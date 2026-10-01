using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Microsoft.Internal;

/// <summary>
/// Represents the answer of the speech token endpoint.
/// </summary>
internal sealed class MicrosoftSpeechToken
{
    /// <summary>
    /// Gets or sets the bearer token the speech endpoint accepts.
    /// </summary>
    [JsonPropertyName("t")]
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the Azure region the token was issued for, which names
    /// the host the speech endpoint lives on.
    /// </summary>
    [JsonPropertyName("r")]
    public string? Region { get; set; }
}
