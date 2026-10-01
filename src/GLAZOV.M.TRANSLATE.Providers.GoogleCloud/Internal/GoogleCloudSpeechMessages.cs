using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Internal;

/// <summary>
/// The body Google Cloud Text-to-Speech takes.
/// </summary>
internal sealed class GoogleCloudSynthesizeRequest
{
    /// <summary>
    /// Gets or sets what to say.
    /// </summary>
    [JsonPropertyName("input")]
    public GoogleCloudSynthesisInput Input { get; set; } = new();

    /// <summary>
    /// Gets or sets in which voice to say it.
    /// </summary>
    [JsonPropertyName("voice")]
    public GoogleCloudVoiceSelection Voice { get; set; } = new();

    /// <summary>
    /// Gets or sets how to encode the recording.
    /// </summary>
    [JsonPropertyName("audioConfig")]
    public GoogleCloudAudioConfig AudioConfig { get; set; } = new();
}

/// <summary>
/// The text to be spoken.
/// </summary>
internal sealed class GoogleCloudSynthesisInput
{
    /// <summary>
    /// Gets or sets the plain text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// The voice to speak in: a language with a region, and optionally a named
/// voice of it.
/// </summary>
internal sealed class GoogleCloudVoiceSelection
{
    /// <summary>
    /// Gets or sets the BCP 47 code of the language with its region, such as
    /// <c>en-US</c>.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the voice, or <see langword="null"/> to take
    /// Google's default for the language.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
}

/// <summary>
/// How the recording is encoded.
/// </summary>
internal sealed class GoogleCloudAudioConfig
{
    /// <summary>
    /// Gets or sets the encoding.
    /// </summary>
    [JsonPropertyName("audioEncoding")]
    public string AudioEncoding { get; set; } = "MP3";
}

/// <summary>
/// The answer of Google Cloud Text-to-Speech.
/// </summary>
internal sealed class GoogleCloudSynthesizeResponse
{
    /// <summary>
    /// Gets or sets the recording, as base64.
    /// </summary>
    [JsonPropertyName("audioContent")]
    public string? AudioContent { get; set; }
}

/// <summary>
/// The answer of Google Cloud Text-to-Speech to the question which voices
/// speak a language.
/// </summary>
internal sealed class GoogleCloudVoicesResponse
{
    /// <summary>
    /// Gets or sets the voices, which is missing when no voice speaks the
    /// language.
    /// </summary>
    [JsonPropertyName("voices")]
    public IReadOnlyList<GoogleCloudVoice>? Voices { get; set; }
}

/// <summary>
/// A voice of Google Cloud Text-to-Speech.
/// </summary>
internal sealed class GoogleCloudVoice
{
    /// <summary>
    /// Gets or sets the BCP 47 codes of the languages the voice speaks.
    /// </summary>
    [JsonPropertyName("languageCodes")]
    public IReadOnlyList<string>? LanguageCodes { get; set; }
}
