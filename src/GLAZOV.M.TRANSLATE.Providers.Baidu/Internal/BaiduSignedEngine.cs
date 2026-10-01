using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GLAZOV.M.TRANSLATE.Providers.Baidu.Internal;

/// <summary>
/// Represents the part the engines of the document and the speech endpoints of
/// Baidu share: a JSON request that carries its identity and its signature in
/// headers.
/// </summary>
/// <remarks>
/// Unlike the text and image endpoints, which sign values of the query, these
/// take the application, the time and the signature as the headers
/// <c>X-Appid</c>, <c>X-Timestamp</c> and <c>X-Sign</c>.
/// </remarks>
internal abstract class BaiduSignedEngine : BaiduEngineBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduSignedEngine"/> class
    /// with an <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="credentials">
    /// The application the calls are made for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    protected BaiduSignedEngine(BaiduCredentials credentials)
        : base(credentials)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduSignedEngine"/> class
    /// with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The application the calls are made for.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    protected BaiduSignedEngine(BaiduCredentials credentials, HttpClient httpClient)
        : base(credentials, httpClient)
    {
    }

    /// <summary>
    /// Sends a signed JSON request and reads the answer.
    /// </summary>
    /// <typeparam name="TResponse">
    /// The type of the answer.
    /// </typeparam>
    /// <param name="url">
    /// The endpoint.
    /// </param>
    /// <param name="body">
    /// The body of the request, exactly as it goes over the wire.
    /// </param>
    /// <param name="signedPayload">
    /// What is signed, or <see langword="null"/> to sign <paramref name="body"/>.
    /// </param>
    /// <param name="responseTypeInfo">
    /// How to read the answer.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The answer, or <see langword="null"/> when the body of the answer is
    /// the JSON <c>null</c>.
    /// </returns>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails or the answer cannot be read.
    /// </exception>
    protected async Task<TResponse?> PostSignedAsync<TResponse>(
        string url,
        string body,
        string? signedPayload,
        JsonTypeInfo<TResponse> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri(url))
        {
            Content = new StringContent(body, Encoding.UTF8, new MediaTypeHeaderValue("application/json")),
        };

        httpRequest.Headers.Add("X-Appid", Credentials.AppId);
        httpRequest.Headers.Add("X-Timestamp", timestamp);
        httpRequest.Headers.Add(
            "X-Sign",
            BaiduSignature.ForHeaders(Credentials.AppId, timestamp, signedPayload ?? body, Credentials.SecretKey));

        try
        {
            using HttpResponseMessage httpResponse = await HttpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            httpResponse.EnsureSuccessStatusCode();

            return await httpResponse.Content
                .ReadFromJsonAsync(responseTypeInfo, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw RequestFailed(exception);
        }
        catch (JsonException exception)
        {
            throw UnreadableAnswer(exception);
        }
    }
}
