using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Google.Internal;

namespace GLTranslate.Providers.Google;

/// <summary>
/// Translates text using the free Google Translate web endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This provider integrates with the same public endpoint used by the
/// <c>translate.google.com</c> web widget rather than the official,
/// authenticated Google Cloud Translation API. It requires no API key, but
/// it is undocumented, unsupported by Google, and may change or stop
/// working without notice.
/// </para>
/// <para>
/// Instances of this class are immutable and thread-safe, provided the
/// supplied <see cref="HttpClient"/> is not mutated after construction.
/// </para>
/// </remarks>
public sealed class GoogleTranslationProvider : ITextTranslationProvider, IDisposable
{
    private readonly GoogleTranslationEngine _engine;

    /// <inheritdoc/>
    public string Name => GoogleProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTranslationProvider"/> class.
    /// </summary>
    public GoogleTranslationProvider()
    {
        _engine = new GoogleTranslationEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client used to send requests. The caller retains ownership
    /// and is responsible for disposing it.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public GoogleTranslationProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleTranslationEngine(httpClient);
    }

    /// <inheritdoc/>
    /// <exception cref="ProviderException">
    /// Thrown when <paramref name="request"/> specifies a language unknown
    /// to GLTranslate, or when the underlying request to Google Translate
    /// fails or returns an unexpected response.
    /// </exception>
    public async Task<TextTranslationResult> ExecuteAsync(TextTranslationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string sourceCode = GoogleLanguageCodeResolver.ToGoogleCode(request.SourceLanguageId);
        string targetCode = GoogleLanguageCodeResolver.ToGoogleCode(request.TargetLanguageId);

        (string translatedText, string detectedSourceCode) = await _engine
            .TranslateAsync(request.Text.Value, sourceCode, targetCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedSourceLanguageId = request.SourceLanguageId
            ?? GoogleLanguageCodeResolver.FromGoogleCode(detectedSourceCode);

        return new TextTranslationResult(
            request.Id,
            new ProviderText(translatedText),
            resolvedSourceLanguageId,
            request.TargetLanguageId,
            wasSourceLanguageDetected: request.SourceLanguageId is null);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine?.Dispose();
    }
}
