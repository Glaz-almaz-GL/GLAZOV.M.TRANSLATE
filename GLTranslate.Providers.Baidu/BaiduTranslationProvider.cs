using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Baidu.Internal;

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
public sealed class BaiduTranslationProvider : ITextTranslationProvider, IDisposable
{
    private readonly BaiduEngine _engine;

    /// <inheritdoc/>
    public string Name => BaiduProvider.Name;

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
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new BaiduEngine(credentials, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate, when
    /// Baidu refuses the request, or when the answer cannot be read.
    /// </exception>
    public async Task<TextTranslationResult> ExecuteAsync(
        TextTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = BaiduLanguageCodeResolver.ToBaiduCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : BaiduLanguageCodeResolver.ToBaiduCode(request.SourceLanguageId);

        (string translation, string resolvedSourceCode) = await _engine
            .TranslateAsync(request.Text.Value, sourceCode, targetCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = request.SourceLanguageId
            ?? BaiduLanguageCodeResolver.FromBaiduCode(resolvedSourceCode);

        return new TextTranslationResult(
            request.Id,
            new ProviderText(translation),
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
