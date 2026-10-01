using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GLAZOV.M.TRANSLATE.Providers.Baidu.Internal;

/// <summary>
/// Performs the calls against the Baidu Translate open platform.
/// </summary>
/// <remarks>
/// <para>
/// This type is the business logic of the Baidu providers and is not part of
/// the public API.
/// </para>
/// <para>
/// Every call is signed with the secret key of the application. The text and
/// the markup go to two endpoints that answer alike; the image goes to a
/// third, which wants the bytes as a file of a form and the rest in the query.
/// </para>
/// </remarks>
internal sealed class BaiduEngine : BaiduEngineBase
{
    private const string TextUrl = "https://fanyi-api.baidu.com/api/trans/vip/translate";

    // The endpoint that keeps the tags of the markup around the translated words.
    private const string MarkupUrl = "https://fanyi-api.baidu.com/ait/api/aiTextTranslate";

    private const string PictureUrl = "https://fanyi-api.baidu.com/api/trans/sdk/picture";

    // The image endpoint takes these three as they are: the platform's
    // documentation fixes their values, and they identify no device.
    private const string DeviceId = "APICUID";
    private const string Mac = "mac";
    private const string PictureVersion = "3";

    private const string AutoLanguage = "auto";

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduEngine"/> class with
    /// an <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="credentials">
    /// The application the calls are made for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public BaiduEngine(BaiduCredentials credentials)
        : base(credentials)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduEngine"/> class with
    /// the specified <see cref="HttpClient"/>.
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
    public BaiduEngine(BaiduCredentials credentials, HttpClient httpClient)
        : base(credentials, httpClient)
    {
    }

