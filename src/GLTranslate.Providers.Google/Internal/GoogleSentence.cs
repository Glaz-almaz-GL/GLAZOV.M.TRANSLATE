using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Google.Internal;

/// <summary>
/// Represents a single entry of the <c>sentences</c> array returned by the
/// Google Translate web endpoint.
/// </summary>
/// <remarks>
/// <para>
/// A translated text is split into one entry per sentence; the full
/// translation is the concatenation of every entry's <see cref="Translation"/>.
/// </para>
/// <para>
/// When the request includes the romanization flag (<c>dt=rm</c>), an
/// extra trailing entry may appear that carries only
/// <see cref="SourceTransliteration"/> (and no <see cref="Translation"/>),
/// which is why every property here is optional.
/// </para>
/// </remarks>
internal sealed class GoogleSentence
{
    /// <summary>
    /// Gets or sets the translated text of this fragment, if this entry
    /// represents a translated sentence.
    /// </summary>
    [JsonPropertyName("trans")]
    public string? Translation { get; set; }

    /// <summary>
    /// Gets or sets the phonetic transliteration of the source text, if
    /// this entry carries one.
    /// </summary>
    [JsonPropertyName("src_translit")]
    public string? SourceTransliteration { get; set; }
}
