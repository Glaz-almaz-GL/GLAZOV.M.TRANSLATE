using System.Text.Json.Serialization;

namespace GLTranslate.Providers.Yandex.Internal;

/// <summary>
/// Represents the answer of the Yandex translation endpoint.
/// </summary>
internal sealed class YandexTranslationResponse
{
    /// <summary>
    /// Gets or sets the status the endpoint reports inside the answer, which
    /// is not the status of the response itself.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the explanation of a failure, when there is one.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the translated texts, one per requested text.
    /// </summary>
    [JsonPropertyName("text")]
    public IReadOnlyList<string>? Text { get; set; }

    /// <summary>
    /// Gets or sets the direction the endpoint translated in, such as
    /// <c>en-ru</c>, which names the source language even when the request
    /// did not.
    /// </summary>
    [JsonPropertyName("lang")]
    public string? Lang { get; set; }
}
