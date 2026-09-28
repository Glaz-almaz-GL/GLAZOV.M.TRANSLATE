using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.TextToSpeech;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Languages.Codes;
using GLTranslate.Providers.Microsoft.Internal;

namespace GLTranslate.Providers.Microsoft;

/// <summary>
/// Speaks text with Microsoft Translator.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint speaks a named voice rather than a language, so the language
/// of a request is turned into the default voice of that language. A language
/// the endpoint has no voice for cannot be spoken.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class MicrosoftTextToSpeechProvider : ITextToSpeechProvider, IDisposable
{
    private static readonly AudioContentType Mp3ContentType = new("audio/mpeg");

    private readonly MicrosoftTextToSpeechEngine _engine;

    /// <inheritdoc/>
    public string Name => MicrosoftProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTextToSpeechProvider"/> class.
    /// </summary>
    public MicrosoftTextToSpeechProvider()
    {
        _engine = new MicrosoftTextToSpeechEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTextToSpeechProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public MicrosoftTextToSpeechProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new MicrosoftTextToSpeechEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the language is unknown to GLTranslate, when the endpoint
    /// has no voice for it, when the request fails, or when the endpoint
    /// answers with something the provider cannot read.
    /// </exception>
    public async Task<TextToSpeechResult> ExecuteAsync(
        TextToSpeechRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A named voice is passed through as it is: only the endpoint knows
        // every voice it has, and it refuses a name it does not know.
        string voiceName = request.VoiceName?.Value ?? ResolveDefaultVoice(request.LanguageId);

        byte[] audio = await _engine
            .SynthesizeAsync(request.Text.Value, voiceName, cancellationToken)
            .ConfigureAwait(false);

        return new TextToSpeechResult(request.Id, audio, Mp3ContentType, request.LanguageId);
    }

    private static string ResolveDefaultVoice(Abstractions.Linguistics.Languages.LanguageId languageId)
    {
        Language language;

        try
        {
            language = LanguageRegistry.Default.Get(languageId);
        }
        catch (KeyNotFoundException exception)
        {
            throw new ProviderException(
                MicrosoftProvider.Name,
                $"Language '{languageId.Value}' is not known to GLTranslate.",
                exception);
        }

        if (!language.Codes.TryGetValue(out Iso6391Code? code))
        {
            throw new ProviderException(
                MicrosoftProvider.Name,
                $"Language '{languageId.Value}' has no ISO 639-1 code, which Microsoft Translator requires.");
        }

        if (!MicrosoftVoices.TryGetDefault(code.Value, out string voiceName))
        {
            throw new ProviderException(
                MicrosoftProvider.Name,
                $"Microsoft Translator has no voice for language '{languageId.Value}'.");
        }

        return voiceName;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
