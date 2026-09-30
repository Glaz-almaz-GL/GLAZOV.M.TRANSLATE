using GLTranslate.Abstractions.Translation;
using GLTranslate.Providers.Google.Internal;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.Google;

/// <summary>
/// Translates markup with Google Translate, leaving its tags where they are.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint translates what stands between the tags and returns the
/// markup otherwise unchanged, so a page fragment comes back as the same
/// fragment in another language. Fetching the page is the caller's business:
/// this provider is handed markup and hands markup back.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class GoogleMarkupTranslationProvider : MarkupTranslationProviderBase, IDisposable
{
    private readonly GoogleMarkupTranslationEngine _engine;

    /// <inheritdoc/>
    public override string Name => GoogleProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleMarkupTranslationProvider"/> class.
    /// </summary>
    public GoogleMarkupTranslationProvider()
        : base(GoogleLanguageCodeResolver.Instance)
    {
        _engine = new GoogleMarkupTranslationEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleMarkupTranslationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public GoogleMarkupTranslationProvider(HttpClient httpClient)
        : base(GoogleLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new GoogleMarkupTranslationEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string markup,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        // Google is asked to detect the language by naming "auto".
        (string translatedMarkup, string? detectedSourceCode) = await _engine
            .TranslateAsync(markup, sourceLanguageCode ?? GoogleLanguageCodeResolver.AutoDetectCode, targetLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(translatedMarkup, detectedSourceCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
