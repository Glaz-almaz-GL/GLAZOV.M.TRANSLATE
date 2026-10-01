using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Google.Internal;

/// <summary>
/// Represents the JSON response returned by the Google Translate web
/// endpoint for a translation request.
/// </summary>
/// <remarks>
/// This shape is undocumented and specific to the free
/// <c>translate.googleapis.com/translate_a/single</c> endpoint (requested
/// with the <c>dj=1</c> parameter, which asks Google to return named JSON
/// fields instead of a positional array). It may change without notice.
/// </remarks>
internal sealed class GoogleTranslationResponse
{
    /// <summary>
    /// Gets or sets the translated sentence fragments.
    /// </summary>
    [JsonPropertyName("sentences")]
    public IReadOnlyList<GoogleSentence>? Sentences { get; set; }

    /// <summary>
    /// Gets or sets the ISO 639-1 code of the detected or confirmed source
    /// language.
    /// </summary>
    [JsonPropertyName("src")]
    public required string Source { get; set; }
}
