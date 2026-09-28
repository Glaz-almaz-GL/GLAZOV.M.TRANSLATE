using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;
using GLTranslate.Providers.Google.Internal;

namespace GLTranslate.Providers.Google;

/// <summary>
/// Synthesizes speech using the free Google Translate text-to-speech web
/// endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This provider integrates with the same public endpoint used by the
/// <c>translate.google.com</c> web widget rather than the official,
/// authenticated Google Cloud Text-to-Speech API. It requires no API key,
/// but it is undocumented, unsupported by Google, and may change or stop
/// working without notice.
/// </para>
/// <para>
/// Instances of this class are immutable and thread-safe, provided the
/// supplied <see cref="HttpClient"/> is not mutated after construction.
/// </para>
/// </remarks>
public sealed class GoogleTextToSpeechProvider : ITextToSpeechProvider, IDisposable
{
    private static readonly AudioContentType Mp3ContentType = new("audio/mpeg");

    private readonly GoogleTextToSpeechEngine _engine;
    private readonly HttpClient? _ownedHttpClient;

    /// <inheritdoc/>
    public string Name => GoogleProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTextToSpeechProvider"/>
    /// class with an internally managed <see cref="HttpClient"/>.
    /// </summary>
    public GoogleTextToSpeechProvider()
    {
        _ownedHttpClient = new HttpClient();
        _engine = new GoogleTextToSpeechEngine(_ownedHttpClient);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTextToSpeechProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client used to send requests. The caller retains ownership
    /// and is responsible for disposing it.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public GoogleTextToSpeechProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleTextToSpeechEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="request"/> specifies a language unknown
    /// to GLTranslate, or when the underlying request to Google Translate
    /// fails.
    /// </exception>
    public async Task<TextToSpeechResult> ExecuteAsync(TextToSpeechRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.VoiceName is not null)
        {
            // Google speaks one voice per language and offers no choice, so a
            // named voice is refused rather than silently ignored.
            throw new ProviderException(
                GoogleProvider.Name,
                "Google Translate offers no choice of voice: leave the voice of the request unset.");
        }

        string languageCode = GoogleLanguageCodeResolver.ToGoogleCode(request.LanguageId);

        byte[] audioData = await _engine
            .SynthesizeAsync(request.Text.Value, languageCode, cancellationToken)
            .ConfigureAwait(false);

        return new TextToSpeechResult(request.Id, audioData, Mp3ContentType, request.LanguageId);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _ownedHttpClient?.Dispose();
    }
}
