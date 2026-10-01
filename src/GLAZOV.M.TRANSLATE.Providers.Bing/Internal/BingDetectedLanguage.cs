using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.Bing.Internal;

/// <summary>
/// Represents the source language Bing detected.
/// </summary>
internal sealed class BingDetectedLanguage
{
    /// <summary>
    /// Gets or sets the detected language code.
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }
}
