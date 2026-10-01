using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Microsoft.Internal;

/// <summary>
/// Represents one text of the Microsoft Translator request body.
/// </summary>
internal sealed class MicrosoftTranslationRequest
{
    /// <summary>
    /// Gets or sets the text to translate.
    /// </summary>
    [JsonPropertyName("Text")]
    public required string Text { get; set; }
}
