using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GLAZOV.M.TRANSLATE.Providers.YandexCloud.Internal;

/// <summary>
/// Performs the calls against the official Yandex Cloud APIs: Translate, Vision
/// OCR and SpeechKit.
/// </summary>
/// <remarks>
/// <para>
/// This type is the business logic of the Yandex Cloud providers and is not
/// part of the public API.
/// </para>
/// <para>
/// Every call carries the API key in the <c>Authorization</c> header, which
/// keeps it out of addresses, logs and exception messages. A refusal comes
/// back with its explanation in the body, which is what the exception reports.
/// </para>
/// </remarks>
internal sealed class YandexCloudEngine : CredentialedEngine<YandexCloudCredentials>
{
    private const string TranslationUrl = "https://translate.api.cloud.yandex.net/translate/v2/translate";
    private const string RecognitionUrl = "https://ocr.api.cloud.yandex.net/ocr/v1/recognizeText";
    private const string SynthesisUrl = "https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize";

    // Translation takes 10,000 characters in all, in a request at most.
    private const int MaxCharactersPerRequest = 10000;

    // SpeechKit speaks 5,000 characters in a request at most, and takes a
    // form of 15 KB at most, which the text must fit once it is encoded.
    private const int MaxSpeechCharacters = 5000;
    private const int MaxSpeechFormLength = 15 * 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudEngine"/> class
    /// with an <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="credentials">
    /// The key the calls are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public YandexCloudEngine(YandexCloudCredentials credentials)
        : base(YandexCloudProvider.Name, credentials)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudEngine"/> class
    /// with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The key the calls are made with.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    public YandexCloudEngine(YandexCloudCredentials credentials, HttpClient httpClient)
        : base(YandexCloudProvider.Name, credentials, httpClient)
    {
    }

    /// <summary>
    /// Translates pieces of text or markup.
    /// </summary>
    /// <param name="pieces">
    /// The pieces to translate. They are sent in as few requests as the limit
    /// of 10,000 characters in all allows.
    /// </param>
    /// <param name="isMarkup">
    /// <see langword="true"/> when the pieces are HTML, whose tags are kept;
    /// <see langword="false"/> when they are plain text.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The code of the language the pieces are written in, or
    /// <see langword="null"/> to let Yandex detect it.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The code of the language to translate into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// One translation for each piece, in the order given, and the code of the
    /// language the first piece was detected to be in when none was named.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when there is nothing to translate or an argument is empty or
    /// consists only of white-space characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a single piece is longer than a request may be, when a
    /// request fails, when Yandex refuses it, or when the answer cannot be
    /// read.
    /// </exception>
    public async Task<(IReadOnlyList<string> Translations, string? DetectedSourceLanguageCode)> TranslateAsync(
        IReadOnlyList<string> pieces,
        bool isMarkup,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(pieces);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        if (pieces.Count == 0)
        {
            throw new ArgumentException("There is nothing to translate.", nameof(pieces));
        }

        if (pieces.Any(piece => piece.Length > MaxCharactersPerRequest))
        {
            throw new ProviderException(
                YandexCloudProvider.Name,
                $"Yandex Cloud translates {MaxCharactersPerRequest.ToString(CultureInfo.InvariantCulture)} characters at most in one request.");
        }

        List<string> translations = new(pieces.Count);
        string? detected = null;

        foreach (string[] batch in Batch(pieces))
        {
            YandexCloudTranslateResponse? answer = await PostJsonAsync(
                TranslationUrl,
                new YandexCloudTranslateRequest
                {
                    TargetLanguageCode = targetLanguageCode,
                    SourceLanguageCode = sourceLanguageCode,
                    Format = isMarkup ? "HTML" : "PLAIN_TEXT",
                    Texts = batch,
                    FolderId = Credentials.FolderId,
                },
                YandexCloudJsonContext.Default.YandexCloudTranslateRequest,
                YandexCloudJsonContext.Default.YandexCloudTranslateResponse,
                cancellationToken)
                .ConfigureAwait(false);

            if (answer?.Translations is not { } batchTranslations
                || batchTranslations.Count != batch.Length
                || batchTranslations.Any(translation => translation.Text is null))
            {
                // No translation, or not one for every piece sent
                throw new ProviderException(YandexCloudProvider.Name, "Yandex Cloud returned no translation.");
            }

            detected ??= batchTranslations[0].DetectedLanguageCode;

            translations.AddRange(batchTranslations.Select(translation => translation.Text!));
        }

        return (translations, detected);
    }

    /// <summary>
    /// Speaks a text.
    /// </summary>
    /// <param name="text">
    /// The text to speak.
    /// </param>
    /// <param name="language">
    /// The code of the language to speak, with its region, such as
    /// <c>en-US</c>.
    /// </param>
    /// <param name="voice">
    /// The name of the voice.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The bytes of an MP3.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an argument is empty or consists only of white-space
    /// characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the text is too long, when the request fails, when Yandex
    /// refuses it or answers with no audio.
    /// </exception>
    public async Task<byte[]> SynthesizeAsync(
        string text,
        string language,
        string voice,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentException.ThrowIfNullOrWhiteSpace(voice);

        List<KeyValuePair<string, string>> fields =
        [
            new("text", text),
            new("lang", language),
            new("voice", voice),
            new("format", "mp3"),
        ];

        if (Credentials.FolderId is not null)
        {
            fields.Add(new("folderId", Credentials.FolderId));
        }

        // The limit of the form is that of its encoded length, which a text of
        // a script that takes several bytes a letter reaches long before the
        // limit of characters.
        string form = await new FormUrlEncodedContent(fields).ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (text.Length > MaxSpeechCharacters || form.Length > MaxSpeechFormLength)
        {
            throw new ProviderException(
                YandexCloudProvider.Name,
                "Yandex Cloud speaks 5000 characters at most in one request, and fewer when the script takes several bytes a letter: the request must be 15 KB once encoded.");
        }

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri(SynthesisUrl))
        {
            Content = new FormUrlEncodedContent(fields),
        };

