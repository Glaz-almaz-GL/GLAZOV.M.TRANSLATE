using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Baidu.Internal;

/// <summary>
/// The answer of the speech endpoint of Baidu.
/// </summary>
internal sealed class BaiduSpeechResponse
{
    /// <summary>
    /// Gets or sets the code of the answer; zero means success.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the explanation of the answer.
    /// </summary>
    [JsonPropertyName("msg")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets what was heard and translated, which is there only on
    /// success.
    /// </summary>
    [JsonPropertyName("data")]
    public BaiduSpeechData? Data { get; set; }
}

/// <summary>
/// What Baidu heard in a recording and how it translated it.
/// </summary>
internal sealed class BaiduSpeechData
{
    /// <summary>
    /// Gets or sets what was heard, in the language it was spoken in.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets what was heard, translated.
    /// </summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the translation spoken aloud, as the base64 of an MP3.
    /// </summary>
    [JsonPropertyName("target_tts")]
    public string? TargetSpeech { get; set; }
}
