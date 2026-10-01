using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using GLAZOV.M.TRANSLATE.Providers.GoogleCloud.Internal;
using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.GoogleCloud;

/// <summary>
/// Translates markup with the official Google Cloud Translation API, leaving
/// its tags where they are.
/// </summary>
/// <remarks>
/// <para>
/// The API translates the words between the tags of HTML and keeps the tags
/// themselves. It writes special characters of the translation as HTML
/// entities, as HTML does.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class GoogleCloudMarkupTranslationProvider : MarkupTranslationProviderBase, IDisposable
{
    private readonly GoogleCloudEngine _engine;

    /// <inheritdoc/>
    public override string Name => GoogleCloudProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudMarkupTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The key the requests are made with.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public GoogleCloudMarkupTranslationProvider(GoogleCloudCredentials credentials)
        : base(GoogleCloudLanguageCodeResolver.Instance)
    {
        _engine = new GoogleCloudEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudMarkupTranslationProvider"/>
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
    public GoogleCloudMarkupTranslationProvider(GoogleCloudCredentials credentials, HttpClient httpClient)
        : base(GoogleCloudLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleCloudEngine(credentials, httpClient);
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
