using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GLTranslate.Providers.Yandex.Internal;

/// <summary>
/// Performs the calls against the Yandex endpoints.
/// </summary>
/// <remarks>
/// <para>
/// This type is the business logic of the Yandex providers and is not part of
/// the public API.
/// </para>
/// <para>
/// The endpoints need no key. They expect the requests of the Yandex
/// Translate mobile application: its user agent, and a session identifier the
/// application renews every few minutes.
/// </para>
/// <para>
/// A failure is reported inside the answer as well as by the status of the
/// response, so the answer is read in both cases.
/// </para>
/// </remarks>
internal sealed class YandexEngine : ProviderEngine
{
    private const string ApiUrl = "https://translate.yandex.net/api/v1/tr.json";

    private const string TransliterationUrl = "https://translate.yandex.net/translit/translit";

    private const string RecognitionUrl = "https://translate.yandex.net/ocr/v1.1/recognize";

    private const string UserAgent = "ru.yandex.translate/3.20.2024";

    // The application renews the session identifier every few minutes; the
    // endpoints start refusing one that has been in use for too long.
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(5);

    private readonly Lock _sessionLock = new();

    private Guid _session;
    private DateTimeOffset _sessionExpiresAt;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexEngine"/> class with
    /// an <see cref="HttpClient"/> of its own.
    /// </summary>
    public YandexEngine()
        : base(YandexProvider.Name)
    {
        AnnounceTheApplication();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexEngine"/> class with
    /// the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public YandexEngine(HttpClient httpClient)
        : base(YandexProvider.Name, httpClient)
    {
        AnnounceTheApplication();
    }

    private void AnnounceTheApplication()
    {
        if (HttpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            // The endpoints answer a caller that does not look like the mobile
            // application with an error.
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }
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
    /// is empty or consists only of white-space characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, when the endpoint reports a failure, or
    /// when it answers with something this engine cannot read.
    /// </exception>
    public async Task<(string TranslatedText, string SourceLanguageCode)> TranslateAsync(
        string text,
        string targetLanguageCode,
        string? sourceLanguageCode,
        CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<string> translations, string resolvedSourceLanguageCode) = await TranslateAsync(
            [text],
            targetLanguageCode,
            sourceLanguageCode,
            cancellationToken)
            .ConfigureAwait(false);

        return (translations[0], resolvedSourceLanguageCode);
    }

    /// <summary>
    /// Translates several texts in one request.
    /// </summary>
    /// <param name="texts">
    /// The texts to translate. The endpoint answers with one translation per
    /// text, in the same order.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The language code to translate into.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The language code the texts are written in, or <see langword="null"/>
    /// to let the endpoint detect it.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The translations and the code of the language they were translated from.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="texts"/> is empty or holds an empty text,
    /// or when <paramref name="targetLanguageCode"/> is empty or consists only
    /// of white-space characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, when the endpoint reports a failure, or
    /// when it answers with something this engine cannot read.
    /// </exception>
    public async Task<(IReadOnlyList<string> Translations, string SourceLanguageCode)> TranslateAsync(
        IReadOnlyList<string> texts,
        string targetLanguageCode,
        string? sourceLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(texts);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        if (texts.Count == 0)
        {
            throw new ArgumentException("There is nothing to translate.", nameof(texts));
        }

        string direction = sourceLanguageCode is null
            ? targetLanguageCode
            : $"{sourceLanguageCode}-{targetLanguageCode}";

        List<KeyValuePair<string, string>> fields = [];

        foreach (string text in texts)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(texts));

            fields.Add(new("text", text));
        }

        fields.Add(new("lang", direction));

        YandexTranslationResponse? answer = await PostAsync(
            $"{ApiUrl}/translate{Query()}",
            [.. fields],
            YandexTranslationJsonContext.Default.YandexTranslationResponse,
            cancellationToken)
            .ConfigureAwait(false);

        if (answer is null)
        {
            // Empty response
            throw new ProviderException(YandexProvider.Name, "Yandex returned an empty response.");
        }

        Ensure(answer.Code, answer.Message);

