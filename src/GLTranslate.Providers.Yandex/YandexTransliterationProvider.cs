using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using GLTranslate.Providers.Yandex.Internal;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.Yandex;

/// <summary>
/// Renders text in the Latin script with Yandex Translate.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint transliterates from the writing system of one language into
/// that of another and needs to be told the first, so a request that names no
/// language is answered by asking the endpoint to detect it.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class YandexTransliterationProvider : TransliterationProviderBase, IDisposable
{
    // The language whose writing system the text is rendered into: the Latin
    // script, which is what a transliteration is asked for.
    private const string LatinLanguageCode = "en";

    private readonly YandexEngine _engine;

    /// <inheritdoc/>
    public override string Name => YandexProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexTransliterationProvider"/> class.
    /// </summary>
    public YandexTransliterationProvider()
        : base(YandexLanguageCodeResolver.Instance)
    {
        _engine = new YandexEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexTransliterationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public YandexTransliterationProvider(HttpClient httpClient)
        : base(YandexLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTransliteration> TransliterateAsync(
        string text,
        LanguageId? languageId,
        string? languageCode,
        CancellationToken cancellationToken)
    {
        // The transliteration endpoint has to be told the language, so a request
        // that names none has it detected first.
        string sourceCode = languageCode
            ?? await _engine.DetectLanguageAsync(text, cancellationToken).ConfigureAwait(false);

        string transliteratedText = await _engine
            .TransliterateAsync(text, sourceCode, LatinLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTransliteration(transliteratedText, sourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
