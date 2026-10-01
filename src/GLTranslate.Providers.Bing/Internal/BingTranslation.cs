using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Bing.Internal;

/// <summary>
/// Represents one translation of the Bing Translator answer.
/// </summary>
internal sealed class BingTranslation
{
    /// <summary>
    /// Gets or sets the translated text.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the language code the text was translated into.
    /// </summary>
    [JsonPropertyName("to")]
    public string? To { get; set; }
}
