using System.Text.Json.Serialization;

namespace GLTranslate.Providers.YandexCloud.Internal;

/// <summary>
/// The body Yandex Cloud Translate takes.
/// </summary>
internal sealed class YandexCloudTranslateRequest
{
    /// <summary>
    /// Gets or sets the code of the language to translate into.
    /// </summary>
    [JsonPropertyName("targetLanguageCode")]
    public string TargetLanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the code of the language to translate from, or
    /// <see langword="null"/> to let Yandex detect it.
    /// </summary>
    [JsonPropertyName("sourceLanguageCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceLanguageCode { get; set; }

    /// <summary>
    /// Gets or sets <c>PLAIN_TEXT</c> or <c>HTML</c>.
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "PLAIN_TEXT";

    /// <summary>
    /// Gets or sets the pieces to translate.
    /// </summary>
    [JsonPropertyName("texts")]
    public IReadOnlyList<string> Texts { get; set; } = [];

    /// <summary>
    /// Gets or sets the folder to bill, or <see langword="null"/> to leave it
    /// to the key.
    /// </summary>
    [JsonPropertyName("folderId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FolderId { get; set; }
}

/// <summary>
/// The answer of Yandex Cloud Translate.
/// </summary>
internal sealed class YandexCloudTranslateResponse
{
    /// <summary>
    /// Gets or sets one translation for each piece sent, in the order sent.
    /// </summary>
    [JsonPropertyName("translations")]
    public IReadOnlyList<YandexCloudTranslation>? Translations { get; set; }
}

/// <summary>
/// One translated piece.
/// </summary>
internal sealed class YandexCloudTranslation
{
    /// <summary>
    /// Gets or sets the translated piece.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the code of the language the piece was detected to be in,
    /// which Yandex reports only when none was given.
    /// </summary>
    [JsonPropertyName("detectedLanguageCode")]
    public string? DetectedLanguageCode { get; set; }
}

/// <summary>
/// The explanation Yandex Cloud gives of a request it refuses, in the body of
/// the answer.
/// </summary>
internal sealed class YandexCloudErrorResponse
{
    /// <summary>
    /// Gets or sets what went wrong.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
