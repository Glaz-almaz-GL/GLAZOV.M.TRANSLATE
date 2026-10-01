using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Internal;

/// <summary>
/// Performs the calls against the official Google Cloud APIs: Translation,
/// Text-to-Speech and Vision.
/// </summary>
/// <remarks>
/// <para>
/// This type is the business logic of the Google Cloud providers and is not
/// part of the public API.
/// </para>
/// <para>
/// Every call carries the API key in the <c>X-Goog-Api-Key</c> header, which
/// Google accepts in place of the <c>key</c> parameter and which keeps the key
/// out of addresses, logs and exception messages. A refusal comes back with
/// its explanation in the body, which is what the exception reports.
/// </para>
/// </remarks>
internal sealed class GoogleCloudEngine : CredentialedEngine<GoogleCloudCredentials>
{
    private const string TranslationUrl = "https://translation.googleapis.com/language/translate/v2";
    private const string SynthesisUrl = "https://texttospeech.googleapis.com/v1/text:synthesize";
    private const string VoicesUrl = "https://texttospeech.googleapis.com/v1/voices";
    private const string VisionUrl = "https://vision.googleapis.com/v1/images:annotate";

    // Translation takes 128 pieces in a request at most.
    private const int MaxPiecesPerRequest = 128;

    // Text-to-Speech takes 5,000 bytes of text in a request at most.
    private const int MaxSpeechBytes = 5000;

    private readonly ConcurrentDictionary<string, string> _voiceLanguages = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudEngine"/> class
    /// with an <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="credentials">
    /// The key the calls are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public GoogleCloudEngine(GoogleCloudCredentials credentials)
        : base(GoogleCloudProvider.Name, credentials)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudEngine"/> class
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
    public GoogleCloudEngine(GoogleCloudCredentials credentials, HttpClient httpClient)
        : base(GoogleCloudProvider.Name, credentials, httpClient)
    {
    }

    /// <summary>
    /// Translates pieces of text or markup.
    /// </summary>
    /// <param name="pieces">
    /// The pieces to translate. More than 128 are sent in several requests.
    /// </param>
    /// <param name="isMarkup">
    /// <see langword="true"/> when the pieces are HTML, whose tags are kept;
    /// <see langword="false"/> when they are plain text.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The code of the language the pieces are written in, or
    /// <see langword="null"/> to let Google detect it.
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
    /// Thrown when a request fails, when Google refuses it, or when the answer
    /// cannot be read.
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

        List<string> translations = new(pieces.Count);
        string? detected = null;

        for (int start = 0; start < pieces.Count; start += MaxPiecesPerRequest)
        {
            string[] batch = [.. pieces.Skip(start).Take(MaxPiecesPerRequest)];

            GoogleCloudTranslateResponse? answer = await PostAsync(
                TranslationUrl,
                new GoogleCloudTranslateRequest
                {
                    Text = batch,
                    Source = sourceLanguageCode,
                    Target = targetLanguageCode,
                    Format = isMarkup ? "html" : "text",
                },
                GoogleCloudJsonContext.Default.GoogleCloudTranslateRequest,
                GoogleCloudJsonContext.Default.GoogleCloudTranslateResponse,
                cancellationToken)
                .ConfigureAwait(false);

            if (answer?.Data?.Translations is not { } batchTranslations
                || batchTranslations.Count != batch.Length
                || batchTranslations.Any(translation => translation.TranslatedText is null))
            {
                // No translation, or not one for every piece sent
                throw new ProviderException(GoogleCloudProvider.Name, "Google Cloud returned no translation.");
            }

            detected ??= batchTranslations[0].DetectedSourceLanguage;

            translations.AddRange(batchTranslations.Select(translation => translation.TranslatedText!));
        }

