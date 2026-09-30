using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using GLTranslate.Providers.Google.Internal;
using GLTranslate.Providers.Common;

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
public sealed class GoogleTransliterationProvider : TransliterationProviderBase, IDisposable
{
    private readonly GoogleTranslationEngine _engine;

    /// <inheritdoc/>
    public override string Name => GoogleProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTransliterationProvider"/>
    /// class with an internally managed <see cref="HttpClient"/>.
    /// </summary>
    public GoogleTransliterationProvider()
        : base(GoogleLanguageCodeResolver.Instance)
    {
        _engine = new GoogleTranslationEngine();
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
        : base(GoogleLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleTranslationEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTransliteration> TransliterateAsync(
        string text,
        LanguageId? languageId,
        string? languageCode,
        CancellationToken cancellationToken)
    {
        // Google is asked to detect the language by naming "auto".
        (string transliteration, string detectedSourceCode) = await _engine
            .TransliterateAsync(text, languageCode ?? GoogleLanguageCodeResolver.AutoDetectCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTransliteration(transliteration, detectedSourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
