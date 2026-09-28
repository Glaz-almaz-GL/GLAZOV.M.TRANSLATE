using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Microsoft.Internal;

/// <summary>
/// Represents one translation of the Microsoft Translator response.
/// </summary>
internal sealed class MicrosoftTranslation
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
