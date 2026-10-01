using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.Baidu.Internal;

/// <summary>
/// The answer of the image endpoint of Baidu.
/// </summary>
internal sealed class BaiduPictureResponse
{
    /// <summary>
    /// Gets or sets the error code, which is zero when the request succeeded.
    /// </summary>
    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the explanation of the error.
    /// </summary>
    [JsonPropertyName("error_msg")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets what was read from the image.
    /// </summary>
    [JsonPropertyName("data")]
    public BaiduPictureData? Data { get; set; }
}

/// <summary>
/// What Baidu read from an image and how it translated it.
/// </summary>
internal sealed class BaiduPictureData
{
    /// <summary>
    /// Gets or sets the code of the language the text was translated from.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the segments of text, each with where it sits.
    /// </summary>
    [JsonPropertyName("content")]
    public IReadOnlyList<BaiduPictureSegment>? Content { get; set; }
}

/// <summary>
/// One segment of text Baidu read on an image, and its translation.
/// </summary>
internal sealed class BaiduPictureSegment
{
    /// <summary>
    /// Gets or sets the segment as it was read.
    /// </summary>
    [JsonPropertyName("src")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the segment translated.
    /// </summary>
    [JsonPropertyName("dst")]
    public string? Destination { get; set; }

    /// <summary>
    /// Gets or sets where the segment sits, as four numbers separated by
    /// spaces: left, top, width and height.
    /// </summary>
    [JsonPropertyName("rect")]
    public string? Rectangle { get; set; }
}
