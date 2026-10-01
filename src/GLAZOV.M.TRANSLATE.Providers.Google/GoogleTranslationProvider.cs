using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using GLAZOV.M.TRANSLATE.Providers.Google.Internal;
using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.Google;

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
public sealed class GoogleTranslationProvider : TextTranslationProviderBase, IDisposable
{
    private readonly GoogleTranslationEngine _engine;

    /// <inheritdoc/>
    public override string Name => GoogleProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTranslationProvider"/> class.
    /// </summary>
    public GoogleTranslationProvider()
        : base(GoogleLanguageCodeResolver.Instance)
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
        : base(GoogleLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleTranslationEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        // Google is asked to detect the language by naming "auto".
        (string translatedText, string detectedSourceCode) = await _engine
            .TranslateAsync(text, sourceLanguageCode ?? GoogleLanguageCodeResolver.AutoDetectCode, targetLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(translatedText, detectedSourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine?.Dispose();
    }
}
