using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Yandex.Internal;

/// <summary>
/// Represents the answer of the Yandex text recognition endpoint.
/// </summary>
internal sealed class YandexOcrResponse
{
    /// <summary>
    /// Gets or sets the outcome the endpoint reports inside the answer, which
    /// is <c>success</c> when it read the image.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets what was read.
    /// </summary>
    [JsonPropertyName("data")]
    public YandexOcrData? Data { get; set; }
}

/// <summary>
/// Represents what the recognition endpoint read from an image.
/// </summary>
internal sealed class YandexOcrData
{
    /// <summary>
    /// Gets or sets the language the endpoint decided the text is written in.
    /// </summary>
    [JsonPropertyName("detected_lang")]
    public string? DetectedLanguage { get; set; }

    /// <summary>
    /// Gets or sets the blocks of text found on the image.
    /// </summary>
    [JsonPropertyName("blocks")]
    public IReadOnlyList<YandexOcrBlock>? Blocks { get; set; }
}

/// <summary>
/// Represents one block of text on an image, which holds its lines.
/// </summary>
internal sealed class YandexOcrBlock
{
    /// <summary>
    /// Gets or sets the lines of the block.
    /// </summary>
    [JsonPropertyName("boxes")]
    public IReadOnlyList<YandexOcrBox>? Boxes { get; set; }
}

/// <summary>
/// Represents one line of text on an image.
/// </summary>
internal sealed class YandexOcrBox
{
    /// <summary>
    /// Gets or sets the distance from the left edge of the image.
    /// </summary>
    [JsonPropertyName("x")]
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the distance from the top edge of the image.
    /// </summary>
    [JsonPropertyName("y")]
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the line.
    /// </summary>
    [JsonPropertyName("w")]
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the line.
    /// </summary>
    [JsonPropertyName("h")]
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the line as it was read.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the words of the line.
    /// </summary>
    [JsonPropertyName("words")]
    public IReadOnlyList<YandexOcrBox>? Words { get; set; }
}
