using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Bing.Internal;

/// <summary>
/// Represents one element of the Bing Translator answer. The first element
/// carries the translations; a second one, present when the source text is
/// written in a script Bing can romanize, carries its transliteration.
/// </summary>
internal sealed class BingTranslationResponse
{
    /// <summary>
    /// Gets or sets the translations of the requested text.
    /// </summary>
    [JsonPropertyName("translations")]
    public IReadOnlyList<BingTranslation>? Translations { get; set; }

    /// <summary>
    /// Gets or sets the detected source language, present even when the
    /// request named one.
    /// </summary>
    [JsonPropertyName("detectedLanguage")]
    public BingDetectedLanguage? DetectedLanguage { get; set; }

    /// <summary>
    /// Gets or sets the source text rendered in another script.
    /// </summary>
    [JsonPropertyName("inputTransliteration")]
    public string? InputTransliteration { get; set; }

    /// <summary>
    /// Gets or sets the ISO 15924 code of the script
    /// <see cref="InputTransliteration"/> is written in.
    /// </summary>
    [JsonPropertyName("script")]
    public string? Script { get; set; }

    /// <summary>
    /// Gets or sets the code Bing reports when it refuses the request. The
    /// endpoint answers 200 whether it worked or not, so this is how a
    /// refusal is recognized.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the explanation of a refusal, when there is one.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
