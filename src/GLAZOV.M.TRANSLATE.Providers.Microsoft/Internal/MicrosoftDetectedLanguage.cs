using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;

/// <summary>
/// Represents the source language Microsoft Translator detected.
/// </summary>
internal sealed class MicrosoftDetectedLanguage
{
    /// <summary>
    /// Gets or sets the detected language code.
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets how confident the endpoint is in the detection,
    /// between zero and one.
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; }
}
