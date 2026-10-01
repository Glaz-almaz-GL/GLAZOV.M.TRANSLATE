using GLAZOV.M.TRANSLATE.Abstractions.Translation;
using GLAZOV.M.TRANSLATE.Providers.Bing.Internal;
using GLAZOV.M.TRANSLATE.Providers.Common;

namespace GLAZOV.M.TRANSLATE.Providers.Bing;

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
public sealed class BingTranslationProvider : TextTranslationProviderBase, IDisposable
{
    private readonly BingEngine _engine;

    /// <inheritdoc/>
    public override string Name => BingProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="BingTranslationProvider"/> class.
    /// </summary>
    public BingTranslationProvider()
        : base(BingLanguageCodeResolver.Instance)
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
        : base(BingLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new BingEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTranslation> TranslateAsync(
        string text,
        string? sourceLanguageCode,
        string targetLanguageCode,
        CancellationToken cancellationToken)
    {
        BingAnswer answer = await _engine
            .TranslateAsync(text, targetLanguageCode, sourceLanguageCode, cancellationToken)
            .ConfigureAwait(false);

        return new ProviderTranslation(answer.TranslatedText, answer.SourceLanguageCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
