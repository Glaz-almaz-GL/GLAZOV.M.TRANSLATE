using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Yandex.Internal;
using GLTranslate.Providers.Common;

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
public sealed class YandexMarkupTranslationProvider : MarkupTranslationProviderBase, IDisposable
{
    private readonly YandexEngine _engine;

    /// <inheritdoc/>
    public override string Name => YandexProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexMarkupTranslationProvider"/> class.
    /// </summary>
    public YandexMarkupTranslationProvider()
        : base(YandexLanguageCodeResolver.Instance)
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
        : base(YandexLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string markup,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        (string translatedMarkup, string detectedSourceCode) = await _engine
            .TranslateMarkupAsync(markup, targetLanguageCode, sourceLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(translatedMarkup, detectedSourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