        if (answer.Text is not { Count: > 0 } translations || translations.Count != texts.Count)
        {
            // No translation
            throw new ProviderException(YandexProvider.Name, "Yandex returned no translation.");
        }

        return (translations, ReadSourceLanguage(answer.Lang, sourceLanguageCode));
    }

    /// <summary>
    /// Translates the specified markup, leaving its tags where they are.
    /// </summary>
    /// <param name="markup">
    /// The markup to translate.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The language code to translate into.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The language code the markup is written in, or <see langword="null"/>
    /// to let the endpoint detect it.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The translated markup and the code of the language it was translated
    /// from.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="markup"/> or <paramref name="targetLanguageCode"/>
    /// is empty or consists only of white-space characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, when the endpoint reports a failure, or
    /// when it answers with something this engine cannot read.
    /// </exception>
    public async Task<(string TranslatedMarkup, string SourceLanguageCode)> TranslateMarkupAsync(
        string markup,
        string targetLanguageCode,
        string? sourceLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(markup);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        string direction = sourceLanguageCode is null
            ? targetLanguageCode
            : $"{sourceLanguageCode}-{targetLanguageCode}";

        YandexTranslationResponse? answer = await PostAsync(
            $"{ApiUrl}/translate{Query(format: "html")}",
            [new("text", markup), new("lang", direction)],
            YandexTranslationJsonContext.Default.YandexTranslationResponse,
            cancellationToken)
            .ConfigureAwait(false);

        if (answer is null)
        {
            throw new ProviderException(YandexProvider.Name, "Yandex returned an empty response.");
        }

        Ensure(answer.Code, answer.Message);

        if (answer.Text is not [{ } translatedMarkup, ..])
        {
            throw new ProviderException(YandexProvider.Name, "Yandex returned no translation.");
        }

        return (translatedMarkup, ReadSourceLanguage(answer.Lang, sourceLanguageCode));
    }

    /// <summary>
    /// Detects the language the specified text is written in.
    /// </summary>
    /// <param name="text">
    /// The text to look at.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The code of the detected language.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text"/> is empty or consists only of
    /// white-space characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, when the endpoint reports a failure, or
    /// when it detects nothing.
    /// </exception>
    public async Task<string> DetectLanguageAsync(string text, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        // This endpoint refuses a POST - "HTTP method is invalid for this
        // service" - and expects the fields in the body of a GET, which is how
        // the mobile application asks it.
        YandexDetectionResponse? answer = await SendAsync(
            HttpMethod.Get,
            $"{ApiUrl}/detect{Query()}",
            [new("text", text), new("hint", "en")],
            YandexTranslationJsonContext.Default.YandexDetectionResponse,
            cancellationToken)
            .ConfigureAwait(false);

        if (answer is null)
        {
            throw new ProviderException(YandexProvider.Name, "Yandex returned an empty response.");
        }

        Ensure(answer.Code, answer.Message);

        return string.IsNullOrWhiteSpace(answer.Lang)
            ? throw new ProviderException(YandexProvider.Name, "Yandex detected no language.")
            : answer.Lang;
    }

    /// <summary>
    /// Transliterates the specified text into the writing system of another
    /// language.
    /// </summary>
    /// <param name="text">
    /// The text to transliterate.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The language code the text is written in.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The language code whose writing system to render into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The transliterated text.
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
    public async Task<string> TransliterateAsync(
        string text,
        string sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLanguageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        // This endpoint answers with a bare JSON string rather than the object
        // the others answer with, and reports a failure by status alone.
        string? transliteration = await PostAsync(
            TransliterationUrl,
            [new("text", text), new("lang", $"{sourceLanguageCode}-{targetLanguageCode}")],
            YandexTranslationJsonContext.Default.String,
            cancellationToken)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(transliteration)
            ? throw new ProviderException(YandexProvider.Name, "Yandex returned no transliteration.")
            : transliteration;
    }

    /// <summary>
    /// Reads the text on the specified image.
    /// </summary>
    /// <param name="image">
    /// The bytes of the image.
    /// </param>
    /// <param name="mediaType">
    /// The media type of the image, such as <c>image/jpeg</c>.
    /// </param>
    /// <param name="fileName">
    /// The file name to send the image under.
    /// </param>
    /// <param name="languageCode">
    /// The language code the text is expected to be written in, or
    /// <see langword="null"/> to let the endpoint decide.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// What the endpoint read.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an argument is empty or consists only of white-space
    /// characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, when the endpoint reports a failure, or
    /// when it answers with something this engine cannot read.
    /// </exception>
    public async Task<YandexOcrData> RecognizeAsync(
        ReadOnlyMemory<byte> image,
        string mediaType,
        string fileName,
        string? languageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (image.IsEmpty)
        {
            throw new ArgumentException("An image cannot be empty.", nameof(image));
        }

        string url = $"{RecognitionUrl}?srv=android&sid={GetSession():N}" +
                     $"&lang={Uri.EscapeDataString(languageCode ?? "*")}&rotate=auto";

        // This endpoint refuses the bytes on their own - "Unsupported media
        // type" - and wants them as a file of a form, the way the application
        // uploads a photograph.
        using MultipartFormDataContent content = [];
        using ByteArrayContent file = new(image.ToArray());

        file.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        content.Add(file, "file", fileName);

        YandexOcrResponse? answer;

        try
        {
            using HttpResponseMessage httpResponse = await HttpClient
                .PostAsync(new Uri(url), content, cancellationToken)
                .ConfigureAwait(false);

            answer = await httpResponse.Content
                .ReadFromJsonAsync(YandexTranslationJsonContext.Default.YandexOcrResponse, cancellationToken)
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

        if (answer is null || !string.Equals(answer.Status, "success", StringComparison.Ordinal))
        {
            throw new ProviderException(YandexProvider.Name, "Yandex could not read the image.");
        }

        return answer.Data ?? new YandexOcrData();
    }

    private static string ReadSourceLanguage(string? direction, string? sourceLanguageCode)
    {
        if (sourceLanguageCode is not null)
        {
            return sourceLanguageCode;
        }

        // The answer names the direction it translated in, which is where the
        // detected source language is reported.
        int separator = direction?.IndexOf('-') ?? -1;

        return separator <= 0
            ? throw new ProviderException(
                YandexProvider.Name,
                "Yandex named no source language, although none was given.")
            : direction![..separator];
    }

    private static void Ensure(int code, string? message)
    {
        if (code == 200)
        {
            return;
        }

        throw new ProviderException(
            YandexProvider.Name,
            string.IsNullOrWhiteSpace(message)
                ? $"Yandex refused the request with code {code}."
                : $"Yandex refused the request: {message}");
    }

    private Task<TResponse?> PostAsync<TResponse>(
        string url,
        KeyValuePair<string, string>[] fields,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<TResponse> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        return SendAsync(HttpMethod.Post, url, fields, responseTypeInfo, cancellationToken);
    }

    private async Task<TResponse?> SendAsync<TResponse>(
        HttpMethod method,
        string url,
        KeyValuePair<string, string>[] fields,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<TResponse> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage httpRequest = new(method, new Uri(url))
        {
            Content = new FormUrlEncodedContent(fields),
        };

        try
        {
            using HttpResponseMessage httpResponse = await HttpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            // A refusal carries its explanation in the body, so the body is read
            // before the status is judged.
            TResponse? answer = await httpResponse.Content
                .ReadFromJsonAsync(responseTypeInfo, cancellationToken)
                .ConfigureAwait(false);

            return !httpResponse.IsSuccessStatusCode && answer is not YandexTranslationResponse and not YandexDetectionResponse
                ? throw new ProviderException(
                    YandexProvider.Name,
                    $"The request to Yandex failed with status {(int)httpResponse.StatusCode}.")
                : answer;
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

    private string Query(string format = "text")
    {
        return $"?ucid={GetSession():N}&srv=android&format={format}";
    }

    private Guid GetSession()
    {
        lock (_sessionLock)
        {
            if (DateTimeOffset.UtcNow >= _sessionExpiresAt)
            {
                _session = Guid.NewGuid();
                _sessionExpiresAt = DateTimeOffset.UtcNow + SessionLifetime;
            }

            return _session;
        }
    }

}
