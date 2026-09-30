using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Baidu.Internal;

namespace GLTranslate.Providers.Baidu;

/// <summary>
/// Translates whole documents with the Baidu Translate open platform.
/// </summary>
/// <remarks>
/// <para>
/// Baidu reads Word (<c>doc</c>, <c>docx</c>), Excel (<c>xls</c>,
/// <c>xlsx</c>), PowerPoint (<c>ppt</c>, <c>pptx</c>), <c>pdf</c>, <c>html</c>,
/// <c>htm</c>, <c>xml</c> and <c>txt</c> files of up to 50 MB and gives back a
/// document of the same kind, except that <c>doc</c> and <c>pdf</c> come back
/// as <c>docx</c> and <c>xls</c> as <c>xlsx</c>: the format of
/// <see cref="DocumentTranslationResult.TranslatedDocument"/> says which.
/// </para>
/// <para>
/// The translation is a job. The provider submits it, asks how it is going
/// every <see cref="BaiduDocumentTranslationOptions.PollInterval"/> and
/// completes when the translated document has been downloaded; cancelling the
/// token stops the waiting, not the job, which Baidu finishes and charges for.
/// The service has to be switched on for the application in the Baidu console,
/// and unlike the free text service it costs money beyond a quota of free
/// characters.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class BaiduDocumentTranslationProvider : IDocumentTranslationProvider, IDisposable
{
    private readonly BaiduDocumentEngine _engine;

    /// <inheritdoc/>
    public string Name => BaiduProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduDocumentTranslationProvider"/> class.
    /// </summary>
    /// <param name="credentials">
    /// The application the requests are made for.
    /// </param>
    /// <param name="options">
    /// How to wait for the job, or <see langword="null"/> for
    /// <see cref="BaiduDocumentTranslationOptions.Default"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> is <see langword="null"/>.
    /// </exception>
    public BaiduDocumentTranslationProvider(BaiduCredentials credentials, BaiduDocumentTranslationOptions? options = null)
    {
        _engine = new BaiduDocumentEngine(credentials, options ?? BaiduDocumentTranslationOptions.Default);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaiduDocumentTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="credentials">
    /// The application the requests are made for.
    /// </param>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <param name="options">
    /// How to wait for the job, or <see langword="null"/> for
    /// <see cref="BaiduDocumentTranslationOptions.Default"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="credentials"/> or <paramref name="httpClient"/>
    /// is <see langword="null"/>.
    /// </exception>
    public BaiduDocumentTranslationProvider(
        BaiduCredentials credentials,
        HttpClient httpClient,
        BaiduDocumentTranslationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new BaiduDocumentEngine(credentials, options ?? BaiduDocumentTranslationOptions.Default, httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate, when
    /// the document is of a kind Baidu does not translate or is too large, when
    /// Baidu refuses the request or fails the job, when the job does not finish
    /// in time, or when an answer cannot be read.
    /// </exception>
    public async Task<DocumentTranslationResult> ExecuteAsync(
        DocumentTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = BaiduLanguageCodeResolver.ToBaiduCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : BaiduLanguageCodeResolver.ToBaiduCode(request.SourceLanguageId);

        (byte[] content, string format, string? detectedSourceCode) = await _engine
            .TranslateAsync(
                request.Document.Content.AsMemory(),
                request.Document.FileName,
                request.Document.Format,
                sourceCode,
                targetCode,
                cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = BaiduLanguageCodeResolver.Instance
            .ResolveSource(request.SourceLanguageId, detectedSourceCode);

        return new DocumentTranslationResult(
            request.Id,
            new ProviderDocument(content, Path.ChangeExtension(request.Document.FileName, format)),
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
