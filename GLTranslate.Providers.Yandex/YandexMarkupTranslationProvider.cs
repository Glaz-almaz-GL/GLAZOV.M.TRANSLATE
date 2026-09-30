using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Yandex.Internal;

namespace GLTranslate.Providers.Yandex;

/// <summary>
/// Translates markup with Yandex Translate, leaving its tags where they are.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint is the same one <see cref="YandexTranslationProvider"/> asks,
/// with <c>format=html</c> in place of <c>format=text</c>: it translates what
/// stands between the tags and returns the markup otherwise unchanged.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class YandexMarkupTranslationProvider : IMarkupTranslationProvider, IDisposable
{
    private readonly YandexEngine _engine;

    /// <inheritdoc/>
    public string Name => YandexProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexMarkupTranslationProvider"/> class.
    /// </summary>
    public YandexMarkupTranslationProvider()
    {
        _engine = new YandexEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexMarkupTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public YandexMarkupTranslationProvider(HttpClient httpClient)
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
    public async Task<MarkupTranslationResult> ExecuteAsync(
        MarkupTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = YandexLanguageCodeResolver.ToYandexCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : YandexLanguageCodeResolver.ToYandexCode(request.SourceLanguageId);

        (string translatedMarkup, string detectedSourceCode) = await _engine
            .TranslateMarkupAsync(request.Markup.Value, targetCode, sourceCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = request.SourceLanguageId
            ?? YandexLanguageCodeResolver.FromYandexCode(detectedSourceCode);

        return new MarkupTranslationResult(
            request.Id,
            new ProviderMarkup(translatedMarkup),
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
