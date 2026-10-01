using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Yandex.Internal;
using GLTranslate.Providers.Common;

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
public sealed class YandexTranslationProvider : TextTranslationProviderBase, IDisposable
{
    private readonly YandexEngine _engine;

    /// <inheritdoc/>
    public override string Name => YandexProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexTranslationProvider"/> class.
    /// </summary>
    public YandexTranslationProvider()
        : base(YandexLanguageCodeResolver.Instance)
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
        : base(YandexLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        (string translatedText, string detectedSourceCode) = await _engine
            .TranslateAsync(text, targetLanguageCode, sourceLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(translatedText, detectedSourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
