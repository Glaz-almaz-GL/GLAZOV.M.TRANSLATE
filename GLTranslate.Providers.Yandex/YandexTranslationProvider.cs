using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Yandex.Internal;

namespace GLTranslate.Providers.Yandex;

/// <summary>
/// Translates text with Yandex Translate.
/// </summary>
/// <remarks>
/// <para>
/// The provider needs no key: it asks the endpoint of the Yandex Translate
/// mobile application, the way that application asks it.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class YandexTranslationProvider : ITextTranslationProvider, IDisposable
{
    private readonly YandexEngine _engine;

    /// <inheritdoc/>
    public string Name => YandexProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexTranslationProvider"/> class.
    /// </summary>
    public YandexTranslationProvider()
    {
        _engine = new YandexEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public YandexTranslationProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate, when
    /// Yandex refuses the direction, when the request fails, or when the
    /// endpoint answers with something the provider cannot read.
    /// </exception>
    public async Task<TextTranslationResult> ExecuteAsync(
        TextTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = YandexLanguageCodeResolver.ToYandexCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : YandexLanguageCodeResolver.ToYandexCode(request.SourceLanguageId);

        (string translatedText, string detectedSourceCode) = await _engine
            .TranslateAsync(request.Text.Value, targetCode, sourceCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = request.SourceLanguageId
            ?? YandexLanguageCodeResolver.FromYandexCode(detectedSourceCode);

        return new TextTranslationResult(
            request.Id,
            new ProviderText(translatedText),
            resolvedSourceLanguageId,
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