        byte[] audio = await SendBytesAsync(httpRequest, cancellationToken).ConfigureAwait(false);

        return audio.Length == 0
            ? throw new ProviderException(YandexCloudProvider.Name, "Yandex Cloud returned no audio.")
            : audio;
    }

    /// <summary>
    /// Reads the text on an image.
    /// </summary>
    /// <param name="image">
    /// The bytes of the image.
    /// </param>
    /// <param name="mediaType">
    /// The media type of the image: <c>image/png</c> or <c>image/jpeg</c>.
    /// </param>
    /// <param name="languageCode">
    /// The code of the language the text is written in, or
    /// <see langword="null"/> to let Yandex find it.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The text found, or <see langword="null"/> when there is none.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the image is empty or the media type is empty or consists
    /// only of white-space characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the media type is one Yandex does not read, when the request
    /// fails, when Yandex refuses it, or when the answer cannot be read.
    /// </exception>
    public async Task<YandexCloudTextAnnotation?> RecognizeAsync(
        ReadOnlyMemory<byte> image,
        string mediaType,
        string? languageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);

        if (image.IsEmpty)
        {
            throw new ArgumentException("An image cannot be empty.", nameof(image));
        }

        string normalizedType = mediaType.ToLowerInvariant() switch
        {
            "image/png" => "image/png",
            "image/jpeg" or "image/jpg" => "image/jpeg",
            _ => throw new ProviderException(
                YandexCloudProvider.Name,
                $"Yandex Cloud reads PNG and JPEG images here, not '{mediaType}'."),
        };

        string body = JsonSerializer.Serialize(
            new YandexCloudRecognizeRequest
            {
                Content = Convert.ToBase64String(image.Span),
                MimeType = normalizedType,

                // "*" lets the service find the language itself.
                LanguageCodes = [languageCode ?? "*"],
            },
            YandexCloudJsonContext.Default.YandexCloudRecognizeRequest);

        using HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri(RecognitionUrl))
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };

        // Yandex keeps what it is sent to improve its service unless it is told
        // not to; an image is the caller's, and is not offered.
        httpRequest.Headers.Add("x-data-logging-enabled", "false");

        if (Credentials.FolderId is not null)
        {
            httpRequest.Headers.Add("x-folder-id", Credentials.FolderId);
        }

        byte[] bytes = await SendBytesAsync(httpRequest, cancellationToken).ConfigureAwait(false);

        YandexCloudRecognizeResponse? answer;

        try
        {
            // The answer of a streaming method may arrive as several JSON values
            // one after another; the first carries the page.
            Utf8JsonReader reader = new(bytes, new JsonReaderOptions { AllowMultipleValues = true });

            answer = JsonSerializer.Deserialize(ref reader, YandexCloudJsonContext.Default.YandexCloudRecognizeResponse);
        }
        catch (JsonException exception)
        {
            throw UnreadableAnswer(exception);
        }

        return (answer?.Result ?? answer)?.TextAnnotation;
    }

    private IEnumerable<string[]> Batch(IReadOnlyList<string> pieces)
    {
        List<string> batch = [];
        int length = 0;

        foreach (string piece in pieces)
        {
            if (batch.Count > 0 && length + piece.Length > MaxCharactersPerRequest)
            {
                yield return [.. batch];

                batch.Clear();
                length = 0;
            }

            batch.Add(piece);
            length += piece.Length;
        }

        yield return [.. batch];
    }

    private async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri(url))
        {
            Content = JsonContent.Create(request, requestTypeInfo),
        };

        byte[] bytes = await SendBytesAsync(httpRequest, cancellationToken).ConfigureAwait(false);

        try
        {
            return JsonSerializer.Deserialize(bytes, responseTypeInfo);
        }
        catch (JsonException exception)
        {
            throw UnreadableAnswer(exception);
        }
    }

    private async Task<byte[]> SendBytesAsync(HttpRequestMessage httpRequest, CancellationToken cancellationToken)
    {
        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Api-Key {Credentials.ApiKey}");

        try
        {
            using HttpResponseMessage httpResponse = await HttpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            byte[] bytes = await httpResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

            // A refusal explains itself in its body, so the body is read before
            // the status is reported.
            return httpResponse.IsSuccessStatusCode ? bytes : throw ReadRefusal((int)httpResponse.StatusCode, bytes);
        }
        catch (HttpRequestException exception)
        {
            throw RequestFailed(exception);
        }
    }

    private ProviderException ReadRefusal(int status, byte[] body)
    {
        string? message = null;

        try
        {
            message = JsonSerializer.Deserialize(body, YandexCloudJsonContext.Default.YandexCloudErrorResponse)?.Message;
        }
        catch (JsonException)
        {
            // The body is not the error Yandex usually writes; the status alone
            // has to do.
        }

        return Refused(status, message);
    }
}
