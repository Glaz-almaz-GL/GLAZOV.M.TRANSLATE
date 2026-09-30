using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using GLTranslate.Domain.Linguistics.Languages;
using GLTranslate.Domain.Linguistics.Scripts;
using GLTranslate.Domain.Linguistics.Scripts.Codes;
using GLTranslate.Providers.Microsoft.Internal;
using GLTranslate.Providers.Common;

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
public sealed class MicrosoftTransliterationProvider : TransliterationProviderBase, IDisposable
{
    private const string LatinScriptCode = "Latn";

    private readonly MicrosoftTranslationEngine _engine;

    /// <inheritdoc/>
    public override string Name => MicrosoftProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftTransliterationProvider"/> class.
    /// </summary>
    public MicrosoftTransliterationProvider()
        : base(MicrosoftLanguageCodeResolver.Instance)
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
        : base(MicrosoftLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new MicrosoftTranslationEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTransliteration> TransliterateAsync(
        string text,
        LanguageId? languageId,
        string? languageCode,
        CancellationToken cancellationToken)
    {
        if (languageId is null || languageCode is null)
        {
            // Unlike translation, the endpoint has no detection to fall back on.
            throw new ProviderException(
                MicrosoftProvider.Name,
                "Microsoft Translator cannot detect the language of a transliteration request: name it in the request.");
        }

        string fromScript = ResolveSourceScript(languageId);

        string transliteratedText = await _engine
            .TransliterateAsync(text, languageCode, fromScript, LatinScriptCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTransliteration(transliteratedText, null);
    }

    private static string ResolveSourceScript(LanguageId languageId)
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
