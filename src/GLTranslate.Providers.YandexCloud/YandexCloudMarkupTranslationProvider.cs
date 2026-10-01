using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.YandexCloud.Internal;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.YandexCloud;

/// <summary>
/// Translates markup with the official Yandex Cloud Translate API, leaving its
/// tags where they are.
/// </summary>
/// <remarks>
/// <para>
/// The API translates the words between the tags of HTML and keeps the tags
/// themselves. Yandex takes 10,000 characters in a request.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class YandexCloudMarkupTranslationProvider : MarkupTranslationProviderBase, IDisposable
{
    private readonly YandexCloudEngine _engine;

    /// <inheritdoc/>
    public override string Name => YandexCloudProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudMarkupTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public YandexCloudMarkupTranslationProvider(YandexCloudCredentials credentials)
        : base(YandexCloudLanguageCodeResolver.Instance)
    {
        _engine = new YandexCloudEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YandexCloudMarkupTranslationProvider"/>
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
    public YandexCloudMarkupTranslationProvider(YandexCloudCredentials credentials, HttpClient httpClient)
        : base(YandexCloudLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new YandexCloudEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string markup,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        (IReadOnlyList<string> translations, string? detectedSourceCode) = await _engine
            .TranslateAsync([markup], isMarkup: true, sourceLanguageCode, targetLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(translations[0], detectedSourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
