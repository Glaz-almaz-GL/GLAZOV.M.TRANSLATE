using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using GLTranslate.Providers.Google.Internal;

namespace GLTranslate.Providers.Google;

/// <summary>
/// Transliterates text using the free Google Translate web endpoint.
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
/// Google only exposes transliteration as a by-product of translating text
/// to a pivot language; this provider hides that detail behind
/// <see cref="ITransliterationProvider"/>.
/// </para>
/// <para>
/// Instances of this class are immutable and thread-safe, provided the
/// supplied <see cref="HttpClient"/> is not mutated after construction.
/// </para>
/// </remarks>
public sealed class GoogleTransliterationProvider : ITransliterationProvider, IDisposable
{
    private readonly GoogleTranslationEngine _engine;
    private readonly HttpClient? _ownedHttpClient;

    /// <inheritdoc/>
    public string Name => GoogleProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTransliterationProvider"/>
    /// class with an internally managed <see cref="HttpClient"/>.
    /// </summary>
    public GoogleTransliterationProvider()
    {
        _ownedHttpClient = new HttpClient();
        _engine = new GoogleTranslationEngine(_ownedHttpClient);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTransliterationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client used to send requests. The caller retains ownership
    /// and is responsible for disposing it.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public GoogleTransliterationProvider(HttpClient httpClient)
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
    public async Task<TransliterationResult> ExecuteAsync(TransliterationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string sourceCode = GoogleLanguageCodeResolver.ToGoogleCode(request.LanguageId);

        (string transliteration, string detectedSourceCode) = await _engine
            .TransliterateAsync(request.Text.Value, sourceCode, cancellationToken)
            .ConfigureAwait(false);

        LanguageId resolvedLanguageId = request.LanguageId
            ?? GoogleLanguageCodeResolver.FromGoogleCode(detectedSourceCode);

        return new TransliterationResult(
            request.Id,
            new TransliteratedText(transliteration),
            resolvedLanguageId,
            wasLanguageDetected: request.LanguageId is null);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _ownedHttpClient?.Dispose();
    }
}
