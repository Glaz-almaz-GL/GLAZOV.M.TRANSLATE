using GLTranslate.Abstractions.Providers;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace GLTranslate.Providers.Bing.Internal;

/// <summary>
/// Performs the calls against the Bing Translator endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This type is the business logic of the Bing providers and is not part of
/// the public API.
/// </para>
/// <para>
/// The endpoint needs no key. It needs the credentials the translator page
/// hands to the script running on it, so they are read from that page and
/// kept until they expire.
/// </para>
/// </remarks>
internal sealed class BingEngine : IDisposable
{
    private const string HostUrl = "https://www.bing.com";

    private const string PageUrl = $"{HostUrl}/translator";

    // The identifier of the translator page, which the endpoint expects to see.
    private const string ImpressionId = "translator.5024.1";

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36";

    // The page hands the credentials to its own script in this array.
    private const string CredentialsMarker = "var params_AbusePreventionHelper = [";

    // The endpoint refuses a longer text.
    private const int MaxTextLength = 1000;

    private readonly HttpClient _httpClient;
    private readonly bool _isExternalHttpClient;
    private readonly SemaphoreSlim _credentialsLock = new(1, 1);

    private BingCredentials? _credentials;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BingEngine"/> class with an
    /// <see cref="HttpClient"/> of its own.
    /// </summary>
    public BingEngine()
        : this(new HttpClient(), isExternal: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BingEngine"/> class with the
    /// specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public BingEngine(HttpClient httpClient)
        : this(httpClient ?? throw new ArgumentNullException(nameof(httpClient)), isExternal: true)
    {
    }

    private BingEngine(HttpClient httpClient, bool isExternal)
    {
        _httpClient = httpClient;
        _isExternalHttpClient = isExternal;

        if (httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            // The page hands its credentials to a browser, so the caller has to
            // look like one.
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
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
    /// The translated text, the code of the language it was translated from,
    /// and the source text in the Latin script when the endpoint rendered it.
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
    /// Thrown when the request fails, when the endpoint refuses it, or when it
    /// answers with something this engine cannot read.
    /// </exception>
    public async Task<BingAnswer> TranslateAsync(
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
                $"Bing Translator accepts at most {MaxTextLength} characters per request.",
                nameof(text));
        }

        BingCredentials credentials = await GetCredentialsAsync(cancellationToken).ConfigureAwait(false);

        using FormUrlEncodedContent content = new(
        [
            new("fromLang", sourceLanguageCode ?? "auto-detect"),
            new("text", text),
            new("to", targetLanguageCode),
            new("token", credentials.Token),
            new("key", credentials.Key.ToString(CultureInfo.InvariantCulture)),
        ]);

        // isVertical=1 is what the page sends, and it is what raises the limit
        // from 500 characters to 1000.
        Uri uri = new($"{HostUrl}/ttranslatev3?isVertical=1&IG={credentials.ImpressionGuid:N}&IID={ImpressionId}");

        BingTranslationResponse[]? answers;

        try
        {
            using HttpResponseMessage httpResponse = await _httpClient
                .PostAsync(uri, content, cancellationToken)
                .ConfigureAwait(false);

            httpResponse.EnsureSuccessStatusCode();

            answers = await httpResponse.Content
                .ReadFromJsonAsync(BingTranslationJsonContext.Default.BingTranslationResponseArray, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            throw new ProviderException(BingProvider.Name, "The request to Bing Translator failed.", exception);
        }
        catch (JsonException exception)
        {
            // The endpoint answers 200 with an object rather than an array when
            // it refuses the credentials, which is how a stale token shows up.
            throw new ProviderException(BingProvider.Name, "Bing Translator returned an unexpected response format.", exception);
        }

        return Read(answers, sourceLanguageCode);
    }

    private static BingAnswer Read(BingTranslationResponse[]? answers, string? sourceLanguageCode)
    {
        if (answers is not [{ } first, ..])
        {
            throw new ProviderException(BingProvider.Name, "Bing Translator returned an empty response.");
        }

        if (first.StatusCode is { } statusCode and not 200)
        {
            throw new ProviderException(
                BingProvider.Name,
                string.IsNullOrWhiteSpace(first.ErrorMessage)
                    ? $"Bing Translator refused the request with code {statusCode}."
                    : $"Bing Translator refused the request: {first.ErrorMessage}");
        }

        if (first.Translations is not [{ Text: { } translatedText }, ..])
        {
            throw new ProviderException(BingProvider.Name, "Bing Translator returned no translation.");
        }

        string resolvedSourceLanguageCode = sourceLanguageCode
            ?? first.DetectedLanguage?.Language
            ?? throw new ProviderException(
                BingProvider.Name,
                "Bing Translator detected no source language, although none was given.");

        // The second element appears only when the source text is written in a
        // script the endpoint can romanize.
        string? inputTransliteration = answers.Length > 1 ? answers[1].InputTransliteration : null;

        return new BingAnswer(translatedText, resolvedSourceLanguageCode, inputTransliteration);
    }

    private async Task<BingCredentials> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        if (_credentials is { } cached && !cached.IsExpired)
        {
            return cached;
        }

        await _credentialsLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_credentials is { } current && !current.IsExpired)
            {
                return current;
            }

            string page;

            try
            {
                page = await _httpClient.GetStringAsync(new Uri(PageUrl), cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException exception)
            {
                throw new ProviderException(BingProvider.Name, "The request for the Bing translator page failed.", exception);
            }

            BingCredentials credentials = ReadCredentials(page);

            _credentials = credentials;

            return credentials;
        }
        finally
        {
            _credentialsLock.Release();
        }
    }

    private static BingCredentials ReadCredentials(string page)
    {
        int start = page.IndexOf(CredentialsMarker, StringComparison.Ordinal);

        if (start < 0)
        {
            throw new ProviderException(BingProvider.Name, "The Bing translator page carried no credentials.");
        }

        int open = start + CredentialsMarker.Length;
        int close = page.IndexOf(']', open);

        if (close < 0)
        {
            throw new ProviderException(BingProvider.Name, "The Bing translator page carried no credentials.");
        }

        // key,"token",lifetime - the key is the moment the page was built, in
        // milliseconds, and the lifetime says how long the pair is good for.
        string[] parts = page[open..close].Split(',');

        if (parts.Length < 3
            || !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long key)
            || !long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long lifetime))
        {
            throw new ProviderException(BingProvider.Name, "The Bing translator page carried credentials this provider cannot read.");
        }

        return new BingCredentials(
            parts[1].Trim().Trim('"'),
            key,
            Guid.NewGuid(),
            DateTimeOffset.FromUnixTimeMilliseconds(key + lifetime));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _credentialsLock.Dispose();

        if (!_isExternalHttpClient)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
    }
}
