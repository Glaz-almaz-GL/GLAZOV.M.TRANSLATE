using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using GLAZOV.M.TRANSLATE.Providers.Baidu.Internal;
using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.Baidu;

/// <summary>
/// Translates markup with the Baidu Translate open platform, leaving its tags
/// where they are.
/// </summary>
/// <remarks>
/// <para>
/// The provider uses the text endpoint of the platform that keeps tags, with
/// the machine translation model, the only one that does. That endpoint needs
/// its own service switched on for the application in the Baidu console.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class BaiduMarkupTranslationProvider : MarkupTranslationProviderBase, IDisposable
{
    private readonly BaiduEngine _engine;

    /// <inheritdoc/>
    public override string Name => BaiduProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduMarkupTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The application the requests are made for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public BaiduMarkupTranslationProvider(BaiduCredentials credentials)
        : base(BaiduLanguageCodeResolver.Instance)
    {
        _engine = new BaiduEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduMarkupTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The application the requests are made for.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is <see langword="null"/>.
    /// </exception>
    public BaiduMarkupTranslationProvider(BaiduCredentials credentials, HttpClient httpClient)
        : base(BaiduLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new BaiduEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string markup,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        (string translation, string detectedSourceCode) = await _engine
            .TranslateMarkupAsync(markup, sourceLanguageCode, targetLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(translation, detectedSourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
