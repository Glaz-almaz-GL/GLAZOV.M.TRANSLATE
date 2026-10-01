using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Baidu.Internal;

/// <summary>
/// The answer of the text and markup endpoints of Baidu.
/// </summary>
/// <remarks>
/// A success carries no error code at all; a failure carries nothing but the
/// code and its message.
/// </remarks>
internal sealed class BaiduTextResponse
{
    /// <summary>
    /// Gets or sets the error code, or zero when the request succeeded.
    /// </summary>
    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the explanation of the error.
    /// </summary>
    [JsonPropertyName("error_msg")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the code of the language the text was translated from.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the translated pieces, one for each line of the text sent.
    /// </summary>
    [JsonPropertyName("trans_result")]
    public IReadOnlyList<BaiduTextLine>? Lines { get; set; }
}

/// <summary>
/// One line of the text as it was sent and as it was translated.
/// </summary>
internal sealed class BaiduTextLine
{
    /// <summary>
    /// Gets or sets the line as it was sent.
    /// </summary>
    [JsonPropertyName("src")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the line translated.
    /// </summary>
    [JsonPropertyName("dst")]
    public string? Destination { get; set; }
}
