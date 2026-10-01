using System.Text.Json.Serialization;

namespace GLAZOV.M.TRANSLATE.Providers.Yandex.Internal;

/// <summary>
/// Represents the answer of the Yandex language detection endpoint.
/// </summary>
internal sealed class YandexDetectionResponse
{
    /// <summary>
    /// Gets or sets the status the endpoint reports inside the answer.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the explanation of a failure, when there is one.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the detected language code.
    /// </summary>
    [JsonPropertyName("lang")]
    public string? Lang { get; set; }
}
