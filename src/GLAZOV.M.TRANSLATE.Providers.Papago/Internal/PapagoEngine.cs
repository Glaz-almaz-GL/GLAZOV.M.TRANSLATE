using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Providers.Common;
using System.Globalization;
using System.Text.Json;

namespace GLAZOV.M.TRANSLATE.Providers.Papago.Internal;

/// <summary>
/// Performs the HTTP call and the response reading required to translate text
/// through the web service of Papago.
/// </summary>
/// <remarks>
/// <para>
/// This is the provider's engine: it contains the business logic of the
/// operation and is not part of the public API. Consumers depend on
/// <see cref="PapagoTranslationProvider"/> or <see cref="PapagoTransliterationProvider"/>
/// instead.
/// </para>
/// <para>
/// The service asks for no key and no signature. It does ask for a device
/// identifier; the engine makes one when it is created and keeps it, so that all
/// of its requests look like one browser.
/// </para>
/// <para>
/// This type is thread-safe as long as the supplied <see cref="HttpClient"/>
/// is.
/// </para>
/// </remarks>
internal sealed class PapagoEngine : ProviderEngine
{
    private const string TranslationPath = "/api/text/translation";

    private readonly PapagoOptions _options;
    private readonly string _deviceId = Guid.NewGuid().ToString();

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoEngine"/> class.
    /// </summary>
    /// <param name="options">The settings of the engine.</param>
    /// <param name="httpClient">
    /// The HTTP client used to send requests. The engine does not own its
    /// lifetime.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="httpClient"/>
    /// is <see langword="null"/>.
    /// </exception>
    public PapagoEngine(PapagoOptions options, HttpClient httpClient)
        : base(PapagoProvider.Name, httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PapagoEngine"/> class with an
    /// <see cref="HttpClient"/> of its own.
    /// </summary>
    /// <param name="options">The settings of the engine.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public PapagoEngine(PapagoOptions options)
        : base(PapagoProvider.Name)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <summary>
    /// Translates text from one language code to another.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="sourceLanguageCode">
    /// The Papago code of the source language, or <c>"auto"</c> to request
    /// automatic detection.
    /// </param>
    /// <param name="targetLanguageCode">The Papago code of the target language.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task that completes with the answer of Papago.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when a parameter is empty or white-space, or when
    /// <paramref name="text"/> is longer than the configured limit.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails, when Papago refuses it, or when the answer
    /// does not have the expected shape.
    /// </exception>
    public async Task<PapagoAnswer> TranslateAsync(
        string text,
        string sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLanguageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLanguageCode);

        if (text.Length > _options.MaxTextLength)
        {
            throw new ArgumentException(
                $"The text is {text.Length.ToString(CultureInfo.InvariantCulture)} characters long; the limit is {_options.MaxTextLength.ToString(CultureInfo.InvariantCulture)}.",
                nameof(text));
        }

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri($"{_options.ServiceUrl}{TranslationPath}"))
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("deviceId", _deviceId),
                new KeyValuePair<string, string>("locale", _options.InterfaceLocale),
                new KeyValuePair<string, string>("dict", "false"),
                new KeyValuePair<string, string>("dictDisplay", "30"),
                new KeyValuePair<string, string>("honorific", "false"),
                new KeyValuePair<string, string>("instant", "false"),
                new KeyValuePair<string, string>("paging", "false"),
                new KeyValuePair<string, string>("source", sourceLanguageCode),
                new KeyValuePair<string, string>("target", targetLanguageCode),
                new KeyValuePair<string, string>("text", text),
            ]),
        };

        request.Headers.TryAddWithoutValidation("Origin", _options.ServiceUrl);
        request.Headers.TryAddWithoutValidation("Referer", $"{_options.ServiceUrl}/");

        string body;

        try
        {
            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw ReadRefusal((int)response.StatusCode, body);
            }
        }
        catch (HttpRequestException exception)
        {
            throw RequestFailed(exception);
        }

        return ReadAnswer(body);
    }

    /// <summary>
    /// Builds the exception for a refused request, taking the reason from the
    /// body when the body is the JSON Papago refuses with.
    /// </summary>
    private ProviderException ReadRefusal(int status, string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);

            string? message = document.RootElement.TryGetProperty("errorMessage", out JsonElement text) && text.ValueKind == JsonValueKind.String
                ? text.GetString()
                : null;

            return Refused(status, message);
        }
        catch (JsonException)
        {
            return Refused(status, null);
        }
    }

    /// <summary>
    /// Reads the translation, the detected language and the transliteration out
    /// of the answer.
    /// </summary>
    private PapagoAnswer ReadAnswer(string body)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);

            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("translatedText", out JsonElement translated) || translated.ValueKind != JsonValueKind.String)
            {
                throw new ProviderException(ProviderName, $"{ProviderName} returned an answer with no translation.");
            }

            string? sourceCode = root.TryGetProperty("srcLangType", out JsonElement source) && source.ValueKind == JsonValueKind.String
                ? source.GetString()
                : null;

            return new PapagoAnswer(
                translated.GetString()!,
                sourceCode is PapagoLanguageCodeResolver.UnknownCode ? null : sourceCode,
                ReadTransliteration(root, "tlitSrc"));
        }
        catch (JsonException exception)
        {
            throw UnreadableAnswer(exception);
        }
    }

    /// <summary>
    /// Joins the transliteration Papago gives word by word, or returns
    /// <see langword="null"/> when it gives none.
    /// </summary>
    private static string? ReadTransliteration(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out JsonElement part)
            || !part.TryGetProperty("message", out JsonElement message)
            || !message.TryGetProperty("tlitResult", out JsonElement words)
            || words.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        List<string> phonemes = [];

        foreach (JsonElement word in words.EnumerateArray())
        {
            if (word.TryGetProperty("phoneme", out JsonElement phoneme) && phoneme.ValueKind == JsonValueKind.String)
            {
                phonemes.Add(phoneme.GetString()!);
            }
        }

        return phonemes.Count == 0 ? null : string.Join(' ', phonemes);
    }
}
