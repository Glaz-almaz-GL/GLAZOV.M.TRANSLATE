using GLAZOV.M.TRANSLATE.Abstractions.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Abstractions.Providers;
using GLAZOV.M.TRANSLATE.Abstractions.TextToSpeech;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Languages;
using GLAZOV.M.TRANSLATE.Domain.Linguistics.Languages.Codes;
using GLAZOV.M.TRANSLATE.Providers.Google.Internal;

namespace GLAZOV.M.TRANSLATE.Providers.Google;

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
    private readonly GoogleTextToSpeechEngine _engine;

    /// <inheritdoc/>
    public string Name => GoogleProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTextToSpeechProvider"/>
    /// class with an internally managed <see cref="HttpClient"/>.
    /// </summary>
    public GoogleTextToSpeechProvider()
    {
        _engine = new GoogleTextToSpeechEngine();
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
    /// Thrown when <paramref name="request"/> names a voice, when it specifies
    /// a language unknown to GLAZOV.M.TRANSLATE or one Google Translate cannot speak,
    /// or when the underlying request to Google Translate fails.
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

        EnsureSpoken(request.LanguageId);

        string languageCode = GoogleLanguageCodeResolver.ToGoogleCode(request.LanguageId);

        byte[] audioData = await _engine
            .SynthesizeAsync(request.Text.Value, languageCode, cancellationToken)
            .ConfigureAwait(false);

        return new TextToSpeechResult(request.Id, audioData, AudioContentType.Mp3, request.LanguageId);
    }

    private static void EnsureSpoken(LanguageId languageId)
    {
        Language language;

        try
        {
            language = LanguageRegistry.Default.Get(languageId);
        }
        catch (KeyNotFoundException exception)
        {
            throw new ProviderException(
                GoogleProvider.Name,
                $"Language '{languageId.Value}' is not known to GLAZOV.M.TRANSLATE.",
                exception);
        }

        // The endpoint answers 400 Bad Request to a language it cannot speak,
        // which reaches the caller as a failed request and explains nothing.
        if (!language.Codes.TryGetValue(out Iso6391Code? code) || !GoogleSpeechLanguages.Contains(code.Value))
        {
            throw new ProviderException(
                GoogleProvider.Name,
                $"Google Translate cannot speak language '{languageId.Value}'.");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
