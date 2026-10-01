using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Internal;

/// <summary>
/// The body Google Cloud Translation takes: what to translate, from and into
/// which language, and whether the text is plain or markup.
/// </summary>
internal sealed class GoogleCloudTranslateRequest
{
    /// <summary>
    /// Gets or sets the pieces to translate.
    /// </summary>
    [JsonPropertyName("q")]
    public IReadOnlyList<string> Text { get; set; } = [];

    /// <summary>
    /// Gets or sets the code of the language to translate from, or
    /// <see langword="null"/> to let Google detect it.
    /// </summary>
    [JsonPropertyName("source")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the code of the language to translate into.
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets <c>text</c> or <c>html</c>. Google assumes <c>html</c> when
    /// it is left out, which would escape plain text, so it is always sent.
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "text";
}

/// <summary>
/// The answer of Google Cloud Translation.
/// </summary>
internal sealed class GoogleCloudTranslateResponse
{
    /// <summary>
    /// Gets or sets the translations.
    /// </summary>
    [JsonPropertyName("data")]
    public GoogleCloudTranslateData? Data { get; set; }
}

/// <summary>
/// The translations in an answer of Google Cloud Translation.
/// </summary>
internal sealed class GoogleCloudTranslateData
{
    /// <summary>
    /// Gets or sets one translation for each piece sent, in the order sent.
    /// </summary>
    [JsonPropertyName("translations")]
    public IReadOnlyList<GoogleCloudTranslation>? Translations { get; set; }
}

/// <summary>
/// One translated piece.
/// </summary>
internal sealed class GoogleCloudTranslation
{
    /// <summary>
    /// Gets or sets the translated piece.
    /// </summary>
    [JsonPropertyName("translatedText")]
    public string? TranslatedText { get; set; }

    /// <summary>
    /// Gets or sets the code of the language the piece was detected to be in,
    /// which Google reports only when none was given.
    /// </summary>
    [JsonPropertyName("detectedSourceLanguage")]
    public string? DetectedSourceLanguage { get; set; }
}

/// <summary>
/// The explanation Google Cloud gives of a request it refuses, in the body of
/// the answer.
/// </summary>
internal sealed class GoogleCloudErrorResponse
{
    /// <summary>
    /// Gets or sets the error.
    /// </summary>
    [JsonPropertyName("error")]
    public GoogleCloudError? Error { get; set; }
}

/// <summary>
/// An error of Google Cloud.
/// </summary>
internal sealed class GoogleCloudError
{
    /// <summary>
    /// Gets or sets the HTTP status the error stands for.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets what went wrong.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
