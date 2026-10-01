using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;

/// <summary>
/// Represents one element of the Microsoft Translator transliteration
/// response, which carries the transliteration of one requested text.
/// </summary>
internal sealed class MicrosoftTransliteration
{
    /// <summary>
    /// Gets or sets the transliterated text.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the ISO 15924 code of the writing system the text was
    /// transliterated into.
    /// </summary>
    [JsonPropertyName("script")]
    public string? Script { get; set; }
}
