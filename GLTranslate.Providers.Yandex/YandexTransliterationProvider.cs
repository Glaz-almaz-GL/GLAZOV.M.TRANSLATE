using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using GLTranslate.Providers.Yandex.Internal;

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
public sealed class YandexTransliterationProvider : ITransliterationProvider, IDisposable
{
    // The language whose writing system the text is rendered into: the Latin
    // script, which is what a transliteration is asked for.
    private const string LatinLanguageCode = "en";

    private readonly YandexEngine _engine;

    /// <inheritdoc/>
    public string Name => YandexProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexTransliterationProvider"/> class.
    /// </summary>
    public YandexTransliterationProvider()
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
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when the language is unknown to GLTranslate, when Yandex
    /// refuses the pair, when the request fails, or when the endpoint answers
    /// with something the provider cannot read.
    /// </exception>
    public async Task<TransliterationResult> ExecuteAsync(
        TransliterationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        bool wasLanguageDetected = request.LanguageId is null;

        string sourceCode = request.LanguageId is null
            ? await _engine.DetectLanguageAsync(request.Text.Value, cancellationToken).ConfigureAwait(false)
            : YandexLanguageCodeResolver.ToYandexCode(request.LanguageId);

        string transliteratedText = await _engine
            .TransliterateAsync(request.Text.Value, sourceCode, LatinLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId languageId = request.LanguageId ?? YandexLanguageCodeResolver.FromYandexCode(sourceCode);

        return new TransliterationResult(
            request.Id,
            new TransliteratedText(transliteratedText),
            languageId,
            wasLanguageDetected);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
