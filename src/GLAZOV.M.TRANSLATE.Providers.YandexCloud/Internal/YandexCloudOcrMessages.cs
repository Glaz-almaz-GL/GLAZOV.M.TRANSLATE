using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.YandexCloud.Internal;

/// <summary>
/// The body Yandex Cloud Vision OCR takes.
/// </summary>
internal sealed class YandexCloudRecognizeRequest
{
    /// <summary>
    /// Gets or sets the bytes of the image, as base64.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the media type of the image.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the codes of the languages the text is written in.
    /// </summary>
    [JsonPropertyName("languageCodes")]
    public IReadOnlyList<string> LanguageCodes { get; set; } = [];

    /// <summary>
    /// Gets or sets the recognition model.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = "page";
}

/// <summary>
/// The answer of Yandex Cloud Vision OCR.
/// </summary>
/// <remarks>
/// The method streams in gRPC, and its REST gateway may wrap what it streams in
/// <c>result</c>; the answer is read either way.
/// </remarks>
internal sealed class YandexCloudRecognizeResponse
{
    /// <summary>
    /// Gets or sets the text found, when the gateway wrapped it in
    /// <c>result</c>.
    /// </summary>
    [JsonPropertyName("result")]
    public YandexCloudRecognizeResponse? Result { get; set; }

    /// <summary>
    /// Gets or sets the text found on the image.
    /// </summary>
    [JsonPropertyName("textAnnotation")]
    public YandexCloudTextAnnotation? TextAnnotation { get; set; }
}

/// <summary>
/// The text found on an image.
/// </summary>
internal sealed class YandexCloudTextAnnotation
{
    /// <summary>
    /// Gets or sets the blocks of text.
    /// </summary>
    [JsonPropertyName("blocks")]
    public IReadOnlyList<YandexCloudBlock>? Blocks { get; set; }
}

/// <summary>
/// A block of text.
/// </summary>
internal sealed class YandexCloudBlock
{
    /// <summary>
    /// Gets or sets the lines of the block.
    /// </summary>
    [JsonPropertyName("lines")]
    public IReadOnlyList<YandexCloudLine>? Lines { get; set; }

    /// <summary>
    /// Gets or sets the languages detected in the block, most likely first.
    /// </summary>
    [JsonPropertyName("languages")]
    public IReadOnlyList<YandexCloudDetectedLanguage>? Languages { get; set; }
}

/// <summary>
/// A language Yandex thinks a block is written in.
/// </summary>
internal sealed class YandexCloudDetectedLanguage
{
    /// <summary>
    /// Gets or sets the code of the language.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public string? LanguageCode { get; set; }
}

/// <summary>
/// A line of text and where it sits.
/// </summary>
internal sealed class YandexCloudLine
{
    /// <summary>
    /// Gets or sets the line.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets where the line sits.
    /// </summary>
    [JsonPropertyName("boundingBox")]
    public YandexCloudPolygon? BoundingBox { get; set; }

    /// <summary>
    /// Gets or sets the words of the line.
    /// </summary>
    [JsonPropertyName("words")]
    public IReadOnlyList<YandexCloudWord>? Words { get; set; }
}

/// <summary>
/// A word and where it sits.
/// </summary>
internal sealed class YandexCloudWord
{
    /// <summary>
    /// Gets or sets the word.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets where the word sits.
    /// </summary>
    [JsonPropertyName("boundingBox")]
    public YandexCloudPolygon? BoundingBox { get; set; }
}

/// <summary>
/// A polygon around a piece of text, by its corners.
/// </summary>
internal sealed class YandexCloudPolygon
{
    /// <summary>
    /// Gets or sets the corners.
    /// </summary>
    [JsonPropertyName("vertices")]
    public IReadOnlyList<YandexCloudVertex>? Vertices { get; set; }
}

/// <summary>
/// A corner of a <see cref="YandexCloudPolygon"/>, in pixels. Yandex writes the
/// coordinates as strings, and leaves out one that is zero.
/// </summary>
internal sealed class YandexCloudVertex
{
    /// <summary>
    /// Gets or sets the distance from the left edge.
    /// </summary>
    [JsonPropertyName("x")]
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the distance from the top edge.
    /// </summary>
    [JsonPropertyName("y")]
    public int Y { get; set; }
}
