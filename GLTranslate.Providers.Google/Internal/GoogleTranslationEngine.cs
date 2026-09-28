using GLTranslate.Abstractions.Providers;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GLTranslate.Providers.Google.Internal;

/// <summary>
/// Performs the HTTP calls and response parsing required to translate and
/// transliterate text through the free Google Translate web endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This is the provider's engine: it contains the business logic of the
/// operation and is not part of the public API. Consumers depend on
/// <see cref="GoogleTranslationProvider"/> or <see cref="GoogleTransliterationProvider"/>
/// instead.
/// </para>
/// <para>
/// This type is thread-safe as long as the supplied <see cref="HttpClient"/>
/// is thread-safe, which is the case for any <see cref="HttpClient"/> not
/// otherwise mutated after construction.
/// </para>
/// </remarks>
internal sealed class GoogleTranslationEngine : IDisposable
{
    private const string ApiEndpoint = "https://translate.googleapis.com/translate_a/single";

    private readonly HttpClient _httpClient;
    private readonly bool _isExternalHttpClient;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTranslationEngine"/> class.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client used to send requests to the Google Translate web
    /// endpoint. The engine does not own its lifetime.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public GoogleTranslationEngine(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
        _isExternalHttpClient = true;
    }

    public GoogleTranslationEngine()
    {
        _httpClient = new();
        _isExternalHttpClient = false;
    }

    /// <summary>
    /// Translates text from one ISO 639-1 language code to another.
    /// </summary>
    /// <param name="text">
    /// The text to translate.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The ISO 639-1 code of the source language, or <c>"auto"</c> to
    /// request automatic detection.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The ISO 639-1 code of the target language.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that completes with the translated text and the ISO 639-1
    /// code of the source language that was actually used.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text"/>, <paramref name="sourceLanguageCode"/>
    /// or <paramref name="targetLanguageCode"/> is empty or consists only
    /// of white-space characters.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, or when the response cannot be
    /// understood.
    /// </exception>
    public async Task<(string TranslatedText, string SourceLanguageCode)> TranslateAsync(
        string text,
        string sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        GoogleTranslationResponse response = await SendAsync(
            text, sourceLanguageCode, targetLanguageCode, includeRomanization: false, cancellationToken)
            .ConfigureAwait(false);

        string translation = response.Sentences is null
            ? string.Empty
            : string.Concat(response.Sentences.Select(sentence => sentence.Translation));

        return (translation, response.Source);
    }

    /// <summary>
    /// Gets the phonetic transliteration of text written in the specified
    /// language.
    /// </summary>
    /// <param name="text">
    /// The text to transliterate.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The ISO 639-1 code of the language the text is written in, or
    /// <c>"auto"</c> to request automatic detection.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task that completes with the transliteration of <paramref name="text"/>
    /// and the ISO 639-1 code of the source language that was actually used.
    /// </returns>
    /// <remarks>
    /// The Google Translate web endpoint only exposes transliteration as a
    /// by-product of translation, so this method performs a translation
    /// request to a fixed pivot language internally. When the source text
    /// is already written in the Latin script, Google returns no
    /// transliteration; in that case the original text is returned as-is.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text"/> or <paramref name="sourceLanguageCode"/>
    /// is empty or consists only of white-space characters.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, or when the response cannot be
    /// understood.
    /// </exception>
    public async Task<(string Transliteration, string SourceLanguageCode)> TransliterateAsync(
        string text,
        string sourceLanguageCode,
        CancellationToken cancellationToken = default)
    {
        const string PivotLanguageCode = "en";

        GoogleTranslationResponse response = await SendAsync(
            text, sourceLanguageCode, PivotLanguageCode, includeRomanization: true, cancellationToken)
            .ConfigureAwait(false);

        string? transliteration = response.Sentences?
            .Select(sentence => sentence.SourceTransliteration)
            .FirstOrDefault(value => value is not null);

        return (transliteration ?? text, response.Source);
    }

    /// <summary>
    /// Sends a request to the Google Translate web endpoint and parses the response.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="sourceLanguageCode">The ISO 639-1 code of the source language.</param>
    /// <param name="targetLanguageCode">The ISO 639-1 code of the target language.</param>
    /// <param name="includeRomanization">Whether to include romanization in the response.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that completes with the response from the Google Translate web endpoint.</returns>
    /// <exception cref="ProviderException">Thrown when the request fails or the response cannot be understood.</exception>
    private async Task<GoogleTranslationResponse> SendAsync(
        string text,
        string sourceLanguageCode,
        string targetLanguageCode,
        bool includeRomanization,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLanguageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        string token = GoogleTokenGenerator.Generate(text);

        string url = $"{ApiEndpoint}?client=gtx" +
                     $"&sl={Uri.EscapeDataString(sourceLanguageCode)}" +
                     $"&tl={Uri.EscapeDataString(targetLanguageCode)}" +
                     "&dt=t&dt=bd" +
                     (includeRomanization ? "&dt=rm" : string.Empty) +
                     "&dj=1&source=input" +
                     $"&tk={Uri.EscapeDataString(token)}";

        // The endpoint answers 429 Too Many Requests to every HTTP/1.1 call, whatever
        // the token, the headers or the caller, and answers the very same request over
        // HTTP/2 normally. That is why the version is requested explicitly instead of
        // being left to the HttpClient default, which is HTTP/1.1. There is deliberately
        // no fallback to HTTP/1.1: it would only replace a clear protocol failure with
        // the 429 the endpoint always gives there.
        //
        // Should HTTP/2 ever stop being an option, the same endpoint also answers over
        // HTTP/1.1 with client=dict-chrome-ex and no tk at all. That client returns the
        // same JSON shape - sentences, dict, src, src_translit - so only the query
        // string changes, and the token becomes dead weight.
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri(url))
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
            Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("q", text)]),
        };

        try
        {
            using HttpResponseMessage httpResponse = await _httpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            httpResponse.EnsureSuccessStatusCode();

            return await httpResponse.Content
                .ReadFromJsonAsync(GoogleTranslationJsonContext.Default.GoogleTranslationResponse, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new ProviderException(GoogleProvider.Name, "Google Translate returned an empty response.");
        }
        catch (HttpRequestException exception)
        {
            throw new ProviderException(GoogleProvider.Name, "The request to Google Translate failed.", exception);
        }
        catch (JsonException exception)
        {
            throw new ProviderException(GoogleProvider.Name, "Google Translate returned an unexpected response format.", exception);
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && !_isExternalHttpClient)
            {
                _httpClient?.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
