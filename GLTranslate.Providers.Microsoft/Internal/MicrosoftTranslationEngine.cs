using GLTranslate.Abstractions.Providers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GLTranslate.Providers.Microsoft.Internal;

/// <summary>
/// Performs the translation calls against the Microsoft Translator endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This type is the business logic of the Microsoft provider and is not part
/// of the public API.
/// </para>
/// <para>
/// The endpoint needs no subscription key: every request carries the
/// <c>X-MT-Signature</c> header built by <see cref="MicrosoftSignature"/>.
/// </para>
/// </remarks>
internal sealed class MicrosoftTranslationEngine : IDisposable
{
    private const string ApiHost = "api.cognitive.microsofttranslator.com";

    private const string ApiVersion = "3.0";

    // The endpoint refuses a longer text with 400 Bad Request.
    private const int MaxTextLength = 1000;

    private readonly HttpClient _httpClient;
    private readonly bool _isExternalHttpClient;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTranslationEngine"/>
    /// class with an <see cref="HttpClient"/> of its own.
    /// </summary>
    public MicrosoftTranslationEngine()
    {
        _httpClient = new HttpClient();
        _isExternalHttpClient = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTranslationEngine"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public MicrosoftTranslationEngine(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
        _isExternalHttpClient = true;
    }

    /// <summary>
    /// Translates the specified text.
    /// </summary>
    /// <param name="text">
    /// The text to translate.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The language code to translate into.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The language code the text is written in, or <see langword="null"/> to
    /// let the endpoint detect it.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The translated text and the code of the language it was translated from.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text"/> or <paramref name="targetLanguageCode"/>
    /// is empty or consists only of white-space characters, or when
    /// <paramref name="text"/> is longer than the endpoint accepts.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails or the endpoint answers with something
    /// this engine cannot read.
    /// </exception>
    public async Task<(string TranslatedText, string SourceLanguageCode)> TranslateAsync(
        string text,
        string targetLanguageCode,
        string? sourceLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        if (text.Length > MaxTextLength)
        {
            throw new ArgumentException(
                $"Microsoft Translator accepts at most {MaxTextLength} characters per request.",
                nameof(text));
        }

        // The signature covers the URL without its scheme, so the same string
        // is both signed and sent.
        string url = $"{ApiHost}/translate?api-version={ApiVersion}&to={Uri.EscapeDataString(targetLanguageCode)}";

        if (sourceLanguageCode is not null)
        {
            url += $"&from={Uri.EscapeDataString(sourceLanguageCode)}";
        }

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri($"https://{url}"))
        {
            Content = JsonContent.Create(
                new[] { new MicrosoftTranslationRequest { Text = text } },
                MicrosoftTranslationJsonContext.Default.MicrosoftTranslationRequestArray),
        };

        httpRequest.Headers.Add("X-MT-Signature", MicrosoftSignature.Create(url));

        try
        {
            using HttpResponseMessage httpResponse = await _httpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            httpResponse.EnsureSuccessStatusCode();

            MicrosoftTranslationResponse[]? responses = await httpResponse.Content
                .ReadFromJsonAsync(MicrosoftTranslationJsonContext.Default.MicrosoftTranslationResponseArray, cancellationToken)
                .ConfigureAwait(false);

            return Read(responses, sourceLanguageCode);
        }
        catch (HttpRequestException exception)
        {
            throw new ProviderException(MicrosoftProvider.Name, "The request to Microsoft Translator failed.", exception);
        }
        catch (JsonException exception)
        {
            throw new ProviderException(MicrosoftProvider.Name, "Microsoft Translator returned an unexpected response format.", exception);
        }
    }

    private static (string TranslatedText, string SourceLanguageCode) Read(
        MicrosoftTranslationResponse[]? responses,
        string? sourceLanguageCode)
    {
        if (responses is not [{ } response, ..])
        {
            throw new ProviderException(MicrosoftProvider.Name, "Microsoft Translator returned an empty response.");
        }

        if (response.Translations is not [{ Text: { } translatedText }, ..])
        {
            throw new ProviderException(MicrosoftProvider.Name, "Microsoft Translator returned no translation.");
        }

        string resolvedSourceLanguageCode = sourceLanguageCode
            ?? response.DetectedLanguage?.Language
            ?? throw new ProviderException(
                MicrosoftProvider.Name,
                "Microsoft Translator detected no source language, although none was given.");

        return (translatedText, resolvedSourceLanguageCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (!_isExternalHttpClient)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
    }
}
