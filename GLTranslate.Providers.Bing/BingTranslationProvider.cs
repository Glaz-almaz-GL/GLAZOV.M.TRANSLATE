using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Bing.Internal;

namespace GLTranslate.Providers.Bing;

/// <summary>
/// Translates text with Bing Translator.
/// </summary>
/// <remarks>
/// <para>
/// The provider needs no key: it reads the credentials the Bing translator
/// page hands to its own script and keeps them until they expire.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class BingTranslationProvider : ITextTranslationProvider, IDisposable
{
    private readonly BingEngine _engine;

    /// <inheritdoc/>
    public string Name => BingProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="BingTranslationProvider"/> class.
    /// </summary>
    public BingTranslationProvider()
    {
        _engine = new BingEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BingTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public BingTranslationProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new BingEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ProviderException">
    /// Thrown when a language of the request is unknown to GLTranslate, when
    /// Bing refuses the request, or when the endpoint answers with something
    /// the provider cannot read.
    /// </exception>
    public async Task<TextTranslationResult> ExecuteAsync(
        TextTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string targetCode = BingLanguageCodeResolver.ToBingCode(request.TargetLanguageId);

        string? sourceCode = request.SourceLanguageId is null
            ? null
            : BingLanguageCodeResolver.ToBingCode(request.SourceLanguageId);

        BingAnswer answer = await _engine
            .TranslateAsync(request.Text.Value, targetCode, sourceCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = request.SourceLanguageId
            ?? BingLanguageCodeResolver.FromBingCode(answer.SourceLanguageCode);

        return new TextTranslationResult(
            request.Id,
            new ProviderText(answer.TranslatedText),
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
