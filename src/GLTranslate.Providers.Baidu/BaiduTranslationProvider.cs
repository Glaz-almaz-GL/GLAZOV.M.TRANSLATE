using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Baidu.Internal;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.Baidu;

/// <summary>
/// Translates text with the Baidu Translate open platform.
/// </summary>
/// <remarks>
/// <para>
/// The provider speaks to the official general translation endpoint on behalf
/// of an application registered with Baidu, whose <see cref="BaiduCredentials"/>
/// it is given. The service has to be switched on for the application in the
/// Baidu console, and Baidu limits how often it may be called: a call over the
/// limit is refused with code 54003, which reaches the caller as a
/// <see cref="ProviderException"/>.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class BaiduTranslationProvider : TextTranslationProviderBase, IDisposable
{
    private readonly BaiduEngine _engine;

    /// <inheritdoc/>
    public override string Name => BaiduProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The application the requests are made for.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public BaiduTranslationProvider(BaiduCredentials credentials)
        : base(BaiduLanguageCodeResolver.Instance)
    {
        _engine = new BaiduEngine(credentials);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduTranslationProvider"/>
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
    public BaiduTranslationProvider(BaiduCredentials credentials, HttpClient httpClient)
        : base(BaiduLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new BaiduEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        (string translation, string detectedSourceCode) = await _engine
            .TranslateAsync(text, sourceLanguageCode, targetLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(translation, detectedSourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