        return (translations, detected);
    }

    /// <summary>
    /// Speaks a text.
    /// </summary>
    /// <param name="text">
    /// The text to speak.
    /// </param>
    /// <param name="languageCode">
    /// The code of the language to speak, with a region if the caller knows
    /// one, as in <c>en-US</c>; a bare language such as <c>en</c> is given the
    /// region of the first voice Google has for it.
    /// </param>
    /// <param name="voiceName">
    /// The name of the voice, such as <c>en-US-Neural2-A</c>, or
    /// <see langword="null"/> to take Google's default for the language.
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
    /// Thrown when the text is too long, when no voice speaks the language,
    /// when a request fails, when Google refuses it, or when the answer cannot
    /// be read.
    /// </exception>
    public async Task<byte[]> SynthesizeAsync(
        string text,
        string languageCode,
        string? voiceName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        if (Encoding.UTF8.GetByteCount(text) > MaxSpeechBytes)
        {
            throw new ProviderException(
                GoogleCloudProvider.Name,
                $"Google Cloud speaks {MaxSpeechBytes.ToString(CultureInfo.InvariantCulture)} bytes of text at most in one request.");
        }

        string regionalCode = languageCode.Contains('-', StringComparison.Ordinal)
            ? languageCode
            : await FindRegionAsync(languageCode, cancellationToken).ConfigureAwait(false);

        GoogleCloudSynthesizeResponse? answer = await PostAsync(
            SynthesisUrl,
            new GoogleCloudSynthesizeRequest
            {
                Input = new GoogleCloudSynthesisInput { Text = text },
                Voice = new GoogleCloudVoiceSelection { LanguageCode = regionalCode, Name = voiceName },
                AudioConfig = new GoogleCloudAudioConfig { AudioEncoding = "MP3" },
            },
            GoogleCloudJsonContext.Default.GoogleCloudSynthesizeRequest,
            GoogleCloudJsonContext.Default.GoogleCloudSynthesizeResponse,
            cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(answer?.AudioContent))
        {
            throw new ProviderException(GoogleCloudProvider.Name, "Google Cloud returned no audio.");
        }

        try
        {
            return Convert.FromBase64String(answer.AudioContent);
        }
        catch (FormatException exception)
        {
            throw UnreadableAnswer(exception);
        }
    }

    /// <summary>
    /// Reads the text on an image.
    /// </summary>
    /// <param name="image">
    /// The bytes of the image.
    /// </param>
    /// <param name="languageHint">
    /// The code of the language the text is likely written in, or
    /// <see langword="null"/> for none.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The text found, page by page, or <see langword="null"/> when there is
    /// none.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the image is empty.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, when Google refuses it, or when the
    /// answer cannot be read.
    /// </exception>
    public async Task<GoogleCloudTextAnnotation?> RecognizeAsync(
        ReadOnlyMemory<byte> image,
        string? languageHint,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (image.IsEmpty)
        {
            throw new ArgumentException("An image cannot be empty.", nameof(image));
        }

        GoogleCloudAnnotateResponse? answer = await PostAsync(
            VisionUrl,
            new GoogleCloudAnnotateRequest
            {
                Requests =
                [
                    new GoogleCloudAnnotateImageRequest
                    {
                        Image = new GoogleCloudVisionImage { Content = Convert.ToBase64String(image.Span) },
                        Features = [new GoogleCloudVisionFeature { Type = "DOCUMENT_TEXT_DETECTION" }],
                        ImageContext = languageHint is null
                            ? null
                            : new GoogleCloudVisionContext { LanguageHints = [languageHint] },
                    },
                ],
            },
            GoogleCloudJsonContext.Default.GoogleCloudAnnotateRequest,
            GoogleCloudJsonContext.Default.GoogleCloudAnnotateResponse,
            cancellationToken)
            .ConfigureAwait(false);

        if (answer?.Responses is not [{ } response])
        {
            // One image was sent, so one answer is due.
            throw new ProviderException(GoogleCloudProvider.Name, "Google Cloud returned no answer about the image.");
        }

        if (response.Error is { } error)
        {
            throw Refused(error.Code, error.Message);
        }

        return response.FullText;
    }

    private async Task<string> FindRegionAsync(string languageCode, CancellationToken cancellationToken)
    {
        if (_voiceLanguages.TryGetValue(languageCode, out string? known))
        {
            return known;
        }

        using HttpRequestMessage httpRequest = new(
            HttpMethod.Get,
            new Uri($"{VoicesUrl}?languageCode={Uri.EscapeDataString(languageCode)}"));

        GoogleCloudVoicesResponse? answer = await SendAsync(
            httpRequest,
            GoogleCloudJsonContext.Default.GoogleCloudVoicesResponse,
            cancellationToken)
            .ConfigureAwait(false);

        string? regional = answer?.Voices?
            .SelectMany(voice => voice.LanguageCodes ?? [])
            .FirstOrDefault(code => code.Contains('-', StringComparison.Ordinal));

        if (regional is null)
        {
            throw new ProviderException(
                GoogleCloudProvider.Name,
                $"Google Cloud has no voice that speaks '{languageCode}'.");
        }

        return _voiceLanguages.GetOrAdd(languageCode, regional);
    }

    private Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri(url))
        {
            Content = JsonContent.Create(request, requestTypeInfo),
        };

        return SendOwnedAsync(httpRequest, responseTypeInfo, cancellationToken);
    }

    private async Task<TResponse?> SendOwnedAsync<TResponse>(
        HttpRequestMessage httpRequest,
        JsonTypeInfo<TResponse> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        using (httpRequest)
        {
            return await SendAsync(httpRequest, responseTypeInfo, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<TResponse?> SendAsync<TResponse>(
        HttpRequestMessage httpRequest,
        JsonTypeInfo<TResponse> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        httpRequest.Headers.Add("X-Goog-Api-Key", Credentials.ApiKey);

        try
        {
            using HttpResponseMessage httpResponse = await HttpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                // A refusal explains itself in its body, so the body is read
                // before the status is reported.
                throw await ReadRefusalAsync(httpResponse, cancellationToken).ConfigureAwait(false);
            }

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

    private async Task<ProviderException> ReadRefusalAsync(
        HttpResponseMessage httpResponse,
        CancellationToken cancellationToken)
    {
        string? message = null;

        try
        {
            GoogleCloudErrorResponse? body = await httpResponse.Content
                .ReadFromJsonAsync(GoogleCloudJsonContext.Default.GoogleCloudErrorResponse, cancellationToken)
                .ConfigureAwait(false);

            message = body?.Error?.Message;
        }
        catch (JsonException)
        {
            // The body is not the error Google usually writes; the status alone
            // has to do.
        }

        return Refused((int)httpResponse.StatusCode, message);
    }
}
