using GLTranslate.Abstractions.Linguistics.Languages;
using GLTranslate.Abstractions.Providers;
using GLTranslate.Abstractions.Transliteration;
using GLTranslate.Providers.Bing.Internal;
using GLTranslate.Providers.Common;

namespace GLTranslate.Providers.Bing;

/// <summary>
/// Renders text in the Latin script with Bing Translator.
/// </summary>
/// <remarks>
/// <para>
/// Bing has no endpoint of its own for this: it romanizes the source text as
/// a by-product of translating it, so the provider asks for a translation
/// into English and keeps only that rendering. The endpoint detects the
/// language itself, so a request need not name it.
/// </para>
/// <para>
/// A text already written in the Latin script is not romanized, and the
/// endpoint returns nothing to keep; such a request is refused with a message
/// that says so.
/// </para>
/// <para>
/// The provider is thread-safe as long as the <see cref="HttpClient"/> it was
/// given is.
/// </para>
/// </remarks>
public sealed class BingTransliterationProvider : TransliterationProviderBase, IDisposable
{
    // The language to translate into while asking for the rendering. It only
    // has to be a language Bing translates into; the translation is discarded.
    private const string TargetLanguageCode = "en";

    private readonly BingEngine _engine;

    /// <inheritdoc/>
    public override string Name => BingProvider.Name;

    /// <summary>
    /// Initializes a new instance of the <see cref="BingTransliterationProvider"/> class.
    /// </summary>
    public BingTransliterationProvider()
        : base(BingLanguageCodeResolver.Instance)
    {
        _engine = new BingEngine();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BingTransliterationProvider"/>
    /// class with the specified <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The client to send the requests with. It stays owned by the caller.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient"/> is <see langword="null"/>.
    /// </exception>
    public BingTransliterationProvider(HttpClient httpClient)
        : base(BingLanguageCodeResolver.Instance)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _engine = new BingEngine(httpClient);
    }

    /// <inheritdoc/>
    protected override async Task<ProviderTransliteration> TransliterateAsync(
        string text,
        LanguageId? languageId,
        string? languageCode,
        CancellationToken cancellationToken)
    {
        BingAnswer answer = await _engine
            .TranslateAsync(text, TargetLanguageCode, languageCode, cancellationToken)
            .ConfigureAwait(false);

        if (answer.InputTransliteration is not { } transliteratedText)
        {
            throw new ProviderException(
                BingProvider.Name,
                "Bing Translator rendered nothing: the text is already written in the Latin script.");
        }

        return new ProviderTransliteration(transliteratedText, answer.SourceLanguageCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
