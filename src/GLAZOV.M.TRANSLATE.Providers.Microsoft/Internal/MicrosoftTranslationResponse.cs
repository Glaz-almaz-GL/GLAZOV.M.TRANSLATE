using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;

/// <summary>
/// Represents one element of the Microsoft Translator response, which
/// carries the translations of one requested text.
/// </summary>
internal sealed class MicrosoftTranslationResponse
{
    /// <summary>
    /// Gets or sets the detected source language, present only when the
    /// request did not name one.
    /// </summary>
    [JsonPropertyName("detectedLanguage")]
    public MicrosoftDetectedLanguage? DetectedLanguage { get; set; }

    /// <summary>
    /// Gets or sets the translations of the requested text.
    /// </summary>
    [JsonPropertyName("translations")]
    public IReadOnlyList<MicrosoftTranslation>? Translations { get; set; }
}