    /// <summary>
    /// Translates plain text.
    /// </summary>
    /// <param name="text">
    /// The text to translate. Its lines are translated together and come back
    /// joined by line breaks.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The Baidu code of the language the text is written in, or
    /// <see langword="null"/> to let Baidu detect it.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The Baidu code of the language to translate into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The translation and the code of the language it was translated from.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an argument is empty or consists only of white-space
    /// characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, when Baidu refuses it, or when the
    /// answer cannot be read.
    /// </exception>
    public Task<(string Translation, string SourceLanguageCode)> TranslateAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        return TranslateAsync(TextUrl, [], text, sourceLanguageCode, targetLanguageCode, cancellationToken);
    }

    /// <summary>
    /// Translates markup, keeping its tags.
    /// </summary>
    /// <param name="markup">
    /// The markup to translate.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The Baidu code of the language the markup is written in, or
    /// <see langword="null"/> to let Baidu detect it.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The Baidu code of the language to translate into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The translated markup and the code of the language it was translated
    /// from.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an argument is empty or consists only of white-space
    /// characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, when Baidu refuses it, or when the
    /// answer cannot be read.
    /// </exception>
    public Task<(string Translation, string SourceLanguageCode)> TranslateMarkupAsync(
        string markup,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        // Tags are kept by the machine translation model only.
        return TranslateAsync(
            MarkupUrl,
            [new("model_type", "nmt"), new("tag_handling", "1")],
            markup,
            sourceLanguageCode,
            targetLanguageCode,
            cancellationToken);
    }

    /// <summary>
    /// Reads the text on an image and translates it.
    /// </summary>
    /// <param name="image">
    /// The bytes of the image.
    /// </param>
    /// <param name="mediaType">
    /// The media type of the image: <c>image/png</c> or <c>image/jpeg</c>.
    /// </param>
    /// <param name="sourceLanguageCode">
    /// The Baidu code of the language the text is written in, or
    /// <see langword="null"/> to let Baidu detect it.
    /// </param>
    /// <param name="targetLanguageCode">
    /// The Baidu code of the language to translate into.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// What Baidu read and how it translated it.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the image is empty or an argument is empty or consists only
    /// of white-space characters.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the media type is one Baidu does not read, when the request
    /// fails, when Baidu refuses it, or when the answer cannot be read.
    /// </exception>
    public async Task<BaiduPictureData> TranslatePictureAsync(
        ReadOnlyMemory<byte> image,
        string mediaType,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        if (image.IsEmpty)
        {
            throw new ArgumentException("An image cannot be empty.", nameof(image));
        }

        // Baidu tells the format by the extension of the file name, in lowercase,
        // and reads png and jpeg only.
        string fileName = mediaType.ToLowerInvariant() switch
        {
            "image/png" => "image.png",
            "image/jpeg" or "image/jpg" => "image.jpg",
            _ => throw new ProviderException(
                BaiduProvider.Name,
                $"Baidu reads only PNG and JPEG images, not '{mediaType}'."),
        };

        string salt = NewSalt();
        string sign = BaiduSignature.ForImage(
            Credentials.AppId,
            image.Span,
            salt,
            DeviceId,
            Mac,
            Credentials.SecretKey);

        string url = $"{PictureUrl}?from={Uri.EscapeDataString(sourceLanguageCode ?? AutoLanguage)}" +
                     $"&to={Uri.EscapeDataString(targetLanguageCode)}" +
                     $"&appid={Uri.EscapeDataString(Credentials.AppId)}" +
                     $"&salt={salt}&cuid={DeviceId}&mac={Mac}&version={PictureVersion}&paste=0&sign={sign}";

        using MultipartFormDataContent content = [];
        using ByteArrayContent file = new(image.ToArray());

        file.Headers.ContentType = new MediaTypeHeaderValue(fileName.EndsWith(".png", StringComparison.Ordinal)
            ? "image/png"
            : "image/jpeg");
        content.Add(file, "image", fileName);

        BaiduPictureResponse? answer = await SendAsync(
            new HttpRequestMessage(HttpMethod.Post, new Uri(url)) { Content = content },
            BaiduJsonContext.Default.BaiduPictureResponse,
            cancellationToken)
            .ConfigureAwait(false);

        if (answer is null)
        {
            // Empty response
            throw new ProviderException(BaiduProvider.Name, "Baidu returned an empty response.");
        }

        EnsureAnswer(answer.ErrorCode, answer.ErrorMessage);

        return answer.Data ?? new BaiduPictureData();
    }

    private async Task<(string Translation, string SourceLanguageCode)> TranslateAsync(
        string url,
        KeyValuePair<string, string>[] extraFields,
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        string salt = NewSalt();

        // The signature is made over the text as it is, before the form encodes
        // it: signing the encoded text is the mistake the platform warns about.
        string sign = BaiduSignature.ForText(Credentials.AppId, text, salt, Credentials.SecretKey);

        List<KeyValuePair<string, string>> fields =
        [
            new("q", text),
            new("from", sourceLanguageCode ?? AutoLanguage),
            new("to", targetLanguageCode),
            new("appid", Credentials.AppId),
            new("salt", salt),
            new("sign", sign),
            .. extraFields,
        ];

        BaiduTextResponse? answer = await SendAsync(
            new HttpRequestMessage(HttpMethod.Post, new Uri(url)) { Content = new FormUrlEncodedContent(fields) },
            BaiduJsonContext.Default.BaiduTextResponse,
            cancellationToken)
            .ConfigureAwait(false);

        if (answer is null)
        {
            throw new ProviderException(BaiduProvider.Name, "Baidu returned an empty response.");
        }

        EnsureAnswer(answer.ErrorCode, answer.ErrorMessage);

        if (answer.Lines is not { Count: > 0 } lines || lines.Any(line => line.Destination is null))
        {
            // No translation
            throw new ProviderException(BaiduProvider.Name, "Baidu returned no translation.");
        }

        // Baidu answers a text of several lines with a piece for each of them.
        string translation = string.Join('\n', lines.Select(line => line.Destination));

        string resolvedSourceCode = sourceLanguageCode ?? answer.From ?? string.Empty;

        return string.IsNullOrWhiteSpace(resolvedSourceCode)
            ? throw new ProviderException(
                BaiduProvider.Name,
                "Baidu named no source language, although none was given.")
            : (translation, resolvedSourceCode);
    }

    private async Task<TResponse?> SendAsync<TResponse>(
        HttpRequestMessage httpRequest,
        JsonTypeInfo<TResponse> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        using (httpRequest)
        {
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

    private void EnsureAnswer(int errorCode, string? errorMessage)
    {
        // Baidu reports 0 for a success on the image endpoint and 52000 on the
        // platform's newer text ones; a text answer without a code succeeded too.
        Ensure(errorCode == 52000 ? 0 : errorCode, errorMessage);
    }

    private static string NewSalt()
    {
        return Guid.NewGuid().ToString("N");
    }
}
