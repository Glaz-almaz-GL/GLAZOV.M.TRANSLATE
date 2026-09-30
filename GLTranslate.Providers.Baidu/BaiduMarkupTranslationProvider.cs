using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Baidu.Internal;

namespace GLTranslate.Providers.Baidu;

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
public sealed class BaiduMarkupTranslationProvider : IMarkupTranslationProvider, IDisposable
{
    private readonly BaiduEngine _engine;

    /// <inheritdoc/>
    public string Name => BaiduProvider.Name;

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
    public async Task<MarkupTranslationResult> ExecuteAsync(
        MarkupTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = BaiduLanguageCodeResolver.ToBaiduCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : BaiduLanguageCodeResolver.ToBaiduCode(request.SourceLanguageId);

        (string translation, string resolvedSourceCode) = await _engine
            .TranslateMarkupAsync(request.Markup.Value, sourceCode, targetCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = request.SourceLanguageId
            ?? BaiduLanguageCodeResolver.FromBaiduCode(resolvedSourceCode);

        return new MarkupTranslationResult(
            request.Id,
            new ProviderMarkup(translation),
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
