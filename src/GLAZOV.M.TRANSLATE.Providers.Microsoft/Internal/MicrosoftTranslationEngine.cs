using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GLAZOV.M.TRANSLATE.Providers.Microsoft.Internal;

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
internal sealed class MicrosoftTranslationEngine : ProviderEngine
{
    private const string ApiHost = "api.cognitive.microsofttranslator.com";

    private const string ApiVersion = "3.0";

    // The endpoint refuses a longer text with 400 Bad Request.
    private const int MaxTextLength = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTranslationEngine"/>
    /// class with an <see cref="HttpClient"/> of its own.
    /// </summary>
    public MicrosoftTranslationEngine()
        : base(MicrosoftProvider.Name)
    {
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
        : base(MicrosoftProvider.Name, httpClient)
    {
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
        ThrowIfDisposed();
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

        MicrosoftTranslationResponse[]? responses = await SendAsync(
            url,
            text,
            MicrosoftTranslationJsonContext.Default.MicrosoftTranslationResponseArray,
            cancellationToken)
            .ConfigureAwait(false);

        return Read(responses, sourceLanguageCode);
    }

    /// <summary>
    /// Posts the text to the specified endpoint and reads the answer.
    /// </summary>
    /// <typeparam name="TResponse">
    /// The type the answer is read into.
    /// </typeparam>
    /// <param name="url">
    /// The request URL without its scheme. The same string is signed and sent.
    /// </param>
    /// <param name="text">
    /// The text to send.
    /// </param>
    /// <param name="responseTypeInfo">
    /// The metadata to read the answer with.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The answer of the endpoint.
    /// </returns>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails or the endpoint answers with something
    /// this engine cannot read.
    /// </exception>
    private async Task<TResponse?> SendAsync<TResponse>(
        string url,
        string text,
        JsonTypeInfo<TResponse> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri($"https://{url}"))
        {
            Content = JsonContent.Create(
                new[] { new MicrosoftTranslationRequest { Text = text } },
                MicrosoftTranslationJsonContext.Default.MicrosoftTranslationRequestArray),
        };

        httpRequest.Headers.Add("X-MT-Signature", MicrosoftSignature.Create(url));

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

    /// <summary>
    /// Transliterates the specified text from one writing system into another.
    /// </summary>
    /// <param name="text">
    /// The text to transliterate.
    /// </param>
    /// <param name="languageCode">
    /// The language code the text is written in. The endpoint does not detect
    /// it, so it must be given.
    /// </param>
    /// <param name="fromScript">
    /// The ISO 15924 code of the writing system the text is written in.
    /// </param>
    /// <param name="toScript">
    /// The ISO 15924 code of the writing system to transliterate into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The transliterated text.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an argument is empty or consists only of white-space
    /// characters, or when <paramref name="text"/> is longer than the endpoint
    /// accepts.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails or the endpoint answers with something
    /// this engine cannot read.
    /// </exception>
    public async Task<string> TransliterateAsync(
        string text,
        string languageCode,
        string fromScript,
        string toScript,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromScript);
        ArgumentException.ThrowIfNullOrWhiteSpace(toScript);

        if (text.Length > MaxTextLength)
        {
            throw new ArgumentException(
                $"Microsoft Translator accepts at most {MaxTextLength} characters per request.",
                nameof(text));
        }

        string url = $"{ApiHost}/transliterate?api-version={ApiVersion}" +
                     $"&language={Uri.EscapeDataString(languageCode)}" +
                     $"&fromScript={Uri.EscapeDataString(fromScript)}" +
                     $"&toScript={Uri.EscapeDataString(toScript)}";

        MicrosoftTransliteration[]? transliterations = await SendAsync(
            url,
            text,
            MicrosoftTranslationJsonContext.Default.MicrosoftTransliterationArray,
            cancellationToken)
            .ConfigureAwait(false);

        if (transliterations is not [{ Text: { } transliteratedText }, ..])
        {
            throw new ProviderException(MicrosoftProvider.Name, "Microsoft Translator returned no transliteration.");
        }

        return transliteratedText;
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

}
