using GLTranslate.Abstractions.Providers;
using GLTranslate.Providers.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace GLTranslate.Providers.Microsoft.Internal;

/// <summary>
/// Performs the speech calls against the Microsoft speech endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This type is the business logic of the Microsoft text-to-speech provider
/// and is not part of the public API.
/// </para>
/// <para>
/// The speech endpoint accepts a bearer token rather than the signature the
/// translation endpoint takes. The token is issued by a separate endpoint,
/// which the signature does open, and is fetched again when it expires.
/// </para>
/// </remarks>
internal sealed class MicrosoftTextToSpeechEngine : ProviderEngine
{
    private const string TokenHost = "dev.microsofttranslator.com";

    private const string TokenPath = "apps/endpoint?api-version=1.0";

    private const string OutputFormat = "audio-16khz-32kbitrate-mono-mp3";

    private const string UserAgent = "GLTranslate";

    // The endpoint refuses a longer text with 400 Bad Request.
    private const int MaxTextLength = 1000;

    // A token lives thirty minutes. It is dropped a little earlier so that a
    // request never travels with one that expires on the way.
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(25);

    // Encodes only what SSML requires, leaving every other character as it is.
    private static readonly HtmlEncoder SsmlEncoder = HtmlEncoder.Create(UnicodeRanges.All);

    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _token;
    private string? _region;
    private DateTimeOffset _tokenExpiresAt;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTextToSpeechEngine"/>
    /// class with an <see cref="HttpClient"/> of its own.
    /// </summary>
    public MicrosoftTextToSpeechEngine()
        : base(MicrosoftProvider.Name)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTextToSpeechEngine"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public MicrosoftTextToSpeechEngine(HttpClient httpClient)
        : base(MicrosoftProvider.Name, httpClient)
    {
    }

    /// <summary>
    /// Speaks the specified text with the specified voice.
    /// </summary>
    /// <param name="text">
    /// The text to speak.
    /// </param>
    /// <param name="voiceName">
    /// The name of the voice to speak it with.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// The spoken text as MP3 audio.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text"/> is empty, consists only of
    /// white-space characters, or is longer than the endpoint accepts.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the engine has been disposed.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request fails or the endpoint answers with something
    /// this engine cannot read.
    /// </exception>
    public async Task<byte[]> SynthesizeAsync(
        string text,
        string voiceName,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (text.Length > MaxTextLength)
        {
            throw new ArgumentException(
                $"Microsoft Translator accepts at most {MaxTextLength} characters per request.",
                nameof(text));
        }

        (string token, string region) = await GetTokenAsync(cancellationToken).ConfigureAwait(false);

        string ssml = BuildSsml(text, voiceName);

        using HttpRequestMessage httpRequest = new(
            HttpMethod.Post,
            new Uri($"https://{region}.tts.speech.microsoft.com/cognitiveservices/v1"))
        {
            Content = new StringContent(ssml, Encoding.UTF8, "application/ssml+xml"),
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Headers.Add("X-Microsoft-OutputFormat", OutputFormat);

        // The speech endpoint answers 400 Bad Request, with an empty body, to a
        // request that carries no User-Agent, whatever else it carries. Any value
        // satisfies it; this one says who is calling.
        httpRequest.Headers.Add("User-Agent", UserAgent);

        try
        {
            using HttpResponseMessage httpResponse = await HttpClient
                .SendAsync(httpRequest, cancellationToken)
                .ConfigureAwait(false);

            httpResponse.EnsureSuccessStatusCode();

            byte[] audio = await httpResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

            if (audio.Length == 0)
            {
                throw new ProviderException(MicrosoftProvider.Name, "Microsoft Translator returned no audio.");
            }

            return audio;
        }
        catch (HttpRequestException exception)
        {
            throw RequestFailed(exception);
        }
    }

    private static string BuildSsml(string text, string voiceName)
    {
        const char Quote = '\'';

        // The endpoint needs the name of the voice; the locale it belongs to is
        // the beginning of that name, and the gender it infers from the voice.
        string locale = MicrosoftVoices.GetLocale(voiceName);

        return $"<speak version={Quote}1.0{Quote} xml:lang={Quote}{locale}{Quote}>" +
               $"<voice name={Quote}{voiceName}{Quote}>" +
               SsmlEncoder.Encode(text) +
               "</voice></speak>";
    }

    private async Task<(string Token, string Region)> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_token is not null && _region is not null && DateTimeOffset.UtcNow < _tokenExpiresAt)
        {
            return (_token, _region);
        }

        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_token is not null && _region is not null && DateTimeOffset.UtcNow < _tokenExpiresAt)
            {
                return (_token, _region);
            }

            string url = $"{TokenHost}/{TokenPath}";

            using HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri($"https://{url}"));

            httpRequest.Headers.Add("X-ClientVersion", "N/A");
            httpRequest.Headers.Add("X-MT-Signature", MicrosoftSignature.Create(url));
            httpRequest.Headers.Add("X-UserId", "0");

            MicrosoftSpeechToken? token;

            try
            {
                using HttpResponseMessage httpResponse = await HttpClient
                    .SendAsync(httpRequest, cancellationToken)
                    .ConfigureAwait(false);

                httpResponse.EnsureSuccessStatusCode();

                token = await httpResponse.Content
                    .ReadFromJsonAsync(MicrosoftTranslationJsonContext.Default.MicrosoftSpeechToken, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException exception)
            {
                throw new ProviderException(MicrosoftProvider.Name, "The request for a Microsoft speech token failed.", exception);
            }
            catch (JsonException exception)
            {
                throw UnreadableAnswer(exception);
            }

            if (token is not { Token: { } value, Region: { } region })
            {
                throw new ProviderException(MicrosoftProvider.Name, "Microsoft Translator returned no speech token.");
            }

            _token = value;
            _region = region;
            _tokenExpiresAt = DateTimeOffset.UtcNow + TokenLifetime;

            return (value, region);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tokenLock.Dispose();
        }

        base.Dispose(disposing);
    }
}
