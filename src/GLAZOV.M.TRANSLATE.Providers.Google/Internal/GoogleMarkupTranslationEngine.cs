using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;
using System.Net;
using System.Text.Json;

namespace GLAZOV.M.TRANSLATE.Providers.Google.Internal;

/// <summary>
/// Performs the markup translation calls against the Google Translate web
/// endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This type is the business logic of the Google markup translation provider
/// and is not part of the public API.
/// </para>
/// <para>
/// Markup goes to a different endpoint than text: <c>translate_a/t</c> with
/// <c>format=html</c>, which returns the same markup with the words between
/// the tags translated. Its answer is a bare array rather than the object the
/// text endpoint returns, and its shape depends on whether the request named
/// the source language.
/// </para>
/// </remarks>
internal sealed class GoogleMarkupTranslationEngine : ProviderEngine
{
    private const string ApiEndpoint = "https://translate.googleapis.com/translate_a/t";

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleMarkupTranslationEngine"/>
    /// class with an <see cref="HttpClient"/> of its own.
    /// </summary>
    public GoogleMarkupTranslationEngine()
        : base(GoogleProvider.Name)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleMarkupTranslationEngine"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public GoogleMarkupTranslationEngine(HttpClient httpClient)
        : base(GoogleProvider.Name, httpClient)
    {
    }

    /// <summary>
    /// Translates the specified markup.
    /// </summary>
    /// <param name="markup">
    /// The markup to translate.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The language code the markup is written in, or <c>"auto"</c> to let the
    /// endpoint detect it.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The language code to translate into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The translated markup and the code of the language it was translated
    /// from, which the endpoint reports only when it detected it.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an argument is empty or consists only of white-space
    /// characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails or the endpoint answers with something
    /// this engine cannot read.
    /// </exception>
    public async Task<(string TranslatedMarkup, string? DetectedSourceCode)> TranslateAsync(
        string markup,
        string sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(markup);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLanguageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        string url = $"{ApiEndpoint}?client=gtx" +
                     $"&sl={Uri.EscapeDataString(sourceLanguageCode)}" +
                     $"&tl={Uri.EscapeDataString(targetLanguageCode)}" +
                     "&format=html" +
                     $"&tk={Uri.EscapeDataString(GoogleTokenGenerator.Generate(markup))}";

        // The endpoint answers 429 Too Many Requests to every HTTP/1.1 call,
        // exactly as the text endpoint does, and answers the same request over
        // HTTP/2 normally.
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri(url))
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
            Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("q", markup)]),
        };

        try
        {
            using HttpResponseMessage httpResponse = await HttpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            httpResponse.EnsureSuccessStatusCode();

            await using Stream stream = await httpResponse.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);

            using JsonDocument document = await JsonDocument
                .ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Read(document.RootElement);
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

    private (string TranslatedMarkup, string? DetectedSourceCode) Read(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            throw new ProviderException(ProviderName, "Google Translate returned an empty response.");
        }

        JsonElement first = root[0];

        // A request that named the source language is answered with the markup
        // alone; one that did not is answered with the markup and the language
        // the endpoint detected.
        if (first.ValueKind == JsonValueKind.String)
        {
            return (first.GetString()!, null);
        }

        if (first.ValueKind == JsonValueKind.Array
            && first.GetArrayLength() >= 2
            && first[0].ValueKind == JsonValueKind.String
            && first[1].ValueKind == JsonValueKind.String)
        {
            return (first[0].GetString()!, first[1].GetString());
        }

        throw new ProviderException(ProviderName, "Google Translate returned an unexpected response format.");
    }
}
