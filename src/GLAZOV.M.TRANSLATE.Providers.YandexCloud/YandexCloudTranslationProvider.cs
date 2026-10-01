using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using GLAZOV.M.TRANSLATE.Providers.YandexCloud.Internal;
using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.YandexCloud;

/// <summary>
/// Translates text with the official Yandex Cloud Translate API.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <c>YandexTranslationProvider</c>, which speaks as the Yandex
/// Translate mobile application does and may stop working without notice, this
/// provider speaks to the billed, documented and supported API (Translate v2),
/// with an API key.
/// </para>
/// <para>
/// Yandex takes 10,000 characters in a request. The provider is thread-safe as
/// long as the <see cref="HttpClient"/> it was given is.
/// </para>
/// </remarks>
public sealed class YandexCloudTranslationProvider : TextTranslationProviderBase, IDisposable
{
    private readonly YandexCloudEngine _engine;

    /// <inheritdoc/>
    public override string Name => YandexCloudProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public YandexCloudTranslationProvider(YandexCloudCredentials credentials)
        : base(YandexCloudLanguageCodeResolver.Instance)
    {
        _engine = new YandexCloudEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    public YandexCloudTranslationProvider(YandexCloudCredentials credentials, HttpClient httpClient)
        : base(YandexCloudLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexCloudEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        (IReadOnlyList<string> translations, string? detectedSourceCode) = await _engine
            .TranslateAsync([text], isMarkup: false, sourceLanguageCode, targetLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(translations[0], detectedSourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
