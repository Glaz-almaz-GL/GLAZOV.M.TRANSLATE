using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Scripts;
using GLTranslate.Domain.Linguistics.Scripts.Codes;
using GLTranslate.Providers.Microsoft.Internal;

namespace GLTranslate.Providers.Microsoft;

/// <summary>
/// Renders text in the Latin script with Microsoft Translator.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint transliterates between two named writing systems and does not
/// detect the language, so a request must name the language it is written in.
/// The writing system to read from is the primary writing system of that
/// language, and the writing system to render into is Latin.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class MicrosoftTransliterationProvider : ITransliterationProvider, IDisposable
{
    private const string LatinScriptCode = "Latn";

    private readonly MicrosoftTranslationEngine _engine;

    /// <inheritdoc/>
    public string Name => MicrosoftProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTransliterationProvider"/> class.
    /// </summary>
    public MicrosoftTransliterationProvider()
    {
        _engine = new MicrosoftTranslationEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTransliterationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public MicrosoftTransliterationProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new MicrosoftTranslationEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the request names no language, when the language is unknown
    /// to Microsoft Translator, when the language is already written in the
    /// Latin script, when the request fails, or when the endpoint answers with
    /// something the provider cannot read.
    /// </exception>
    public async Task<TransliterationResult> ExecuteAsync(
        TransliterationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.LanguageId is null)
        {
            // Unlike translation, the endpoint has no detection to fall back on.
            throw new ProviderException(
                MicrosoftProvider.Name,
                "Microsoft Translator cannot detect the language of a transliteration request: name it in the request.");
        }

        string languageCode = MicrosoftLanguageCodeResolver.ToMicrosoftCode(request.LanguageId);
        string fromScript = ResolveSourceScript(request.LanguageId);

        string transliteratedText = await _engine
            .TransliterateAsync(request.Text.Value, languageCode, fromScript, LatinScriptCode, cancellationToken)
            .ConfigureAwait(false);

        return new TransliterationResult(
            request.Id,
            new TransliteratedText(transliteratedText),
            request.LanguageId,
            wasLanguageDetected: false);
    }

    private static string ResolveSourceScript(Abstractions.Linguistics.Languages.LanguageId languageId)
    {
        Language language = LanguageRegistry.Default.Get(languageId);
        Script script = language.Scripts[0];
        string scriptCode = script.Codes.Get<Iso15924Code>().Value;

        if (string.Equals(scriptCode, LatinScriptCode, StringComparison.Ordinal))
        {
            // Rendering Latin in Latin is not a transliteration, and the endpoint
            // answers 400 Bad Request to such a pair.
            throw new ProviderException(
                MicrosoftProvider.Name,
                $"Language '{languageId.Value}' is already written in the Latin script, so there is nothing to transliterate.");
        }

        return scriptCode;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
